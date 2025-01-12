using System.Collections;
using TMPro;
using UnityEngine;

public class RoundCounter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI counterText;
    [SerializeField] private float fadeDuration;
    [SerializeField] private float fadeAfterSeconds;

    private float _alphaValue;
    private float _fadeTime;
    
    // Start is called before the first frame update
    void Start()
    {
        _alphaValue = counterText.color.a;
        counterText.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        GameManager.NewRound.AddListener(OnNewRound);
    }
    
    private void OnDisable()
    {
        GameManager.NewRound.RemoveListener(OnNewRound);
        StopAllCoroutines();
    }

    private void OnNewRound(int roundCount)
    {
        counterText.text = "Round " + roundCount;
        counterText.gameObject.SetActive(true);
        
        StopAllCoroutines();
        StartCoroutine(nameof(FadeText));
    }

    private IEnumerator FadeText()
    {
        var textColor = counterText.color;
        counterText.color = new Color(textColor.r, textColor.g, textColor.b, _alphaValue);
        
        yield return new WaitForSeconds(fadeAfterSeconds);

        _fadeTime = 0;

        while (_fadeTime < fadeDuration)
        {
            _fadeTime += Time.deltaTime;
            counterText.color = new Color(textColor.r, textColor.g, textColor.b,
                Mathf.Lerp(_alphaValue, 0, _fadeTime / fadeDuration));
            
            yield return new WaitForEndOfFrame();
        }
        
        counterText.gameObject.SetActive(false);
    }
}
