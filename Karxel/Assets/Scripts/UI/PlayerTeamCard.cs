using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerTeamCard : MonoBehaviour
{
    [SerializeField] private Sprite readyIcon;
    [SerializeField] private Sprite unreadyIcon;
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private Image image;

    public void Initialize(string playerName, bool isReady)
    {
        text.text = playerName;
        image.sprite = isReady ? readyIcon : unreadyIcon;
    }
}
