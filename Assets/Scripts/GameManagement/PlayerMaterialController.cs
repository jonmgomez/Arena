using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerMaterialController : NetworkBehaviour
{
    [SerializeField] private Material[] materials;

    private readonly Dictionary<ulong, int> playerMaterials = new();
    private readonly List<Material> availableMaterials = new();

    void Awake()
    {
        availableMaterials.AddRange(materials);
    }

    public void GetMaterial(ulong clientId)
    {
        RequestMaterialServerRpc(Net.LocalClientId, clientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestMaterialServerRpc(ulong requestClientId, ulong clientId)
    {
        Debug.Assert(availableMaterials.Count > 0, "No materials available to choose from");
        Debug.Log($"Requesting material for client {clientId}");

        if (playerMaterials.ContainsKey(clientId))
        {
            SetMaterialForPlayerClientRpc(clientId, playerMaterials[clientId], Utility.SendToOneClient(requestClientId));
            return;
        }

        int materialIndex = Random.Range(0, availableMaterials.Count);
        Material chosenMaterial = availableMaterials[materialIndex];
        availableMaterials.Remove(chosenMaterial);

        materialIndex = System.Array.IndexOf(materials, chosenMaterial);

        playerMaterials.Add(clientId, materialIndex);

        SetMaterialForPlayerClientRpc(clientId, materialIndex, Utility.SendToOneClient(requestClientId));
    }

    [ClientRpc]
    private void SetMaterialForPlayerClientRpc(ulong clientId, int materialIndex, ClientRpcParams clientRpcParams = default)
    {
        Player player = GameState.Instance.GetPlayer(clientId);

        if (player == null)
        {
            Debug.LogWarning($"Player with clientId {clientId} not found");
            return;
        }

        player.SetMaterial(materials[materialIndex]);
    }
}
