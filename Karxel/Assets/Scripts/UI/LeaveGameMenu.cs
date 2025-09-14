using Mirror;
using TMPro;
using UnityEngine;

public class LeaveGameMenu : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI clientLeaveText;
    [SerializeField] private TextMeshProUGUI hostLeaveText;

    private void Start()
    {
        if (isServer)
            hostLeaveText.gameObject.SetActive(true);
        else
            clientLeaveText.gameObject.SetActive(true);
    }

    public void OnLeaveButtonPressed()
    {
        if (isServer)
        {
            GameManager.Instance!.ReturnToLobby();
        }
        else
        {
            var epicLobby = FindAnyObjectByType<EOSLobby>();

            if (epicLobby)
            {
                epicLobby.LeaveLobby();
                return;
            }
            
            NetworkManager.singleton.StopClient();
        }
    }
}
