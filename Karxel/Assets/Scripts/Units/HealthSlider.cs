using UnityEngine;
using UnityEngine.UI;

public class HealthSlider : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private Image fill;

    [Header("Color Settings")] 
    [SerializeField] private Color redFill = new (250, 6, 6);
    [SerializeField] private Color redBackground = new (144, 9, 9);
    [SerializeField] private Color blueFill = new (6, 39, 250);
    [SerializeField] private Color blueBackground = new (9, 35, 144);

    public void SetupSliderColor(Team team)
    {
        if (team == Team.Blue)
        {
            background.color = blueBackground;
            fill.color = blueFill;
        }
        else if(team == Team.Red)
        {
            background.color = redBackground;
            fill.color = redFill;
        }
    }
}
