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
    NetworkManager _networkManager;

    protected Callback<LobbyCreated_t> LobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> GameLobbyJoinRequested;
    protected Callback<LobbyEnter_t> LobbyEntered;
    protected Callback<LobbyMatchList_t> LobbyList;
    protected Callback<LobbyDataUpdate_t> LobbyDataUpdate;
    protected Callback<P2PSessionConnectFail_t> ConnectingFailed;

    public List<CSteamID> lobbyIDs = new();

    public ulong currentLobbyID;

    public Action<List<CSteamID>, LobbyDataUpdate_t> LobbyDataUpdated;

    // Start is called before the first frame update
    void Start()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogWarning("Steam is not initialized");
            return;
        }
        
        _networkManager = GetComponent<NetworkManager>();

        LobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        GameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnLobbyJoinRequested);
        LobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        LobbyList = Callback<LobbyMatchList_t>.Create(OnGetLobbyList);
        LobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(OnGetLobbyData);

        ConnectingFailed = Callback<P2PSessionConnectFail_t>.Create(OnConnectingFailed);

        MainMenu.Singleton.OnCreatingLobby += JoinGameHost;
        MainMenu.Singleton.OnGetSteamLobbyList += GetLobbiesList;
    }

    public void JoinGameHost()
    {
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, _networkManager.maxConnections);
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
    
    public void JoinLobby(CSteamID lobbyID)
    {
        SteamMatchmaking.JoinLobby(lobbyID);
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        currentLobbyID = callback.m_ulSteamIDLobby;
        Debug.Log("LOBBY - ID: " + currentLobbyID);

        if (callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogWarning("Lobby creating not worked!");
            return;
        }

        _networkManager.StartHost();
        
        SteamMatchmaking.SetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), "HostAddress", SteamUser.GetSteamID().ToString());
        SteamMatchmaking.SetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), "name", SteamFriends.GetPersonaName() + "'s Lobby");
    }

    private void OnLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        currentLobbyID = callback.m_ulSteamIDLobby;
        
        if (NetworkServer.active) 
            return;

        var hostAddress = SteamMatchmaking.GetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), "HostAddress");

        _networkManager.networkAddress = hostAddress;
        _networkManager.StartClient();
    }

    private void OnGetLobbyList(LobbyMatchList_t callback)
    {
        Debug.Log("Found " + callback.m_nLobbiesMatching + " lobbies!");

        for (int i = 0; i < callback.m_nLobbiesMatching; i++)
        {
            CSteamID lobbyID = SteamMatchmaking.GetLobbyByIndex(i);
            lobbyIDs.Add(lobbyID);

            SteamMatchmaking.RequestLobbyData(lobbyID);
        }
    }

    private void OnGetLobbyData(LobbyDataUpdate_t callback)
    {
        if (Utils.IsSceneActive(NetworkManager.singleton.offlineScene))
        {
            //MainMenu.instance.DisplayLobbies(lobbyIDs, callback);
            LobbyDataUpdated?.Invoke(lobbyIDs, callback);
        }
    }

    private void OnConnectingFailed(P2PSessionConnectFail_t callback)
    {
        Debug.LogWarning("Steam connection Error!");
    }

    private void OnDisable()
    {
        Debug.LogWarning("OnDisable");
        //QuitLobby();
        LobbyCreated.Unregister();
        GameLobbyJoinRequested.Unregister();
        LobbyEntered.Unregister();
        LobbyList.Unregister();
        LobbyDataUpdate.Unregister();

        ConnectingFailed.Unregister();
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
            _networkManager.StopHost();
        }
        else
        {
            _networkManager.StopClient();
        }
    }
}
