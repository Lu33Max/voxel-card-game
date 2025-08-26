using System.Collections;
using TMPro;
using UnityEngine;

public class RoundCounter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI counterText;
    [SerializeField] private TextMeshProUGUI eventText;
    
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
        GameManager.Instance.NewRound += OnNewRound;
    }
    
    private void OnDisable()
    {
        GameManager.Instance.NewRound -= OnNewRound;
        StopAllCoroutines();
    }

    private void OnNewRound(int roundCount)
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
        var textColor = counterText.color;
        var newColor = new Color(textColor.r, textColor.g, textColor.b, _alphaValue);
        
        counterText.color = newColor;
        eventText.color  = newColor;
        
        yield return new WaitForSeconds(fadeAfterSeconds);

        _fadeTime = 0;

        while (_fadeTime < fadeDuration)
        {
            _fadeTime += Time.deltaTime;
            var currentColor = new Color(textColor.r, textColor.g, textColor.b,
                Mathf.Lerp(_alphaValue, 0, _fadeTime / fadeDuration));
            
            counterText.color = currentColor;
            eventText.color = currentColor;
            
            yield return new WaitForEndOfFrame();
        }
        
        counterText.gameObject.SetActive(false);
        eventText.gameObject.SetActive(false);
    }
}
