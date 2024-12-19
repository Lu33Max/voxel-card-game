using Mirror;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public static MainMenu Singleton;

    public event Action OnCreatingLobby;
    public event Action OnGetSteamLobbyList;

    [SerializeField] private GameObject lobbyDataItemPrefab;
    [SerializeField] private GameObject lobbyListContent;

    private List<GameObject> _listOfLobbies = new();

    private void Awake()
    {
        Singleton = this;
    }

    private void Start()
    {
        SteamLobby lobby = NetworkManager.singleton.GetComponent<SteamLobby>();
        
        // Steam lobby will be null for testing with localhost
        if(lobby != null)
            lobby.onGetLobbyData += DisplayLobbies;
    }

    public void DisplayLobbies(List<CSteamID> _lobbyIDs, LobbyDataUpdate_t _callback)
    {
        for (int i = 0; i < _lobbyIDs.Count; i++)
        {
            if (_lobbyIDs[i].m_SteamID == _callback.m_ulSteamIDLobby)
            {

                GameObject createdItem = Instantiate(lobbyDataItemPrefab, lobbyListContent.transform, true);

                createdItem.GetComponent<LobbyDataEntry>().lobbyID = (CSteamID)_lobbyIDs[i].m_SteamID;
                createdItem.GetComponent<LobbyDataEntry>().lobbyName = SteamMatchmaking.GetLobbyData((CSteamID)_lobbyIDs[i].m_SteamID, "name");
                createdItem.GetComponent<LobbyDataEntry>().SetLobbyData();

                createdItem.transform.localScale = Vector3.one;

                _listOfLobbies.Add(createdItem);
            }
        }
    }
    public void CreateLobbyButton()
    {
        OnCreatingLobby?.Invoke();
    }


    public void GetListOfLobbiesButton()
    {
        OnGetSteamLobbyList?.Invoke();
    }

    public void CloseGame()
    {
        Application.Quit();
    }
}
