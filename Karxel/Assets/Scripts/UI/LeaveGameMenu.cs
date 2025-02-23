using Mirror;
using TMPro;
using UnityEngine;

public class LeaveGameMenu : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI leaveButtonText;

    [SerializeField] private TextMeshProUGUI clientLeaveText;
    [SerializeField] private TextMeshProUGUI hostLeaveText;

    private void Start()
    {
        if (isServer)
        {
            hostLeaveText.gameObject.SetActive(true);
        }
        else
        {
            clientLeaveText.gameObject.SetActive(true);
            leaveButtonText.text = "LEAVE LOBBY";
        }
    }

    public void OnLeaveButtonPressed()
    {
        if (isServer)
        {
            GameManager.Instance.ReturnToLobby();
        }
        else
        {
            var epicLobby = FindObjectOfType<EOSLobby>();

            if (epicLobby != null)
            {
                epicLobby.LeaveLobby();
                return;
            }
            
            NetworkManager.singleton.StopClient();
        }
    }
}
