using EasyTextEffects;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TextButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Button Settings")]
    [SerializeField] private Color buttonColor;
    [SerializeField] private string? buttonText;
    
    [Header("Events")]
    public UnityEvent? onButtonClicked;
    
    private Button _button = null!;
    private TextEffect _textEffect = null!;
    
    private void Awake()
    {
        _button = GetComponentInChildren<Button>();
        _textEffect = GetComponentInChildren<TextEffect>();
    }

    private void OnEnable()
    {
        _button.onClick.AddListener(HandleButtonClicked);
    }

    private void OnDisable()
    {
        _button.onClick.RemoveListener(HandleButtonClicked);
    }

    private void OnValidate()
    {
        GetComponentsInChildren<Image>()[1].color = buttonColor;
        GetComponentInChildren<TextMeshProUGUI>().text = buttonText;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(_button.IsInteractable())
            _textEffect.StartManualEffects();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _textEffect.StopManualEffects();
    }

    private void HandleButtonClicked() => onButtonClicked?.Invoke();
}
