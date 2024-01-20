using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class Player : NetworkBehaviour
{
    [SerializeField] NetworkVariable<float> health = new(100f);
    float maxHealth = 100f;
    [SerializeField] float healthRegenDelay = 5f;
    bool regeneratingHealth = false;
    [SerializeField] float healthRegenRatePerSecond = 10f;
    Vignette vignette;
    [SerializeField] float vignetteMaxIntensity = 0.4f;

    void Start()
    {
        if (!IsOwner)
            return;

        maxHealth = health.Value;
        Volume volume = FindObjectOfType<Volume>();
        volume.profile.TryGet<Vignette>(out vignette);
        vignette.intensity.value = 0f;
    }

    void Update()
    {
        if (!IsOwner)
            return;

        if (regeneratingHealth)
            RegenHealth();
    }

    // Called from other scripts to deal damage to this player
    public void TakeDamage(float damage)
    {
        TakeDamageServerRpc(OwnerClientId, damage);
    }

    [ServerRpc(RequireOwnership = false)]
    void TakeDamageServerRpc(ulong clientId, float damage)
    {
        Debug.Log($"Player {clientId} took {damage} damage");
        health.Value -= damage;
        if (health.Value <= 0f)
        {
            Debug.Log("Player died");
            GetComponent<NetworkObject>().Despawn(true);
        }

        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[]{clientId}
            }
        };
        TakeDamageClientRpc(damage, clientRpcParams);
    }

    // Called only on the client that was hit
    [ClientRpc]
    void TakeDamageClientRpc(float damage, ClientRpcParams clientRpcParams = default)
    {
        TakeDamageInternal();
    }

    private void TakeDamageInternal()
    {
        Debug.Log("I took damage!");
        regeneratingHealth = false;
        RecalculateHealthVignette();
        StartCoroutine(StartHealthRegen());
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
}
