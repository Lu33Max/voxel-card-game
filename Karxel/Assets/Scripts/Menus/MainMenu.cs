using Mirror;
using System.Collections.Generic;
using Epic.OnlineServices.Lobby;
using TMPro;
using UnityEngine;
using Attribute = Epic.OnlineServices.Lobby.Attribute;

public class MainMenu : MonoBehaviour
{
    enum HostType {
        Local,
        Epic
    }

    [SerializeField] private GameObject lobbyDataItemPrefab;
    [SerializeField] private GameObject lobbyListContent;
    [SerializeField] private TMP_InputField nameInput;

    private HostType _hostType;
    private EOSLobby _eosLobby;
    private float _lastRefreshTime;

    private void Start()
    {
        nameInput.text = PlayerPrefs.GetString("playerName", "RandomPlayer");
        
        _eosLobby = NetworkManager.singleton.GetComponent<EOSLobby>();
        
        if (_eosLobby != null)
        {
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
    
    public void CreateLobbyButton()
    {
        if (_hostType == HostType.Epic)
            _eosLobby.CreateLobby(8, LobbyPermissionLevel.Publicadvertised, true,
                new []
                {
                    new AttributeData(key: "LobbyName", value: PlayerPrefs.GetString("lobbyName")),
                    new AttributeData(key: "Version", value: Application.version),
                    new AttributeData(key: "Visibility", value: PlayerPrefs.GetInt("lobbyVisibility").ToString()),
                    new AttributeData(key: "Password", value: PlayerPrefs.GetString("lobbyPassword"))
                });
    }
    
    public void GetListOfLobbiesButton()
    {
        if(Time.time < _lastRefreshTime + 1)
            return;

        _lastRefreshTime = Time.time;
        
        if(_hostType == HostType.Epic)
            NetworkManager.singleton.GetComponent<EOSLobby>().FindLobbies();
    }
    
    public void OnPlayerNameUpdated(string newValue)
    {
        PlayerPrefs.SetString("playerName", newValue);
    }

    public void CloseGame()
    {
        Application.Quit();
    }
    
    private void DisplayLobbiesEpic(List<LobbyDetails> foundLobbies)
    {
        for(int i = 0; i < lobbyListContent.transform.childCount; i++)
            Destroy(lobbyListContent.transform.GetChild(i).gameObject);

        foreach (var lobby in foundLobbies)
        {
            GameObject createdItem = Instantiate(lobbyDataItemPrefab, lobbyListContent.transform, true);
            createdItem.transform.localScale = Vector3.one;
            
            var lobbyCard = createdItem.GetComponent<LobbyDataEntry>();
            lobbyCard.SetLobbyData(lobby);
        }
    }
    
    private void OnCreateLobbySuccess(List<Attribute> attributes) {
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
