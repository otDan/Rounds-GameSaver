using GameSaver.Asset;
using GameSaver.Menu;
using TMPro;
using UnboundLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Color = UnityEngine.Color;

namespace GameSaver.Component;

internal class CardDisplayMono : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private static Canvas cardPreviewCanvas;
    private static GameObject cardPreview;
    private static Image cardPreviewImage;
    private static Image cardPreviewBackgroundImage;
    private static GameObject cardObject;
    private static TextMeshProUGUI noCardText;
    private static Rect cardPreviewRect;
    public Transform container;
    public CardInfo card;
    public Color color;

    private void Start()
    {
        if (cardPreview == null)
        {
            var preview = Instantiate(AssetManager.CardPreview);
            cardPreviewCanvas = preview.GetComponent<Canvas>();
            cardPreviewCanvas.worldCamera = SaveLoadMenu.instance.gameCamera;

            cardPreview = cardPreviewCanvas.transform.GetChild(0).gameObject;
            cardPreviewRect = cardPreview.GetComponent<RectTransform>().rect;
            cardPreviewBackgroundImage = cardPreview.GetComponent<Image>();
            cardPreviewImage = cardPreview.transform.GetChild(0).GetComponent<Image>();

            noCardText = cardPreview.GetComponentInChildren<TextMeshProUGUI>();
        }

        cardPreviewCanvas.enabled = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        cardPreviewCanvas.enabled = true;
        cardPreviewImage.color = color;
        cardPreviewBackgroundImage.color = new Color(color.r * 0.45f, color.g * 0.45f, color.b * 0.45f);
        cardPreview.transform.position = transform.position;

        if (cardObject != null) Destroy(cardObject);
        if (card == null)
        {
            noCardText.enabled = true;
            return;
        }

        noCardText.enabled = false;
        cardObject = Instantiate(card.gameObject, cardPreview.transform);
        var rectTransform = cardObject.GetOrAddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);
        cardObject.transform.localPosition = new Vector3(cardPreviewRect.width / 2, -(cardPreviewRect.height / 2) - 8, 0);

        var cardVis = cardObject.GetComponentInChildren<CardVisuals>();
        cardVis.firstValueToSet = true;
        var setScaleToZero = cardObject.GetComponentInChildren<ScaleShake>();
        this.ExecuteAfterFrames(1, () =>
        {
            setScaleToZero.targetScale = 15;
        });
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        cardPreviewCanvas.enabled = false;
    }
}