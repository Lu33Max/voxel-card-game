using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    public static CardManager singleton;

    [SerializeField] CardData[] allCards;

    private void Start()
    {
        singleton = this;
    }

    public void DrawCard()
    {

    }
}
