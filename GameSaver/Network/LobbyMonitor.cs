using System.Collections.Generic;
using System.Linq;
using GameSaver.Asset;
using GameSaver.Menu;
using GameSaver.Util;
using Photon.Pun;
using RWF.UI;
using Steamworks;
using TMPro;
using UnboundLib;
using UnboundLib.Networking;
using UnityEngine;
using UnityEngine.UI;

namespace GameSaver.Network
{
    public class LobbyMonitor : MonoBehaviourPunCallbacks
    {
        public static LobbyMonitor instance { get; private set; }
        private static bool _enabled;
        
        // public static List<ulong> steamIds = new();

        private void Awake()
        {
            instance = this;
        }

        private void Start()
        {

        }

        private void Update()
        {
            if (!_enabled) return;
        }

        public override void OnCreatedRoom() { }

        public override void OnJoinedRoom()
        {
            if (PhotonNetwork.OfflineMode) return;
            Unbound.Instance.ExecuteAfterFrames(1, LoadSaveButton);
            Unbound.Instance.ExecuteAfterSeconds(2, () =>
            {
                var steamId = SteamUser.GetSteamID().m_SteamID;
                SendSteamId(PhotonNetwork.LocalPlayer.ActorNumber, steamId);
            });
        }

        public override void OnLeftRoom()
        {
            _enabled = false;
        }

        public static void SendSteamId(int player, ulong steamId)
        {
            NetworkingManager.RPC(typeof(LobbyMonitor), nameof(SyncSteamId), player, steamId.ToString());
        }

        [UnboundRPC]
        private static void SyncSteamId(int player, string serializedSteamId)
        {
            var steamId = ulong.Parse(serializedSteamId);
            Util.SteamManager.steamIds.Add(player, steamId);
            GameSaver.Instance.Log($"Joined steam id {steamId}");
        }

        public void LoadSaveButton()
        {
            if (!PhotonNetwork.IsMasterClient) return;

            var lobbyMenu = GameObject.Find("PrivateRoom/Main/Group").gameObject;

            var loadSaveObject = new GameObject("LOAD_SAVE");
            loadSaveObject.transform.SetParent(lobbyMenu.transform);
            loadSaveObject.transform.localScale = Vector3.one;
            loadSaveObject.transform.SetSiblingIndex(loadSaveObject.transform.GetSiblingIndex() - 1);

            var loadSaveText = GetText("LOAD");
            loadSaveText.transform.SetParent(loadSaveObject.transform);
            loadSaveText.transform.localScale = Vector3.one;

            loadSaveObject.AddComponent<RectTransform>();
            loadSaveObject.AddComponent<CanvasRenderer>();
            var loadSaveLayout = loadSaveObject.AddComponent<LayoutElement>();
            loadSaveLayout.minHeight = 92;
            var backListButton = loadSaveObject.AddComponent<ListMenuButton>();
            backListButton.setBarHeight = 92f;
            
            
            var saveMenuAsset = Instantiate(AssetManager.ElementSection);
            var saveLoadMenu = saveMenuAsset.AddComponent<SaveLoadMenu>();
            saveLoadMenu.lobbyUi = lobbyMenu;
            saveLoadMenu.listMenuButton = backListButton;
            
            // menuManager.Init();

            var loadSaveButton = loadSaveObject.AddComponent<Button>();
            loadSaveButton.onClick.AddListener(() => { saveLoadMenu.Open(); });

            var loadSaveList = loadSaveObject.AddComponent<ListMenuButton>();
            loadSaveList.setBarHeight = 92f;

            _enabled = true;
        }

        public GameObject GetText(string str)
        {
            var textGo = new GameObject("Text");

            textGo.AddComponent<CanvasRenderer>();
            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = str;
            text.color = new Color32(230, 230, 230, 255);
            text.font = MenuFont;
            text.fontSize = 60;
            text.fontWeight = FontWeight.Regular;
            text.alignment = TextAlignmentOptions.Center;
            text.rectTransform.sizeDelta = new Vector2(2050, 92);

            return textGo;
        }

        private static TMP_FontAsset _menuFont;
        public static TMP_FontAsset MenuFont
        {
            get
            {
                if (_menuFont || !MainMenuHandler.instance) return _menuFont;
                var localGo = MainMenuHandler.instance.transform.Find("Canvas/ListSelector/Main/Group/Local").gameObject;
                _menuFont = localGo.GetComponentInChildren<TextMeshProUGUI>().font;

                return _menuFont;
            }
        }
    }
}
