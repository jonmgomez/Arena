using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class HighlightTextOnMouseOver : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Color highlightColor = Color.white;
    [SerializeField] private TextMeshProUGUI text;

    private Color startColor;

    void Start()
    {
        if (text == null)
        {
            text = GetComponentInChildren<TextMeshProUGUI>();
        }

        startColor = text.color;
    }

    void OnDisable()
    {
        text.color = startColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        text.color = highlightColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        text.color = startColor;
    }
}
