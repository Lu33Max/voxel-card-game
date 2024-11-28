using Mirror;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public static MainMenu singleton;

    public event Action OnGetSteamLobbyList;

    [SerializeField] GameObject lobbyDataItemPrefab;
    [SerializeField] GameObject lobbyListContent;

    List<GameObject> listOfLobbies = new List<GameObject>();

    [SerializeField] GameObject lobbyListLoading;

    private void Awake()
    {
        singleton = this;

        NetworkManager.singleton.GetComponent<SteamLobby>().onGetLobbyData += DisplayLobbies;
    }

    public void DisplayLobbies(List<CSteamID> _lobbyIDs, LobbyDataUpdate_t _callback)
    {
        for (int i = 0; i < _lobbyIDs.Count; i++)
        {
            if (_lobbyIDs[i].m_SteamID == _callback.m_ulSteamIDLobby)
            {

                GameObject createdItem = Instantiate(lobbyDataItemPrefab);

                createdItem.GetComponent<LobbyDataEntry>().lobbyID = (CSteamID)_lobbyIDs[i].m_SteamID;
                createdItem.GetComponent<LobbyDataEntry>().lobbyName = SteamMatchmaking.GetLobbyData((CSteamID)_lobbyIDs[i].m_SteamID, "name");

                createdItem.GetComponent<LobbyDataEntry>().SetLobbyData();

                createdItem.transform.SetParent(lobbyListContent.transform);
                createdItem.transform.localScale = Vector3.one;

                listOfLobbies.Add(createdItem);

            }
        }

        lobbyListLoading.SetActive(false);
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
