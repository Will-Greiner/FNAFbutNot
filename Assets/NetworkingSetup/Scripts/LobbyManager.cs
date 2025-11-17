using Netcode.Transports;
using Steamworks;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] GameObject JoinFriendUI;
    [SerializeField] GameObject WaitForStartUI;

    private Callback<LobbyCreated_t> onLobbyCreated;
    private Callback<LobbyEnter_t> onLobbyEntered;
    private Callback<GameLobbyJoinRequested_t> onLobbyInvite;

    //SteamID contains fields in 64bit value: Universe (which Steam environment), Account Type(user, clan, lobby, game server, etc.), Instance(desktop/web/etc.), Account ID(the unique number)
    private CSteamID currentLobby;

    private const int maxLobbyMembers = 5;
    public struct FriendLobbyInfo
    {
        public CSteamID FriendId;
        public string FriendName;
        public CSteamID LobbyId;
    }

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

    /// Scans your Steam friends for those currently sitting in a joinable lobby of this game.
    public List<FriendLobbyInfo> GetJoinableFriendLobbies()
    {
        var list = new List<FriendLobbyInfo>();
        int friendCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);

        for (int i = 0; i < friendCount; i++)
        {
            var fid = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);

            FriendGameInfo_t info;
            if (SteamFriends.GetFriendGamePlayed(fid, out info))
            {
                bool sameGame = info.m_gameID.AppID() == SteamUtils.GetAppID();
                bool hasLobby = info.m_steamIDLobby.IsValid() && info.m_steamIDLobby != CSteamID.Nil;

                if (sameGame && hasLobby)
                {
                    list.Add(new FriendLobbyInfo
                    {
                        FriendId = fid,
                        FriendName = SteamFriends.GetFriendPersonaName(fid),
                        LobbyId = info.m_steamIDLobby
                    });
                }
            }
        }
        return list;
    }

    /// Call this from Join button after the player picks a friend from the list.
    public void JoinFriendLobby(CSteamID friendId)
    {
        FriendGameInfo_t info;
        if (SteamFriends.GetFriendGamePlayed(friendId, out info) && info.m_steamIDLobby.IsValid())
        {
            JoinLobby(info.m_steamIDLobby);
        }
        else
        {
            Debug.LogWarning("Friend is not in a joinable lobby for this game.");
        }
    }

    public void JoinLobby(CSteamID lobbyId)
    {
        if (lobbyId == CSteamID.Nil || !lobbyId.IsValid())
        {
            Debug.LogWarning("[Lobby] Invalid lobby ID");
            return;
        }

        // If you’re already a host/client, shut down before joining another lobby.
        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient)
        {
            Debug.Log("[Lobby] Shutting down existing Netcode session before joining...");
            NetworkManager.Singleton.Shutdown();
        }

        if (currentLobby != CSteamID.Nil)
        {
            SteamMatchmaking.LeaveLobby(currentLobby);
            currentLobby = CSteamID.Nil;
        }

        Debug.Log($"[Lobby] Joining lobby {lobbyId.m_SteamID}");
        SteamMatchmaking.JoinLobby(lobbyId); // -> will invoke OnLobbyEntered on success
        JoinFriendUI.SetActive(false);
        WaitForStartUI.SetActive(true);
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

    public void CloseLobby()
    {
        // 1) Stop Netcode (host or client)
        if (NetworkManager.Singleton != null &&
            (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient))
        {
            Debug.Log("[Lobby] Shutting down Netcode session...");
            NetworkManager.Singleton.Shutdown();
        }

        // 2) Leave the Steam lobby (this destroys it when the last person leaves)
        if (currentLobby != CSteamID.Nil)
        {
            // If we're the owner, optionally make it non-joinable before leaving
            if (SteamMatchmaking.GetLobbyOwner(currentLobby) == SteamUser.GetSteamID())
            {
                SteamMatchmaking.SetLobbyJoinable(currentLobby, false);
            }

            Debug.Log($"[Lobby] Leaving lobby {currentLobby.m_SteamID}");
            SteamMatchmaking.LeaveLobby(currentLobby);
            currentLobby = CSteamID.Nil;
        }

        // 3) Reset any lobby-specific UI
        if (WaitForStartUI != null)
            WaitForStartUI.SetActive(false);
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
