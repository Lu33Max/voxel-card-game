using System.Collections;
using TMPro;
using UnityEngine;

/// <summary> Responsible for displaying the current round count and a countdown to the next <see cref="StageEventInstance"/> </summary>
public class RoundCounter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI counterText = null!;
    [SerializeField] private TextMeshProUGUI eventText = null!;
    
    [SerializeField] private float fadeDuration;
    [SerializeField] private float fadeAfterSeconds;

    [SerializeField] private int maxEventPreWarnRounds;

    private float _alphaValue;
    private float _fadeTime;
    
    private void Start()
    {
        _alphaValue = counterText.color.a;
        counterText.gameObject.SetActive(false);
        eventText.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        GameManager.Instance!.NewRound += HandleNewRound;
    }
    
    private void OnDisable()
    {
        GameManager.Instance!.NewRound -= HandleNewRound;
        StopAllCoroutines();
    }

    private void HandleNewRound(int roundCount)
    {
        counterText.text = $"Round {roundCount}";
        counterText.gameObject.SetActive(true);

        var nextEvent = StageEventManager.Instance ? StageEventManager.Instance.GetNextEvent(roundCount) : null;

        if (nextEvent != null && nextEvent.triggerRound <= roundCount + maxEventPreWarnRounds)
        {
            eventText.text = $"{nextEvent.parameters.name} starting in {nextEvent.triggerRound - roundCount} rounds";
            eventText.gameObject.SetActive(true);
        }
        
        StopAllCoroutines();
        StartCoroutine(nameof(FadeText));
    }

    private IEnumerator FadeText()
    {
        counterText.color = new Color(counterText.color.r, counterText.color.g, counterText.color.b, _alphaValue);
        eventText.color  = new Color(eventText.color.r, eventText.color.g, eventText.color.b, _alphaValue);
        
        yield return new WaitForSeconds(fadeAfterSeconds);

        _fadeTime = 0;

        while (_fadeTime < fadeDuration)
        {
            _fadeTime += Time.deltaTime;
            var alpha = Mathf.Lerp(_alphaValue, 0, _fadeTime / fadeDuration);
            
            counterText.color = new Color(counterText.color.r, counterText.color.g, counterText.color.b, alpha);
            eventText.color = new Color(eventText.color.r, eventText.color.g, eventText.color.b, alpha);;
            
            yield return new WaitForEndOfFrame();
        }
        
        counterText.gameObject.SetActive(false);
        eventText.gameObject.SetActive(false);
    }
}
