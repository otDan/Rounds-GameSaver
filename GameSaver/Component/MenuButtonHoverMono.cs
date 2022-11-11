using System;
using System.IO;
using GameSaver.Asset;
using GameSaver.Menu;
using GameSaver.Util;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;

namespace GameSaver.Component
{
    public class MenuButtonHoverMono : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public SaveManager.GameInfoData gameInfoData;

        public void OnPointerEnter(PointerEventData eventData)
        {
            SaveLoadMenu.deleteObject.SetActive(true);
            SaveLoadMenu.deleteObject.transform.position = transform.position;
            var deleteButton = SaveLoadMenu.deleteObject.GetComponent<Button>();
            deleteButton.onClick.AddListener(() =>
            {
                SaveManager.DeleteGameSave(gameInfoData);
                SaveLoadMenu.deleteObject.SetActive(false);
            });
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // SaveLoadMenu.deleteObject.SetActive(false);
        }
    }
}
