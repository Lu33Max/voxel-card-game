using UnityEngine;
using UnityEngine.UI;

public class FlowLayoutGroup : LayoutGroup
{
    [SerializeField] private float spacing = 10f;
    [SerializeField] private float rowSpacing = 10f;

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
        ArrangeChildren();
    }

    public override void CalculateLayoutInputVertical()
    {
        ArrangeChildren();
    }

    public override void SetLayoutHorizontal()
    {
        ArrangeChildren();
    }

    public override void SetLayoutVertical()
    {
        ArrangeChildren();
    }

    private void ArrangeChildren()
    {
        float maxWidth = rectTransform.rect.width; // Breite des Containers
        float currentX = padding.left;
        float currentY = padding.top;
        float rowHeight = 0f;

        // Temporäre Liste für Zeilen
        System.Collections.Generic.List<RectTransform> currentRow = new System.Collections.Generic.List<RectTransform>();

        for (int i = 0; i < rectChildren.Count; i++)
        {
            RectTransform child = rectChildren[i];

            // Messen der Breite und Höhe des Kindes
            float childWidth = LayoutUtility.GetPreferredWidth(child);
            float childHeight = LayoutUtility.GetPreferredHeight(child);

            // Überprüfen, ob das Element in die aktuelle Zeile passt
            if (currentX + childWidth + padding.right > maxWidth && currentRow.Count > 0)
            {
                // Zeile zentrieren und zeichnen
                AlignRow(currentRow, currentX - spacing, maxWidth, currentY);

                // Neue Zeile beginnen
                currentRow.Clear();
                currentX = padding.left;
                currentY += rowHeight + rowSpacing;
                rowHeight = 0f;
            }

            // Kind zur aktuellen Zeile hinzufügen
            currentRow.Add(child);

            // Aktualisiere die Position und Höhe
            currentX += childWidth + spacing;
            rowHeight = Mathf.Max(rowHeight, childHeight);
        }

        // Letzte Zeile zentrieren und zeichnen
        if (currentRow.Count > 0)
        {
            AlignRow(currentRow, currentX - spacing, maxWidth, currentY);
        }
    }

    private void AlignRow(System.Collections.Generic.List<RectTransform> row, float totalWidth, float maxWidth, float yPosition)
    {
        // Berechne den Startpunkt, um die Zeile horizontal zu zentrieren
        float offsetX = (maxWidth - totalWidth) / 2f;

        foreach (RectTransform child in row)
        {
            float childWidth = LayoutUtility.GetPreferredWidth(child);
            float childHeight = LayoutUtility.GetPreferredHeight(child);

            // Positioniere das Kind
            SetChildAlongAxis(child, 0, offsetX);
            SetChildAlongAxis(child, 1, yPosition);

            // Verschiebe den Offset für das nächste Kind
            offsetX += childWidth + spacing;
        }
    }
}