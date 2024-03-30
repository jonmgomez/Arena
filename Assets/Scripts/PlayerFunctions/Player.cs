using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


public class Player : NetworkBehaviour
{
    private static Vector3 OFF_SCREEN = new(0f, -100f, 0f);
    private readonly Logger logger = new("PLAYR");
    private const string SELF_LAYER = "PlayerSelf";

    [Header("Health")]
    [Tooltip("Starting and max health of the player")]
    [SerializeField] private float health = 100f;
    [Tooltip("Time in seconds before health starts regenerating")]
    [SerializeField] private float healthRegenDelay = 5f;
    [Tooltip("Amount of health regenerated per second")]
    [SerializeField] private float healthRegenPerSecond = 10f;
    [Tooltip("Maximum intensity of the health vignette. This is the maximum red overlay on the screen when health is low.")]
    [SerializeField] float healthVignetteMaxIntensity = 0.4f;

    [Header("References")]
    [Tooltip("Third person mesh of the player. This is the mesh that other players see")]
    [SerializeField] private GameObject thirdPersonMesh;
    [SerializeField] private Renderer[] thirdPersonRenderers;
    [Tooltip("First person mesh of the player. This is the mesh that the player sees")]
    [SerializeField] private GameObject firstPersonMesh;
    [SerializeField] private GameObject firstPersonWeapons;
    [SerializeField] private Renderer firstPersonRenderer;

    [Tooltip("TextMeshPro object to display player name")]
    [SerializeField] TextMeshPro playerNameText;

    [Tooltip("Specify the head collider box. Collisions to this will deal more damage")]
    [SerializeField] private Collider headCollider;

    [Header("Debug")]
    [Tooltip("Show the third person mesh instead of the first person mesh for the local player")]
    [SerializeField] private bool showThirdPersonMesh = false;

    // Health ----------------
    // Internal variable to track health on the client before the server has a chance to update it
    private float clientSideHealth = 100f;
    private float maxHealth = 100f;
    private bool isDead = false;
    private bool regeneratingHealth = false;
    Vignette healthVignette;

    // Components ----------------
    private ClientNetworkTransform clientNetworkTransform;
    private CharacterController characterController;
    private PlayerMovement playerMovement;
    private PlayerWeapon playerWeapon;
    private PlayerWeaponAnimator playerWeaponAnimator;
    private PlayerScore playerScore;
    private PlayerCamera playerCamera;
    [NonSerialized] public PlayerHUD HUD;

    Coroutine regenHealthCoroutine = null;

    public override void OnNetworkSpawn()
    {
        // If this player has just connected and immediately loads in a game scene,
        // then we must wait for the client to be ready before properly setting player data
        // Note that the local player instance has not spawned (and will when synced),
        // but the other players have already spawned due to Unity spawning all NetworkObjects
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
        List<Renderer> renderers = new();
        if (IsOwner && !showThirdPersonMesh)
        {
            renderers.AddRange(thirdPersonMesh.GetComponentsInChildren<Renderer>(true));
        }
        else
        {
            renderers.AddRange(firstPersonMesh.GetComponentsInChildren<Renderer>(true));
            renderers.AddRange(firstPersonWeapons.GetComponentsInChildren<Renderer>(true));
        }

        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = false;
        }

        GameState.Instance.SetPlayer(this);
        playerNameText.text = GameState.Instance.GetClientData(OwnerClientId).clientName;
        gameObject.name = "Player-" + OwnerClientId + " (" + playerNameText.text + ")";


        if (IsOwner)
        {
            Transform[] children = GetComponentsInChildren<Transform>();
            foreach (Transform child in children)
            {
                child.gameObject.layer = LayerMask.NameToLayer(SELF_LAYER);
            }
        }

        InGameController.Instance.GetPlayerMaterial(this);
        InGameController.Instance.PlayerSpawned(this);
    }

    void Awake()
    {
        clientNetworkTransform = GetComponent<ClientNetworkTransform>();
        characterController = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();
        playerWeapon = GetComponent<PlayerWeapon>();
        playerWeaponAnimator = playerWeapon.GetComponent<PlayerWeaponAnimator>();
        playerScore = GetComponent<PlayerScore>();
        playerCamera = GetComponentInChildren<PlayerCamera>();

        if (!IsOwner)
            return;
    }

    void Start()
    {
        if (!IsOwner)
            return;

        characterController.enabled = true;
        playerNameText.gameObject.SetActive(false);

        HUD = FindObjectOfType<PlayerHUD>(true);
        HUD.gameObject.SetActive(true);

        clientSideHealth = health;
        maxHealth = health;

        Volume volume = FindObjectOfType<Volume>();
        volume.profile.TryGet<Vignette>(out healthVignette);
        healthVignette.intensity.value = 0f;
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
        TakeDamageInternal(damage, damagerClientId, false);
    }

    /// <summary>
    /// Same as TakeDamage(), but does not require a clientId attributed to the player that dealt the damage.
    /// This is for damage which is not dealt by a player, such as falling off the map.
    /// </summary>
    /// <param name="damage">Amount of damage to deal to player</param>
    public void TakeDamageAnonymous(float damage)
    {
        TakeDamageInternal(damage, Net.LocalClientId, true);
    }

    private void TakeDamageInternal(float damage, ulong damagerClientId, bool isAnonymous)
    {
        if (clientSideHealth <= 0f) return;

        clientSideHealth -= damage;
        if (!isAnonymous)
            logger.Log($"{Utility.PlayerNameToString(OwnerClientId)} took {damage} damage from {Utility.PlayerNameToString(damagerClientId)}, Health {health}, client-side health {clientSideHealth}");
        else
            logger.Log($"{Utility.PlayerNameToString(OwnerClientId)} took {damage} damage, Health {health}, client-side health {clientSideHealth}");

        if (clientSideHealth <= 0f)
        {
            logger.Log($"{Utility.PlayerNameToString(OwnerClientId)} died");
            OnDeath();
            InGameController.Instance.PlayerDied(this, damagerClientId, isAnonymous);
        }
        else
        {
            InGameController.Instance.PlayerDamaged(this, damagerClientId, isAnonymous);
        }

        TakeDamageServerRpc(damage, damagerClientId, isAnonymous);
    }

    [ServerRpc(RequireOwnership = false)]
    private void TakeDamageServerRpc(float damage, ulong clientId, bool isAnonymous = false)
    {
        if (health > 0f && health - damage <= 0f)
        {
            logger.Log($"[S] {Utility.PlayerNameToString(OwnerClientId)} died");
            PlayerSpawnController.Instance.RespawnPlayer(this);
        }

        if (Net.IsServerOnly)
            health -= damage;

        TakeDamageClientRpc(damage, clientId, isAnonymous);

        regeneratingHealth = false;
        regenHealthCoroutine = this.RestartCoroutine(StartHealthRegenOnServer(), regenHealthCoroutine);
    }

    [ClientRpc]
    private void TakeDamageClientRpc(float damage, ulong clientId, bool isAnonymous = false)
    {
        if (health <= 0f)
            return;

        // Client side health tracks local changes. Health changes are verified by the server
        health -= damage;

        // If this is not executing on the player that dealt the damage, update the client-side health
        // Otherwise, client-side health was already updated by the client that dealt the damage
        if (Net.IsLocalClient(clientId))
            return;

        if (!isAnonymous)
        {
            clientSideHealth -= damage;
            logger.Log($"{Utility.PlayerNameToString(OwnerClientId)} took {damage} damage from {Utility.PlayerNameToString(clientId)}, " +
                       $"Health: {health}, Client-Side Health: {clientSideHealth}");
        }
        else
        {
            clientSideHealth -= damage;
            logger.Log($"{Utility.PlayerNameToString(OwnerClientId)} took {damage} damage, Health: {health}, Client-Side Health: {clientSideHealth}");
        }

        if (clientSideHealth <= 0f)
        {
            logger.Log($"{Utility.PlayerNameToString(OwnerClientId)} died");
            OnDeath();

            InGameController.Instance.PlayerDied(this, clientId, isAnonymous);
        }
        else
        {
            if (IsOwner)
            {
                RecalculateHealthVignette();
            }

            InGameController.Instance.PlayerDamaged(this, clientId, isAnonymous);
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
        healthVignette.intensity.value = (1f - health / maxHealth) * healthVignetteMaxIntensity;
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
            healthVignette.intensity.value = 0f;
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
            healthVignette.intensity.value = 0f;
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

    public void SetMaterial(Material material)
    {
        foreach (Renderer renderer in thirdPersonRenderers)
        {
            renderer.material = material;
        }

        firstPersonRenderer.material = material;
    }

    public string GetName() => playerNameText.text;
    public bool IsHeadCollider(Collider collider) => headCollider == collider;
    public bool ShowFirstPersonMesh() => IsOwner && !showThirdPersonMesh;

    public PlayerMovement GetPlayerMovement() => playerMovement;
    public PlayerWeapon GetPlayerWeapon() => playerWeapon;
    public PlayerWeaponAnimator GetPlayerWeaponAnimator() => playerWeaponAnimator;
    public PlayerCamera GetPlayerCamera() => playerCamera;
    public PlayerScore GetPlayerScore() => playerScore;
}
