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
            lobby.LobbyDataUpdated += DisplayLobbies;
    }

    public void DisplayLobbies(List<CSteamID> lobbyIDs, LobbyDataUpdate_t callback)
    {
        for(int i = 0; i < lobbyListContent.transform.childCount; i++)
            Destroy(lobbyListContent.transform.GetChild(i).gameObject);
        
        for (int i = 0; i < lobbyIDs.Count; i++)
        {
            if (lobbyIDs[i].m_SteamID != callback.m_ulSteamIDLobby) 
                continue;
            
            GameObject createdItem = Instantiate(lobbyDataItemPrefab, lobbyListContent.transform, true);
            var lobbyCard = createdItem.GetComponent<LobbyDataEntry>();

            lobbyCard.lobbyID = (CSteamID)lobbyIDs[i].m_SteamID;
            lobbyCard.lobbyName = SteamMatchmaking.GetLobbyData((CSteamID)lobbyIDs[i].m_SteamID, "name");
            lobbyCard.SetLobbyData();

            createdItem.transform.localScale = Vector3.one;

            _listOfLobbies.Add(createdItem);
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
