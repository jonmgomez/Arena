using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.VisualScripting;
using UnityEngine;

public class RelayManager : MonoBehaviour
{
    private readonly Logger logger = new("RELAY");

    [Header("Debug")]
    [SerializeField] bool useRelay = true;

    public event System.Action<string> OnJoinCodeGenerated;

    // Start is called before the first frame update
    private async void Start()
    {
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            logger.Log($"Signed in as {AuthenticationService.Instance.PlayerId}");
        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async void CreateRelay(bool host = true)
    {
        try
        {
            Allocation allocation =  await RelayService.Instance.CreateAllocationAsync(3);

            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            logger.Log($"[SERVER] Created relay with code {joinCode}");

            RelayServerData serverData = new(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(serverData);

            if (host)
                NetworkManager.Singleton.StartHost();
            else
                NetworkManager.Singleton.StartServer();

            OnJoinCodeGenerated?.Invoke(joinCode);
        }
        catch (RelayServiceException e)
        {
            logger.LogException(e);
        }
    }

    public async void JoinRelay(string joinCode)
    {
        try
        {
            logger.Log($"[CLIENT] Joining relay with code {joinCode}");
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            RelayServerData serverData = new(joinAllocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(serverData);
            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            logger.LogException(e);
        }
    }

    public bool UsingRelay() => useRelay;
}
