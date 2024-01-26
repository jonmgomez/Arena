using System.Collections;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


public class Player : NetworkBehaviour
{
    static Vector3 OFF_SCREEN = new(0f, -100f, 0f);

    bool isDead = false;

    [SerializeField] float health = 100f;
    // Internal variable to track health on the client before the server has a chance to update it
    float clientSideHealth = 100f;
    float maxHealth = 100f;

    bool regeneratingHealth = false;
    [SerializeField] float healthRegenDelay = 5f;
    [SerializeField] float healthRegenPerSecond = 10f;

    Vignette vignette;
    [SerializeField] float vignetteMaxIntensity = 0.4f;

    ClientNetworkTransform clientNetworkTransform;
    CharacterController characterController;
    PlayerMovement playerMovement;
    PlayerWeapon playerWeapon;
    [SerializeField]  PlayerCamera playerCamera;

    // Necessary to prevent regen coroutine from running multiple times. Only using the method name does not work
    Coroutine regenHealthCoroutine = null;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            PlayerSpawnController.Instance.RegisterPlayer(this);
    }

    void Start()
    {
        clientNetworkTransform = GetComponent<ClientNetworkTransform>();
        if (!IsOwner)
            return;

        clientSideHealth = health;
        maxHealth = health;

        Volume volume = FindObjectOfType<Volume>();
        volume.profile.TryGet<Vignette>(out vignette);
        vignette.intensity.value = 0f;

        characterController = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();
        playerWeapon = GetComponent<PlayerWeapon>();
    }

    void Update()
    {
        if (IsServer && regeneratingHealth)
            RegenHealthOnServer();

        if (!IsOwner)
            return;
    }

    public void TakeDamage(float damage, ulong clientId)
    {
        Debug.Log($"[CLIENT] Player {OwnerClientId} took {damage} damage, Health {health}, client-side health {clientSideHealth}");
        clientSideHealth -= damage;
        if (clientSideHealth <= 0f)
        {
            Debug.Log($"[CLIENT] Player {OwnerClientId} died");
            OnDeath();
        }

        TakeDamageServerRpc(damage, clientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void TakeDamageServerRpc(float damage, ulong clientId)
    {
        if (health - damage <= 0f)
        {
            PlayerSpawnController.Instance.RespawnPlayer(this);
        }

        TakeDamageClientRpc(damage, clientId);

        regeneratingHealth = false;
        if (regenHealthCoroutine != null)
            StopCoroutine(regenHealthCoroutine);
        regenHealthCoroutine = StartCoroutine(StartHealthRegenOnServer());
    }

    [ClientRpc]
    private void TakeDamageClientRpc(float damage, ulong clientId)
    {
        health -= damage;

        // If this is not executing on the player that dealt the damage, update the client-side health
        // Otherwise, client-side health was already updated by the client that dealt the damage
        if (clientId != NetworkManager.Singleton.LocalClientId)
            clientSideHealth -= damage;

        if (clientSideHealth <= 0f)
        {
            OnDeath();
        }
        else if (IsOwner)
        {
            RecalculateHealthVignette();
        }
    }

    [ClientRpc]
    public void HealClientRpc(float amount)
    {
        if (health + amount > maxHealth)
        {
            health = maxHealth;
            clientSideHealth += maxHealth - health;
        }
        else
        {
            health += amount;
            clientSideHealth += amount;
        }

        if (IsOwner)
        {
            RecalculateHealthVignette();
        }
    }

    IEnumerator StartHealthRegenOnServer()
    {
        yield return new WaitForSeconds(healthRegenDelay);
        regeneratingHealth = true;
    }

    // OnServer: Only called on the server (ServerRpc)
    private void RegenHealthOnServer()
    {
        HealClientRpc(healthRegenPerSecond * Time.deltaTime);
    }

    private void RecalculateHealthVignette()
    {
        vignette.intensity.value = (1f - health / maxHealth) * vignetteMaxIntensity;
    }

    private void OnDeath()
    {
        if (isDead)
            return;

        isDead = true;
        clientNetworkTransform.enabled = false;
        transform.position = OFF_SCREEN;

        if (IsOwner) // Client specific references
        {
            vignette.intensity.value = 0f;
            characterController.enabled = false;
            playerMovement.enabled = false;
            playerWeapon.SetEnabled(false);
            playerCamera.SetEnabled(false);
        }
    }

    // OnServer: Only called on the server (ServerRpc)
    public void RespawnOnServer(Vector3 spawnPoint)
    {
        RespawnClientRpc(spawnPoint);
    }

    [ClientRpc]
    public void RespawnClientRpc(Vector3 spawnPoint)
    {
        isDead = false;
        health = maxHealth;
        clientSideHealth = maxHealth;

        transform.position = spawnPoint;
        clientNetworkTransform.enabled = true;

        // Interpolation from offscreen to spawn point looks bad, so disable interpolation for a short time
        clientNetworkTransform.Interpolate = false;
        this.Invoke(() => clientNetworkTransform.Interpolate = true, 0.25f);

        if (IsOwner)
        {
            characterController.enabled = true;
            playerMovement.enabled = true;
            playerWeapon.SetEnabled(true);
            playerCamera.SetEnabled(true);
        }
    }
}
