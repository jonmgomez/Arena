using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


public class Player : NetworkBehaviour
{
    static Vector3 OFF_SCREEN = new(0f, -100f, 0f);
    private readonly Logger logger = new("PLAYR");
    private const string SELF_LAYER = "PlayerSelf";

    [SerializeField] TextMeshPro playerNameText;

    [SerializeField] float health = 100f;
    // Internal variable to track health on the client before the server has a chance to update it
    float clientSideHealth = 100f;
    float maxHealth = 100f;
    bool isDead = false;

    bool regeneratingHealth = false;
    [SerializeField] float healthRegenDelay = 5f;
    [SerializeField] float healthRegenPerSecond = 10f;

    Vignette vignette;
    [SerializeField] float vignetteMaxIntensity = 0.4f;

    ClientNetworkTransform clientNetworkTransform;
    CharacterController characterController;
    PlayerMovement playerMovement;
    PlayerWeapon playerWeapon;
    [NonSerialized] public PlayerHUD HUD;
    [SerializeField] PlayerCamera playerCamera;

    [SerializeField] GameObject thirdPersonMesh;
    [SerializeField] GameObject firstPersonMesh;

    [Header("Debug")]
    [SerializeField] private bool showThirdPersonMesh = false;

    // Necessary to prevent regen coroutine from running multiple times. Only using the method name does not work
    Coroutine regenHealthCoroutine = null;

    public override void OnNetworkSpawn()
    {
        // If this player has just connected and immediately loads in a game scene,
        // then we must wait for the client to be ready before properly setting player data
        if (GameState.Instance.GetConnectedClients().Count <= 0)
        {
            GameState.Instance.ClientReady += (clientId) =>
            {
                if (Net.IsLocalClient(clientId))
                {
                    NetworkSpawn();
                }
            };
        }
        else
        {
            NetworkSpawn();
        }
    }

    void NetworkSpawn()
    {
        Renderer[] renderers;
        if (IsOwner && !showThirdPersonMesh)
        {
            renderers = thirdPersonMesh.GetComponentsInChildren<Renderer>(true);
        }
        else
        {
            renderers = firstPersonMesh.GetComponentsInChildren<Renderer>(true);
        }

        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = false;
        }

        GameState.Instance.SetPlayer(this);
        playerNameText.text = GameState.Instance.GetClientData(OwnerClientId).clientName;

        if (IsOwner)
        {
            Transform[] children = GetComponentsInChildren<Transform>();
            foreach (Transform child in children)
            {
                child.gameObject.layer = LayerMask.NameToLayer(SELF_LAYER);
            }
        }
    }

    void Start()
    {
        clientNetworkTransform = GetComponent<ClientNetworkTransform>();
        characterController = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();
        playerWeapon = GetComponent<PlayerWeapon>();

        if (!IsOwner)
            return;

        playerNameText.gameObject.SetActive(false);

        HUD = FindObjectOfType<PlayerHUD>(true);
        HUD.gameObject.SetActive(true);

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

    #region Health
    public void TakeDamage(float damage, ulong damagerClientId)
    {
        clientSideHealth -= damage;
        if (clientSideHealth <= 0f)
        {
            logger.Log($"[CLIENT] Player-{OwnerClientId} died");
            OnDeath();
        }
        logger.Log($"[CLIENT] Player-{OwnerClientId} took {damage} damage from Player-{damagerClientId}, Health {health}, client-side health {clientSideHealth}");

        TakeDamageServerRpc(damage, damagerClientId);
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
            logger.Log($"[CLIENT] Player-{OwnerClientId} died");
            OnDeath();
        }
        logger.Log($"[CLIENT] Player {OwnerClientId} took {damage} damage, Health {health}, client-side health {clientSideHealth}");

        TakeDamageServerRpc(damage, 0, true);
    }

    [ServerRpc(RequireOwnership = false)]
    private void TakeDamageServerRpc(float damage, ulong clientId, bool isAnonymous = false)
    {
        if (health > 0f && health - damage <= 0f)
        {
            logger.Log($"[SERVER] Player {OwnerClientId} died");
            InGameController.Instance.PlayerDied(this, clientId, isAnonymous);
            PlayerSpawnController.Instance.RespawnPlayer(this);
        }
        else
        {
            InGameController.Instance.PlayerDamaged(this, clientId, isAnonymous);
        }

        if (Net.IsServerOnly)
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
                logger.Log($"[CLIENT] Player {OwnerClientId} took {damage} damage from another Player (Player-{clientId}), " +
                          $"Health: {health}, Client-Side Health: {clientSideHealth}");
                clientSideHealth -= damage;
            }
        }

        if (clientSideHealth <= 0f)
        {
            logger.Log($"[CLIENT] Player {OwnerClientId} died");
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
    #endregion

    private void RecalculateHealthVignette()
    {
        vignette.intensity.value = (1f - health / maxHealth) * vignetteMaxIntensity;
    }

    private void OnDeath()
    {
        if (isDead)
            return;

        isDead = true;
        characterController.enabled = false;
        transform.position = OFF_SCREEN;
        clientNetworkTransform.enabled = false;

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
        if (Net.IsServerOnly)
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

    public string GetName() => playerNameText.text;

    public bool ShowFirstPersonMesh() => IsOwner && !showThirdPersonMesh;
}
