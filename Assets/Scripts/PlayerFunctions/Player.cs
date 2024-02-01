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
        characterController = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();
        playerWeapon = GetComponent<PlayerWeapon>();
        if (!IsOwner)
            return;

        clientSideHealth = health;
        maxHealth = health;

        Volume volume = FindObjectOfType<Volume>();
        volume.profile.TryGet<Vignette>(out vignette);
        vignette.intensity.value = 0f;

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
        clientSideHealth -= damage;
        if (clientSideHealth <= 0f)
        {
            Debug.Log($"[CLIENT] Player-{OwnerClientId} died");
            OnDeath();
        }
        Debug.Log($"[CLIENT] Player-{OwnerClientId} took {damage} damage from Player-{clientId}, Health {health}, client-side health {clientSideHealth}");

        TakeDamageServerRpc(damage, clientId);
    }

    /// <summary>
    /// Same as TakeDamage(), but does not require a clientId attributed to the player that dealt the damage.
    /// This is for damage which is not dealt by a player, such as falling off the map.
    /// </summary>
    /// <param name="damage">Amount of damage to deal to player</param>
    public void TakeDamageAnonymous(float damage)
    {
        clientSideHealth -= damage;
        if (clientSideHealth <= 0f)
        {
            Debug.Log($"[CLIENT] Player-{OwnerClientId} died");
            OnDeath();
        }
        Debug.Log($"[CLIENT] Player {OwnerClientId} took {damage} damage, Health {health}, client-side health {clientSideHealth}");

        TakeDamageServerRpc(damage, 0, true);
    }

    [ServerRpc(RequireOwnership = false)]
    private void TakeDamageServerRpc(float damage, ulong clientId, bool isAnonymous = false)
    {
        if (health - damage <= 0f)
        {
            Debug.Log($"[SERVER] Player {OwnerClientId} died");
            PlayerSpawnController.Instance.RespawnPlayer(this);
        }

        if (IsServer && !IsHost)
            health -= damage;

        TakeDamageClientRpc(damage, clientId, isAnonymous);

        regeneratingHealth = false;
        if (regenHealthCoroutine != null)
            StopCoroutine(regenHealthCoroutine);
        regenHealthCoroutine = StartCoroutine(StartHealthRegenOnServer());
    }

    [ClientRpc]
    private void TakeDamageClientRpc(float damage, ulong clientId, bool isAnonymous = false)
    {
        health -= damage;

        if (!isAnonymous)
        {
            // If this is not executing on the player that dealt the damage, update the client-side health
            // Otherwise, client-side health was already updated by the client that dealt the damage
            if (clientId != NetworkManager.Singleton.LocalClientId)
            {
                Debug.Log($"[CLIENT] Player {OwnerClientId} took {damage} damage from another Player (Player-{clientId}), " +
                          $"Health: {health}, Client-Side Health: {clientSideHealth}");
                clientSideHealth -= damage;
            }
        }

        if (clientSideHealth <= 0f)
        {
            Debug.Log($"[CLIENT] Player {OwnerClientId} died (client rpc)");
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
            if (clientSideHealth > maxHealth)
                clientSideHealth = maxHealth;
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
        characterController.enabled = false;

        if (IsOwner) // Client specific references
        {
            vignette.intensity.value = 0f;
            playerMovement.enabled = false;
            playerWeapon.SetEnabled(false);
            playerCamera.SetEnabled(false);
        }
    }

    // OnServer: Only called on the server (ServerRpc)
    public void RespawnOnServer(Vector3 spawnPoint)
    {
        if (IsServer && !IsHost)
        {
            RespawnPlayer(spawnPoint);
        }

        RespawnClientRpc(spawnPoint);
    }

    [ClientRpc]
    public void RespawnClientRpc(Vector3 spawnPoint)
    {
        RespawnPlayer(spawnPoint);

        if (IsOwner)
        {
            vignette.intensity.value = 0f;
            playerMovement.enabled = true;
            playerWeapon.SetEnabled(true);
            playerCamera.SetEnabled(true);
        }
    }

    private void RespawnPlayer(Vector3 spawnPoint)
    {
        isDead = false;
        health = maxHealth;
        clientSideHealth = maxHealth;

        transform.position = spawnPoint;
        clientNetworkTransform.enabled = true;
        characterController.enabled = true;

        // Interpolation from offscreen to spawn point looks bad, so disable interpolation for a short time
        clientNetworkTransform.Interpolate = false;
        this.Invoke(() => clientNetworkTransform.Interpolate = true, 0.25f);
    }
}