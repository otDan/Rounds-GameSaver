using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnboundLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameSaver.Mono
{
    internal class CardDisplayMono : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private static GameObject _cardPreview;
        public Transform container;
        public CardInfo card;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_cardPreview != null) Destroy(_cardPreview);

            _cardPreview = Instantiate(card.gameObject);
            var rectTransform = _cardPreview.GetOrAddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(0, 0);

            var cardVis = _cardPreview.GetComponentInChildren<CardVisuals>();
            cardVis.firstValueToSet = true;
            _cardPreview.transform.localPosition = Vector3.zero;
            _cardPreview.GetComponentInChildren<Canvas>().sortingLayerName = "MostFront";
            _cardPreview.GetComponentInChildren<GraphicRaycaster>().enabled = false;
            _cardPreview.GetComponentInChildren<SetScaleToZero>().enabled = false;
            _cardPreview.GetComponentInChildren<SetScaleToZero>().transform.localScale = Vector3.one * 0.25f;
            this.ExecuteAfterFrames(1, () => { 
                if (_cardPreview == null) return;
                _cardPreview.transform.localScale = Vector3.one * 0.5f;
                int y = 4;
                if (eventData.position.y > Screen.height/2 ) y = -5;
                _cardPreview.transform.position = transform.position + new Vector3(4, y);
            });
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_cardPreview != null) Destroy(_cardPreview);
        }
    }
}
