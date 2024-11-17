using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Steamworks;
using System;

/// <summary>
/// Steam Lobbies
/// Creating, Quitting, Lobby List of Steam Lobbies
/// Steam Connecting / Disconnecting and Lobby Creation
/// </summary>
public class SteamLobby : MonoBehaviour
{
    public static SteamLobby sinleton;

    NetworkManager networkManager;

    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    protected Callback<LobbyEnter_t> lobbyEntered;
    protected Callback<LobbyMatchList_t> lobbyList;
    protected Callback<LobbyDataUpdate_t> lobbyDataUpdate;

    protected Callback<P2PSessionConnectFail_t> connectingFailed;

    public List<CSteamID> lobbyIDs = new List<CSteamID>();

    public ulong currentLobbyID;

    public Action<List<CSteamID>, LobbyDataUpdate_t> onGetLobbyData;

    // Start is called before the first frame update
    void Start()
    {
        networkManager = GetComponent<NetworkManager>();
        sinleton = this;

        if (!SteamManager.Initialized)
        {
            Debug.LogWarning("Steam is not initialized");
            return;
        }

        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnLobbyJoinRequested);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        lobbyList = Callback<LobbyMatchList_t>.Create(OnGetLobbyList);
        lobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(OnGetLobbyData);

        connectingFailed = Callback<P2PSessionConnectFail_t>.Create(OnConnectingFailed);
    }

    public void JoinGameHost()
    {
        //networkManager.StartHost();

        if (true)
        {
            SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, networkManager.maxConnections);
        }
        else
        {
            SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePrivate, networkManager.maxConnections);
        }

        Debug.Log("Lobby created");
    }

    public void GetLobbiesList()
    {
        if (lobbyIDs.Count > 0)
        {
            lobbyIDs.Clear();
        }

        SteamMatchmaking.AddRequestLobbyListResultCountFilter(50);
        SteamMatchmaking.RequestLobbyList();
    }

    void OnLobbyCreated(LobbyCreated_t callback)
    {
        currentLobbyID = callback.m_ulSteamIDLobby;
        Debug.Log("LOBBY - ID: " + currentLobbyID);

        if (callback.m_eResult != EResult.k_EResultOK)   //Wenn Lobby createn nicht funktioniert hat
        {
            Debug.LogWarning("Lobby creating not worked!");
            return;
        }

        networkManager.StartHost();

        SteamMatchmaking.SetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), "HostAddress", SteamUser.GetSteamID().ToString());
        SteamMatchmaking.SetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), "name", SteamFriends.GetPersonaName().ToString() + "'s Lobby");
    }

    void OnLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    void OnLobbyEntered(LobbyEnter_t callback)
    {
        if (NetworkServer.active) return;

        string hostAddress = SteamMatchmaking.GetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), "HostAddress");

        networkManager.networkAddress = hostAddress;
        networkManager.StartClient();

        currentLobbyID = callback.m_ulSteamIDLobby;
    }

    void OnGetLobbyList(LobbyMatchList_t callback)
    {
        Debug.Log("Found " + callback.m_nLobbiesMatching + " lobbies!");

        for (int i = 0; i < callback.m_nLobbiesMatching; i++)
        {
            CSteamID lobbyID = SteamMatchmaking.GetLobbyByIndex(i);
            lobbyIDs.Add(lobbyID);

            SteamMatchmaking.RequestLobbyData(lobbyID);
        }
    }

    void OnGetLobbyData(LobbyDataUpdate_t callback)
    {
        if (Utils.IsSceneActive(NetworkManager.singleton.offlineScene))
        {
            //MainMenu.instance.DisplayLobbies(lobbyIDs, callback);
            onGetLobbyData?.Invoke(lobbyIDs, callback);
        }
    }

    public void JoinLobby(CSteamID _lobbyID)
    {
        SteamMatchmaking.JoinLobby(_lobbyID);
    }

    void OnConnectingFailed(P2PSessionConnectFail_t callback)
    {
        Debug.LogWarning("Steam connection Error!");
    }

    private void OnDisable()
    {
        Debug.LogWarning("OnDisable");
        //QuitLobby();
        lobbyCreated.Unregister();
        gameLobbyJoinRequested.Unregister();
        lobbyEntered.Unregister();
        lobbyList.Unregister();
        lobbyDataUpdate.Unregister();

        connectingFailed.Unregister();
    }

    public void QuitLobby()
    {
        SteamMatchmaking.LeaveLobby(new CSteamID(currentLobbyID)); //Die Zeile wirft Error OnDisable

        Invoke(nameof(DisconnectPlayer), 0.2f);
    }

    void DisconnectPlayer()
    {
        if (NetworkClient.activeHost)
        {
            networkManager.StopHost();
        }
        else
        {
            networkManager.StopClient();
        }
    }
}
