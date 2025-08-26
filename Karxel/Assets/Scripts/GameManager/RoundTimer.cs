using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary> Handles the display and sound effects of the countdown timer during rounds </summary>
[RequireComponent(typeof(AudioSource))]
public class RoundTimer : MonoBehaviour
{
    [Header("Countdown Display")]
    [SerializeField, Tooltip("Reference to the text display element showing the remaining seconds")] 
    private TextMeshProUGUI timerText;
    [SerializeField, Tooltip("Number of seconds at which the countdown starts playing")] 
    private int countdownStart;
    [SerializeField, Tooltip("Number of seconds at which the countdown sfx gets swapped")] 
    private int countdownDramatic;
    
    [Header("Countdown SFX")]
    [SerializeField, Tooltip("Regular sfx that gets played on every second during the countdown")] 
    private AudioClip timerRegular;
    [SerializeField, Tooltip("Sfx that gets played during the last few seconds of the countdown")] 
    private AudioClip timerEnding;
    [SerializeField, Tooltip("Increase in pitch for every sfx that gets played")] 
    private float pitchIncrease = 0.02f;
    
    [Header("Phase Display")]
    [SerializeField] private List<Image> phaseDisplays;
    [SerializeField] private Sprite moveSprite;
    [SerializeField] private Sprite attackSprite;
    
    /// <summary> AudioSource used to play all countdown sfx from </summary>
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

    /// <summary> Updates the timer display every frame the remaining time gets updated in the GameManager </summary>
    /// <param name="newTime"> Newly remaining time in seconds </param>
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

    /// <summary> Plays phase-switch sfx whenever the move or attack phase begins and switch phase icons </summary>
    private void OnGameStateChanged(GameState newState)
    {
        if(newState is not GameState.Attack and not GameState.Movement) 
            return;

        foreach (var display in phaseDisplays)
            display.sprite = newState == GameState.Attack ? attackSprite : moveSprite;
        
        AudioManager.PlaySfx(_timerAudio, timerEnding, 1.2f, 1.2f);
        AudioManager.PlaySfx(_timerAudio, timerEnding, 1.22f, 1.22f);
    }
}
