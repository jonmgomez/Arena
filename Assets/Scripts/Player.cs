using System.Collections;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class Player : NetworkBehaviour
{
    Vector3 offScreen = new Vector3(0f, -100f, 0f);

    [SerializeField] NetworkVariable<float> health = new(100f);
    float maxHealth = 100f;
    [SerializeField] float healthRegenDelay = 5f;
    bool regeneratingHealth = false;
    [SerializeField] float healthRegenRatePerSecond = 10f;
    Vignette vignette;
    [SerializeField] float vignetteMaxIntensity = 0.4f;

    ClientNetworkTransform clientNetworkTransform;
    CharacterController characterController;
    PlayerMovement playerMovement;
    [SerializeField]  PlayerCamera playerCamera;

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
    }

    void Update()
    {
        if (!IsOwner)
            return;

        if (regeneratingHealth)
            RegenHealth();
    }

    // Called from other scripts
    // Then calls to server to deal damage to this player
    // And then calls to client that was hit to handle effects
    // TakeDamage() -> TakeDamageServerRpc() -> TakeDamageClientRpc()
    public void TakeDamage(float damage)
    {
        TakeDamageServerRpc(OwnerClientId, damage);
    }

    [ServerRpc(RequireOwnership = false)]
    private void TakeDamageServerRpc(ulong clientId, float damage)
    {
        Debug.Log($"Player {clientId} took {damage} damage");
        health.Value -= damage;
        if (health.Value <= 0f)
        {
            PlayerSpawnController.Instance.RespawnPlayer(this);
        }

        ClientRpcParams clientRpcParams = Utility.CreateClientRpcParams(clientId);
        TakeDamageClientRpc(health.Value, clientRpcParams);
    }

    // Called only on the client that was hit
    [ClientRpc]
    private void TakeDamageClientRpc(float newHealthValue, ClientRpcParams clientRpcParams = default)
    {
        TakeDamageInternal(newHealthValue);
    }

    private void TakeDamageInternal(float newHealthValue)
    {
        if (newHealthValue <= 0f)
        {
            OnDeath();
        }
        else
        {
            regeneratingHealth = false;
            // RecalculateHealthVignette();
            // StartCoroutine(StartHealthRegen());
        }
    }

    IEnumerator StartHealthRegen()
    {
        yield return new WaitForSeconds(healthRegenDelay);
        regeneratingHealth = true;
    }

    private void RegenHealth()
    {
        health.Value += healthRegenRatePerSecond * Time.deltaTime;
        if (health.Value >= 100f)
        {
            health.Value = 100f;
            regeneratingHealth = false;
        }

        RecalculateHealthVignette();
    }

    private void RecalculateHealthVignette()
    {
        vignette.intensity.value = (1f - health.Value / maxHealth) * vignetteMaxIntensity;
    }

    private void OnDeath()
    {
        transform.position = offScreen;
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
