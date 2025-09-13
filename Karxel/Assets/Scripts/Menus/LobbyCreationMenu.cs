using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyCreationMenu : MonoBehaviour
{
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TextMeshProUGUI visibilityToggle;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button createLobbyBtn;

    private int _visibility;
    
    private void Awake()
    {
        nameInput.text = PlayerPrefs.GetString("lobbyName", "");

        _visibility = PlayerPrefs.GetInt("lobbyVisibility", 0);
        visibilityToggle.text = _visibility == 0 ? "Public" : "Private";
        passwordInput.interactable = _visibility != 0;

        passwordInput.text = PlayerPrefs.GetString("lobbyPassword", "");

        createLobbyBtn.interactable = !string.IsNullOrWhiteSpace(nameInput.text) &&
            (_visibility == 0 || !string.IsNullOrWhiteSpace(passwordInput.text));
    }

    public void OnNameUpdated(string newName)
    {
        PlayerPrefs.SetString("lobbyName", newName);
        CheckForValidState();
    }

    public void OnVisibilityToggled()
    {
        _visibility = _visibility == 0 ? 1 : 0;
        PlayerPrefs.SetInt("lobbyVisibility", _visibility);
        
        visibilityToggle.text = _visibility == 0 ? "Public" : "Private";
        passwordInput.interactable = _visibility != 0;

        CheckForValidState();
    }

    public void OnPasswordUpdate(string newPassword)
    {
        PlayerPrefs.SetString("lobbyPassword", newPassword);
        CheckForValidState();
    }

    private void CheckForValidState()
    {
        createLobbyBtn.interactable = !string.IsNullOrWhiteSpace(nameInput.text) &&
            (_visibility == 0 || !string.IsNullOrWhiteSpace(passwordInput.text));
    }
}
