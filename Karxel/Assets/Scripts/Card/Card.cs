using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card : MonoBehaviour
{
    [SerializeField] CardData cardData;

    public void CardClickedButton()
    {
        StartCoroutine(SelectFigure());
    }

    IEnumerator SelectFigure()
    {
        while (true)
        {
            // Warte auf einen Mausklick
            yield return new WaitUntil(() => Input.GetMouseButtonDown(0));

            // Erstelle einen Ray von der Kamera durch die Mausposition
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Überprüfe, ob der Ray ein 3D-Objekt trifft
            if (Physics.Raycast(ray, out hit))
            {
                Debug.Log("Raycast getroffen! Objekt: " + hit.collider.name);

                //Karte der Figur übergeben
                //Im UI löschen

                break; // Schleife beenden
            }
            else
            {
                Debug.Log("Kein Treffer. Warte auf den nächsten Klick.");
                yield return null;
            }
        }
    }
}
