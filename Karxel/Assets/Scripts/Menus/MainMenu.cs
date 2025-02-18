using Mirror;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using Epic.OnlineServices.Lobby;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using Attribute = Epic.OnlineServices.Lobby.Attribute;

public class MainMenu : MonoBehaviour
{
    enum HostType {
        Local,
        Steam,
        Epic
    }
    
    public static MainMenu Singleton;
    public event Action OnCreatingLobby;
    public event Action OnGetSteamLobbyList;

    [SerializeField] private GameObject lobbyDataItemPrefab;
    [SerializeField] private GameObject lobbyListContent;

    private HostType _hostType;
    private EOSLobby _eosLobby;

    private void Awake()
    {
        Singleton = this;
    }

    private void Start()
    {
        SteamLobby steamLobby = NetworkManager.singleton.GetComponent<SteamLobby>();
        AudioManager.Instance.PlayMusic(AudioManager.Instance.MenuMusic);
        
        // Steam lobby will be null for testing with localhost
        if (steamLobby != null)
        {
            steamLobby.LobbyDataUpdated += DisplayLobbiesSteam;
            _hostType = HostType.Steam;
            return;
        }

        EOSLobby epicLobby = NetworkManager.singleton.GetComponent<EOSLobby>();
        
        if (epicLobby != null)
        {
            _eosLobby = epicLobby;
            epicLobby.FindLobbiesSucceeded += DisplayLobbiesEpic;
            _hostType = HostType.Epic;
            
            _eosLobby.CreateLobbySucceeded += OnCreateLobbySuccess;
            _eosLobby.JoinLobbySucceeded += OnJoinLobbySuccess;
            _eosLobby.FindLobbiesSucceeded += DisplayLobbiesEpic;
            return;
        }

        _hostType = HostType.Local;
    }

    //deregister events
    private void OnDisable() {
        if(_hostType != HostType.Epic)
            return;
        
        //unsubscribe from events
        _eosLobby.CreateLobbySucceeded -= OnCreateLobbySuccess;
        _eosLobby.JoinLobbySucceeded -= OnJoinLobbySuccess;
        _eosLobby.FindLobbiesSucceeded -= DisplayLobbiesEpic;
    }

    public void DisplayLobbiesSteam(List<CSteamID> lobbyIDs, LobbyDataUpdate_t callback)
    {
        for(int i = 0; i < lobbyListContent.transform.childCount; i++)
            Destroy(lobbyListContent.transform.GetChild(i).gameObject);
        
        for (int i = 0; i < lobbyIDs.Count; i++)
        {
            if (lobbyIDs[i].m_SteamID != callback.m_ulSteamIDLobby) 
                continue;
            
            GameObject createdItem = Instantiate(lobbyDataItemPrefab, lobbyListContent.transform, true);
            var lobbyCard = createdItem.GetComponent<LobbyDataEntry>();

            lobbyCard.steamLobbyID = (CSteamID)lobbyIDs[i].m_SteamID;
            lobbyCard.lobbyName = SteamMatchmaking.GetLobbyData((CSteamID)lobbyIDs[i].m_SteamID, "name");
            lobbyCard.SetLobbyData();

            createdItem.transform.localScale = Vector3.one;
        }
    }

    public void DisplayLobbiesEpic(List<LobbyDetails> foundLobbies)
    {
        for(int i = 0; i < lobbyListContent.transform.childCount; i++)
            Destroy(lobbyListContent.transform.GetChild(i).gameObject);

        foreach (var lobby in foundLobbies)
        {
            Attribute? lobbyNameAttribute = new Attribute();
            LobbyDetailsCopyAttributeByKeyOptions copyOptions = new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = "LobbyName" };
            lobby.CopyAttributeByKey(ref copyOptions, out lobbyNameAttribute);
            
            GameObject createdItem = Instantiate(lobbyDataItemPrefab, lobbyListContent.transform, true);
            var lobbyCard = createdItem.GetComponent<LobbyDataEntry>();

            if (lobbyNameAttribute.HasValue && lobbyNameAttribute.Value.Data.HasValue)
            {
                var data = lobbyNameAttribute.Value.Data.Value;
                
                lobbyCard.lobbyName = data.Value.AsUtf8.Length > 30
                    ? data.Value.AsUtf8.ToString().Substring(0, 27).Trim() + "..."
                    : data.Value.AsUtf8;
            }
            
            lobbyCard.SetLobbyData();

            createdItem.transform.localScale = Vector3.one;
        }
    }
    
    public void CreateLobbyButton()
    {
        if(_hostType == HostType.Steam)
            OnCreatingLobby?.Invoke();
        else if (_hostType == HostType.Epic)
            _eosLobby.CreateLobby(8, LobbyPermissionLevel.Publicadvertised, false,
                new []
                {
                    new AttributeData
                    {
                        Key = "LobbyName", Value = "Test Lobby"
                    },
                });
    }
    
    public void GetListOfLobbiesButton()
    {
        if(_hostType == HostType.Steam)
            OnGetSteamLobbyList?.Invoke();
        else if(_hostType == HostType.Epic)
            NetworkManager.singleton.GetComponent<EOSLobby>().FindLobbies();
    }

    public void CloseGame()
    {
        Application.Quit();
    }
    
    private void OnCreateLobbySuccess(List<Attribute> attributes) {
        Debug.Log("Starting host");
        NetworkManager.singleton.StartHost();
    }

    //when the user joined the lobby successfully, set network address and connect
    private void OnJoinLobbySuccess(List<Attribute> attributes) {
        Attribute hostAddressAttribute = attributes.Find((x) => x.Data.HasValue && x.Data.Value.Key == EOSLobby.hostAddressKey);
        if (!hostAddressAttribute.Data.HasValue)
        {
            Debug.LogError("Host address not found in lobby attributes. Cannot connect to host.");
            return;
        }

        NetworkManager.singleton.networkAddress = hostAddressAttribute.Data.Value.Value.AsUtf8;
        NetworkManager.singleton.StartClient();
    }
}
