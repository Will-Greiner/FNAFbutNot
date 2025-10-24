using Netcode.Transports;
using Steamworks;
using Unity.Netcode;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{


    private Callback<LobbyCreated_t> onLobbyCreated;
    private Callback<LobbyEnter_t> onLobbyEntered;
    private Callback<GameLobbyJoinRequested_t> onLobbyInvite;

    //SteamID contains fields in 64bit value: Universe (which Steam environment), Account Type(user, clan, lobby, game server, etc.), Instance(desktop/web/etc.), Account ID(the unique number)
    private CSteamID currentLobby;

    private const int maxLobbyMembers = 5;

    private void Awake()
    {
        onLobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        onLobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        onLobbyInvite = Callback<GameLobbyJoinRequested_t>.Create(OnLobbyInvite);
    }

    public void Host()
    {
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, maxLobbyMembers);
    }

    private void OnLobbyCreated(LobbyCreated_t data)
    {
        //Display error if it couldn't successfully complete the callback
        if (data.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError("Failed to create lobby");
            return;
        }

        // Set the lobby data so friends can see/join via steam overlay
        currentLobby = new CSteamID(data.m_ulSteamIDLobby);
        SteamMatchmaking.SetLobbyData(currentLobby, "name", SteamFriends.GetPersonaName() + "'s Lobby");

        // Allow players to join lobby
        SteamMatchmaking.SetLobbyJoinable(currentLobby, true);

        bool ok = NetworkManager.Singleton.StartHost();
        Debug.Log(ok ? "[NGO] StartHost OK" : "[NGO] StartHost FAILED");

        var nm = NetworkManager.Singleton;
        nm.OnServerStarted += () => Debug.Log("[NGO] SERVER STARTED");
        nm.OnClientConnectedCallback += id => Debug.Log($"[NGO] CLIENT CONNECTED: {id}");
        nm.OnClientDisconnectCallback += id => Debug.Log($"[NGO] CLIENT DISCONNECTED: {id}");
    }

    private void OnLobbyEntered(LobbyEnter_t data)
    {
        currentLobby = new CSteamID(data.m_ulSteamIDLobby);

        if (!NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient)
        {
            // Tell SteamSocketsTransport who to connect to (host SteamID)
            var transport = (SteamNetworkingSocketsTransport)
                NetworkManager.Singleton.NetworkConfig.NetworkTransport;
            transport.ConnectToSteamID = (ulong)SteamMatchmaking.GetLobbyOwner(currentLobby);

            bool ok = NetworkManager.Singleton.StartClient();
            Debug.Log(ok ? "[NGO] StartClient OK" : "[NGO] StartClient FAILED");
        }
    }

    private void OnLobbyInvite(GameLobbyJoinRequested_t data)
    {
        //Handles Steam overlay "Join Game" invites -> triggers OnLobbyEntered on this client
        SteamMatchmaking.JoinLobby(data.m_steamIDLobby);
    }

    public void OpenInviteOveraly()
    {
        if (!SteamAPI.IsSteamRunning())
        {
            Debug.LogWarning("Steam is not running; cannot open invite overlay.");
            return;
        }
        if (!SteamUtils.IsOverlayEnabled())
        {
            Debug.LogWarning("Steam Overlay is disabled; enable it in Steam settings.");
            return;
        }
        if (currentLobby == CSteamID.Nil)
        {
            Debug.LogWarning("No active lobby; create or join a lobby first.");
            return;
        }

        SteamFriends.ActivateGameOverlayInviteDialog(currentLobby);
    }
}
