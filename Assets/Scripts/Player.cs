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

    [SerializeField] NetworkVariable<float> health = new(100f);
    float maxHealth = 100f;
    bool regeneratingHealth = false;
    [SerializeField] float healthRegenDelay = 5f;
    [SerializeField] float healthRegenRatePerSecond = 10f;
    Vignette vignette;
    [SerializeField] float vignetteMaxIntensity = 0.4f;

    ClientNetworkTransform clientNetworkTransform;
    CharacterController characterController;
    PlayerMovement playerMovement;
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
        if (!IsOwner)
            return;

        maxHealth = health.Value;
        Volume volume = FindObjectOfType<Volume>();
        volume.profile.TryGet<Vignette>(out vignette);
        vignette.intensity.value = 0f;

        clientNetworkTransform = GetComponent<ClientNetworkTransform>();
        characterController = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();

        health.OnValueChanged += HealthChanged;
    }

    void Update()
    {
        if (IsServer && regeneratingHealth)
            RegenHealthOnServer();

        if (!IsOwner)
            return;
    }


    // Called from other scripts
    // Then calls to server to deal damage to this player
    // TakeDamage() -> TakeDamageServerRpc()
    public void TakeDamage(float damage)
    {
        TakeDamageServerRpc(damage);
    }

    [ServerRpc(RequireOwnership = false)]
    private void TakeDamageServerRpc(float damage)
    {
        Debug.Log($"Player {OwnerClientId} took {damage} damage");
        health.Value -= damage;
        if (health.Value <= 0f)
        {
            PlayerSpawnController.Instance.RespawnPlayer(this);
        }

        if (regenHealthCoroutine != null)
            StopCoroutine(regenHealthCoroutine);
        regeneratingHealth = false;
        regenHealthCoroutine = StartCoroutine(StartHealthRegenOnServer());
    }

    private void HealthChanged(float oldHealthValue, float newHealthValue)
    {
        bool damaged = newHealthValue < oldHealthValue;
        if (newHealthValue <= 0f)
        {
            OnDeath();
        }
        else
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
        health.Value += healthRegenRatePerSecond * Time.deltaTime;
        if (health.Value >= 100f)
        {
            health.Value = 100f;
            regeneratingHealth = false;
        }
    }

    private void RecalculateHealthVignette()
    {
        vignette.intensity.value = (1f - health.Value / maxHealth) * vignetteMaxIntensity;
    }

    private void OnDeath()
    {
        transform.position = OFF_SCREEN;
        vignette.intensity.value = 0f;
        clientNetworkTransform.enabled = false;
        characterController.enabled = false;
        playerMovement.enabled = false;
        playerCamera.SetEnabled(false);
    }

    // OnServer: Only called on the server (ServerRpc)
    public void RespawnOnServer(Vector3 spawnPoint, ulong clientId)
    {
        health.Value = maxHealth;

        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[]{clientId}
            }
        };
        RespawnClientRpc(spawnPoint, clientRpcParams);
    }

    [ClientRpc]
    public void RespawnClientRpc(Vector3 spawnPoint, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log(health.Value);
        transform.position = spawnPoint;
        clientNetworkTransform.enabled = true;
        characterController.enabled = true;
        playerMovement.enabled = true;
        playerCamera.SetEnabled(true);
    }
}
