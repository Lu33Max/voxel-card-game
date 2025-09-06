using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Card : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI pointText = null!;
    [SerializeField] private TextMeshProUGUI nameText = null!;
    [SerializeField] private TextMeshProUGUI valueText = null!;
    [SerializeField] private Image cardImage = null!;

    [SerializeField] private Sprite cardBGRegular = null!;
    [SerializeField] private Sprite cardBGUnselected = null!;
    [SerializeField] private Sprite cardBGRare = null!;
    [SerializeField] private Image backgroundImage = null!;

    public CardData CardData => GameManager.Instance != null &&
                                GameManager.Instance.gameState is GameState.Movement or GameState.MovementExecution
        ? _cardData.moveSide
        : _cardData.attackSide;

    private CardComponent _cardData = null!;
    private RectTransform _transform = null!;
    
    public void Initialize(CardComponent data, Vector2 startPos)
    {
        _cardData = data;
        
        _transform = GetComponent<RectTransform>();
        _transform.position = new Vector3(0, startPos.y, 0);

        SetCardDesign();
    }

    private void OnEnable()
    {
        ActionPointManager.Instance.actionPointsUpdated.AddListener(OnActionPointsUpdated);
    }

    private void OnDisable()
    {
        ActionPointManager.Instance.actionPointsUpdated.RemoveListener(OnActionPointsUpdated);
        StopAllCoroutines();
    }

    public void UpdatePosition(Vector2 newPos)
    {
        _transform.anchoredPosition = new Vector3(newPos.x, newPos.y);
    }
    
    public void UpdateYPosition(float newYPos)
    {
        _transform.anchoredPosition = new Vector3(_transform.anchoredPosition.x, newYPos);
    }

    public void UpdateState()
    {
        backgroundImage.sprite = !CanBeSelected() ? cardBGUnselected : CardData.rarity == CardData.Rarity.Common ? cardBGRegular : cardBGRare;
    }

    public void CardClickedButton()
    {
        if(!CanBeSelected()) return;
        HandManager.Instance.CardClicked(this);
    }

    public void RemoveCard()
    {
        CardManager.Instance.AddCardToUsed(_cardData);
        Destroy(gameObject);
    }
    
    public void SwapActiveFace()
    {
        StartCoroutine(RotateCard());
    }

    private void SetCardDesign()
    {
        pointText.text = CardData.cost.ToString();
        nameText.text = CardData.cardName;
        cardImage.sprite = CardData.cardSprite;
        backgroundImage.sprite = CardData.rarity == CardData.Rarity.Common ? cardBGRegular : cardBGRare;
        
        UpdateState();
    }

    private IEnumerator RotateCard()
    {
        while (transform.rotation.eulerAngles.y < 90)
        {
            transform.Rotate(new Vector3(0, 5, 0));
            yield return new WaitForEndOfFrame();
        }
        
        SetCardDesign();
        transform.rotation = Quaternion.Euler(0, 270, 0);
        
        while (transform.rotation.eulerAngles.y > 5)
        {
            transform.Rotate(new Vector3(0, 5, 0));
            yield return new WaitForEndOfFrame();
        }
        
        transform.rotation = Quaternion.Euler(0, 0, 0);
    }
    
    private bool CanBeSelected()
    {
        return ActionPointManager.ActionPoints - CardData.cost >= 0 && CardData.IsCorrectPhase();
    }

    private void OnActionPointsUpdated(int newPoints)
    {
        if (CardData != null && newPoints < CardData.cost)
            backgroundImage.sprite = cardBGUnselected;
    }
}
