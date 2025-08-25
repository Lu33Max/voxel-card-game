using TMPro;
using UnityEngine;

/// <summary> Handles the display and sound effects of the countdown timer during rounds </summary>
[RequireComponent(typeof(AudioSource))]
public class RoundTimer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private int countdownStart;
    [SerializeField] private int countdownDramatic;
    
    [SerializeField] private AudioClip timerRegular;
    [SerializeField] private AudioClip timerEnding;
    [SerializeField] private float pitchIncrease = 0.02f;
    
    private AudioSource _timerAudio;

    private void Start()
    {
        _timerAudio = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        GameManager.Instance.TimerUpdated += OnTimerUpdated;
        GameManager.Instance.GameStateChanged += OnGameStateChanged;
    }
    
    private void OnDisable()
    {
        GameManager.Instance.TimerUpdated -= OnTimerUpdated;
        GameManager.Instance.GameStateChanged -= OnGameStateChanged;
    }

    private void OnTimerUpdated(float newTime)
    {
        var totalSeconds = Mathf.FloorToInt(newTime);
        var minuteDisplay = Mathf.FloorToInt(totalSeconds / 60f);
        var secondDisplay = (totalSeconds - minuteDisplay * 60).ToString().PadLeft(2, '0');
        var newText = $"{minuteDisplay}:{secondDisplay}";

        if (newTime <= countdownStart + 1 && newTime > 0 && timerText.text != newText)
        {
            var pitch = 1 + (countdownStart - totalSeconds) * pitchIncrease;
            AudioManager.PlaySfx(_timerAudio, totalSeconds > countdownDramatic ? timerRegular : timerEnding, pitch, pitch);
        }
        
        timerText.color = newTime > countdownStart + 1 ? Color.white : Color.red;
        timerText.text = newText;
    }

    private void OnGameStateChanged(GameState newState)
    {
        if(newState is not GameState.Attack and not GameState.Movement) 
            return;
        
        AudioManager.PlaySfx(_timerAudio, timerEnding, 1.2f, 1.2f);
        AudioManager.PlaySfx(_timerAudio, timerEnding, 1.22f, 1.22f);
    }
}
