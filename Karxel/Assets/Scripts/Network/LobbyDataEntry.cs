using Epic.OnlineServices.Lobby;
using UnityEngine;
using TMPro;
using Mirror;
using UnityEngine.UI;

/// <summary>
/// Data Entry of a Lobby in the Lobby List
/// </summary>
public class LobbyDataEntry : MonoBehaviour
{
    [SerializeField] private TMP_Text lobbyNameText;
    [SerializeField] private TMP_Text lobbyPlayerText;
    [SerializeField] private TMP_Text lobbyVersionText;
    [SerializeField] private Image lobbyVisibilityDisplay;
    [SerializeField] private TMP_InputField passwordInput;
    
    private LobbyDetails _lobbyDetails;
    
    private string _lobbyName;
    private string _version;
    private int _playerCount;
    private string _visibility;
    private string _password;
    
    public void SetLobbyData(LobbyDetails lobby)
    {
        _lobbyDetails = lobby;
        
        // Lobby name
        LobbyDetailsCopyAttributeByKeyOptions copyOptions = new() { AttrKey = "LobbyName" };
        lobby.CopyAttributeByKey(ref copyOptions, out var lobbyNameAttribute);
            
        if (lobbyNameAttribute.HasValue && lobbyNameAttribute.Value.Data.HasValue)
        {
            var data = lobbyNameAttribute.Value.Data.Value;
                
            _lobbyName = data.Value.AsUtf8.Length > 30
                ? data.Value.AsUtf8.ToString().Substring(0, 27).Trim() + "..."
                : data.Value.AsUtf8;
        }

        // Lobby visibility
        copyOptions.AttrKey = "Visibility";
        lobby.CopyAttributeByKey(ref copyOptions, out var lobbyVisibilityAttribute);

        if (lobbyVisibilityAttribute.HasValue && lobbyVisibilityAttribute.Value.Data.HasValue)
            _visibility = lobbyVisibilityAttribute.Value.Data.Value.Value.AsUtf8.ToString();
            
        // Lobby version
        copyOptions.AttrKey = "Version";
        lobby.CopyAttributeByKey(ref copyOptions, out var lobbyVersionAttribute);

        if (lobbyVersionAttribute.HasValue && lobbyVersionAttribute.Value.Data.HasValue)
            _version = lobbyVersionAttribute.Value.Data.Value.Value.AsUtf8.ToString();
        
        // Password version
        copyOptions.AttrKey = "Password";
        lobby.CopyAttributeByKey(ref copyOptions, out var lobbyPasswordAttribute);

        if (lobbyPasswordAttribute.HasValue && lobbyPasswordAttribute.Value.Data.HasValue)
            _password = lobbyPasswordAttribute.Value.Data.Value.Value.AsUtf8.ToString();
        
        // Lobby member count
        LobbyDetailsGetMemberCountOptions memberCountOptions = new();
        _playerCount = (int)lobby.GetMemberCount(ref memberCountOptions);
        
        UpdateDisplay();
    }

    public void JoinLobby()
    {
        if(_lobbyDetails == null || _version != Application.version)
            return;

        if ((_visibility == "1" && _password == passwordInput.text) || _visibility == "0")
            NetworkManager.singleton.GetComponent<EOSLobby>().JoinLobby(_lobbyDetails, new[] { "LobbyName" });
    }

    private void UpdateDisplay()
    {
        lobbyNameText.text = _lobbyName;
        lobbyPlayerText.text = $"{_playerCount}/8";
        
        lobbyVisibilityDisplay.gameObject.SetActive(_visibility == "1");
        passwordInput.gameObject.SetActive(_visibility == "1");

        if (Application.version != _version)
        {
            lobbyVersionText.text = _version;
            lobbyVersionText.color = Color.red;   
        }
        else
        {
            lobbyVersionText.text = "";
        }
    }
}
