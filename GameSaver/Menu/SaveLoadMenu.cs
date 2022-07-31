using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using GameSaver.Asset;
using GameSaver.Mono;
using GameSaver.Util;
using TMPro;
using UnboundLib;
using UnboundLib.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace GameSaver.Menu
{
    internal class SaveLoadMenu : MonoBehaviour
    {
        public GameObject lobbyUi;
        public ListMenuButton listMenuButton;

        private GameObject _linksUi;
        private GameObject _pingUi;
        private GameObject _codeUi;
        private GameObject _timerUi;

        private TextMeshProUGUI _selectedText;
        private SaveManager.GameInfoData _selectedGame;
        private SaveManager.SaveData _selectedSave;

        private void Start()
        {
            gameObject.GetComponent<RectTransform>().localScale = Vector3.one;
            gameObject.GetComponent<Canvas>().worldCamera = Camera.main;

            GameObject loadButton = gameObject.transform.Find("LoadButton").gameObject;
            loadButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                var loadText = loadButton.transform.GetComponentInChildren<TextMeshProUGUI>();
                if (_selectedSave == null)
                {
                    loadText.text = "SELECT A SAVE";
                    Unbound.Instance.ExecuteAfterSeconds(1, () => { loadText.text = "LOAD"; });
                }
                else
                {
                    if (_selectedText != null) _selectedText.text = "";
                    SaveManager.SelectSave(_selectedSave);
                    _selectedText = _selectedSave.loaded;
                    _selectedSave.loaded.text = "LOADED";
                    loadText.text = "LOADED";
                    Unbound.Instance.ExecuteAfterSeconds(1, () => { loadText.text = "LOAD"; });
                }
            });

            GameObject backButton = gameObject.transform.Find("BackButton").gameObject;
            backButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                GameSaver.Instance.StartCoroutine(Swoop(lobbyUi, 0, -Screen.height * 2, true));
                GameSaver.Instance.StartCoroutine(Swoop(gameObject, 0, Screen.height * 2, false));
                
                _linksUi?.gameObject.SetActive(true);
                _pingUi?.gameObject.SetActive(true);
                _codeUi?.gameObject.SetActive(true);
                _timerUi?.gameObject.SetActive(true);
                gameObject.SetActive(false);
            });

            gameObject.SetActive(false);
        }

        public void Open()
        {
            _linksUi = GameObject.Find("Links(Clone)");
            _pingUi = GameObject.Find("UIHolder");
            _codeUi = GameObject.Find("LobbyImprovementsBG");
            _timerUi = GameObject.Find("TimerLobbyUI");
            _linksUi?.gameObject.SetActive(false);
            _pingUi?.gameObject.SetActive(false);
            _codeUi?.gameObject.SetActive(false);
            _timerUi?.gameObject.SetActive(false);

            GameSaver.Instance.StartCoroutine(Swoop(lobbyUi, 0, Screen.height * 2, true));
            GameSaver.Instance.StartCoroutine(Swoop(gameObject, 0, -Screen.height * 2, false, () =>
            {
                listMenuButton.OnPointerEnter(null);
                gameObject.SetActive(true);
                SaveManager.LoadGames();
                GameSaver.Instance.StartCoroutine(LoadButtons());
            }));
        }
        
        private IEnumerator LoadButtons()
        {
            var gameButtonContainer = transform.GetChild(0).GetChild(0).GetChild(0).GetChild(0);
            var roundButtonContainer = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0);
            var roundDisplayContainer = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(1).GetChild(0);
            var playerContainer = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);

            foreach (var gameInfoData in SaveManager.orderedGames.Where(gameInfoData => gameInfoData != null))
            {
                if (gameInfoData.rounds == 0) continue;
                if (gameInfoData.gameData.button == null)
                {
                    var gameButton = Instantiate(AssetManager.GameButton, gameButtonContainer.transform);
                    gameButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = $"{gameInfoData.gameData.startTime}";
                    gameButton.transform.GetChild(1).GetChild(0).GetComponent<TextMeshProUGUI>().text = $"ROUND\n{gameInfoData.rounds}";
                    gameButton.transform.GetChild(2).GetChild(1).GetComponent<TextMeshProUGUI>().text =  $"{gameInfoData.gameData.playerAmount}/32";
                    gameButton.transform.GetChild(3).GetComponent<TextMeshProUGUI>().text = gameInfoData.gameData.gameMode.ToUpper();

                    foreach (var saveData in gameInfoData.gameSaves.Where(saveData => saveData != null))
                    {
                        if (saveData.button == null)
                        {
                            var roundDisplay = Instantiate(AssetManager.SaveInfo, roundDisplayContainer.transform);
                            roundDisplay.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = $"ROUND {saveData.round}";
                            roundDisplay.transform.GetChild(1).GetChild(1).GetComponent<TextMeshProUGUI>().text = $"{saveData.players.Count}/32";
                            roundDisplay.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = gameInfoData.gameData.gameMode.ToUpper();
                            saveData.loaded = roundDisplay.transform.GetChild(3).GetComponent<TextMeshProUGUI>();
                            saveData.loaded.text = "";
                            saveData.display = roundDisplay;
                            roundDisplay.SetActive(false);

                            var roundButton = Instantiate(AssetManager.RoundButton, roundButtonContainer.transform);
                            var text = $"{saveData.time}".Split(' ');
                            roundButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text =
                                $"{text[0]}\n{text[1]}";
                            roundButton.transform.GetChild(1).GetChild(0).GetComponent<TextMeshProUGUI>().text =
                                $"ROUND {saveData.round}";
                            roundButton.transform.GetChild(3).GetComponent<TextMeshProUGUI>().text = Regex
                                .Replace(saveData.saveType.ToString(), "([a-z])_?([A-Z])", "$1\n$2").ToUpper();
                            
                            foreach (var playerData in saveData.players.Where(playerData => playerData != null))
                            {
                                if (playerData.display == null)
                                {
                                    var playerAsset = Instantiate(AssetManager.Player, playerContainer.transform);
                                    playerAsset.transform.GetChild(0).GetChild(0).GetChild(0).gameObject
                                        .SetActive(playerData.host);
                                    playerAsset.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text =
                                        playerData.name;
                                    playerAsset.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text =
                                        ExtraPlayerSkins.GetTeamColorName(playerData.serializedColor).ToUpper();
                                    playerAsset.transform.GetChild(2).GetComponent<TextMeshProUGUI>().color =
                                        playerData.color.color;

                                    var cardScrollView = playerAsset.transform.GetChild(1).GetChild(0).gameObject;
                                    var cardContainer = cardScrollView.transform.GetChild(0).GetChild(0);
                                    // Destroy(cardScrollView.GetComponent<ScrollRect>());
                                    // Unbound.Instance.ExecuteAfterFrames(1, () =>
                                    // {
                                    //     cardScrollView.GetOrAddComponent<CustomScrollRect>();
                                    // });
                                    foreach (var card in playerData.cards)
                                    {
                                        if (card == null)
                                        {
                                            var cardAsset = Instantiate(AssetManager.Card, cardContainer.transform);
                                            cardAsset.transform.GetComponentInChildren<TextMeshProUGUI>().text = "???";
                                            cardAsset.transform.GetComponent<Image>().color = playerData.color.color;
                                        }
                                        else
                                        {
                                            var cardAsset = Instantiate(AssetManager.Card, cardContainer.transform);
                                            cardAsset.transform.GetComponentInChildren<TextMeshProUGUI>().text =
                                                CardInitials(card);
                                            cardAsset.transform.GetComponent<Image>().color = playerData.color.color;

                                            var displayMono = cardAsset.GetOrAddComponent<CardDisplayMono>();
                                            displayMono.container = playerAsset.transform;
                                            displayMono.card = card;
                                        }
                                    }
                                    
                                    var pointContainer = playerAsset.transform.GetChild(3);
                                    for (int i = 0; i < saveData.pointsToWin; i++)
                                    {
                                        GameObject pointObject = Instantiate(AssetManager.Point, pointContainer.transform);
                                        Image image = pointObject.GetComponentInChildren<Image>();
                                        image.color = playerData.color.color;
                                        
                                        if (i == playerData.rounds)
                                        {
                                            var fillAmount = playerData.points == 0 ? 0 : (float) playerData.points / saveData.pointsToWinRound;
                                            if (fillAmount <= 0)
                                            {
                                                image.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(5, 5);
                                                image.color = Color.gray;
                                            }
                                            else
                                            {
                                                image.fillAmount = fillAmount;
                                            }
                                        }
                                        else if (i > playerData.rounds)
                                        {
                                            image.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(5, 5);
                                            image.color = Color.gray;
                                        }
                                    }

                                    playerData.display = playerAsset;
                                    playerAsset.SetActive(false);
                                }
                                else
                                {
                                    playerData.display.SetActive(false);
                                }
                            }

                            roundButton.GetComponent<Button>().onClick.AddListener(() =>
                            {
                                if (_selectedSave == saveData) return;
                                _selectedSave = saveData;
                                DisablePlayerAssets();
                                GameSaver.Instance.StartCoroutine(EnablePlayerAssets(saveData));
                            });
                            saveData.button = roundButton;
                            roundButton.SetActive(false);
                        }
                        else
                        {
                            saveData.button.SetActive(false);
                        }
                        yield return null;
                    }

                    gameButton.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        if (_selectedGame == gameInfoData) return;
                        _selectedGame = gameInfoData;
                        _selectedSave = null;
                        DisableRoundButtons();
                        DisablePlayerAssets();
                        GameSaver.Instance.StartCoroutine(EnableRoundButtons(gameInfoData));
                    });
                    gameInfoData.gameData.button = gameButton;
                }
                else
                {
                    gameInfoData.gameData.button.SetActive(true);
                }
                yield return null;
            }
            yield return null;
        }

        internal string CardInitials(CardInfo card)
        {
            string text = card.cardName;
            text = text.Substring(0, 2);
            string text2 = text[0].ToString().ToUpper();
            if (text.Length > 1)
            {
                string str = text[1].ToString().ToLower();
                text = text2 + str;
            }
            else
            {
                text = text2;
            }
            return text;
        }

        private IEnumerator EnableGameButtons()
        {
            foreach (var gameInfoData in SaveManager.orderedGames.Where(gameInfoData => gameInfoData.gameData.button != null))
            {
                gameInfoData.gameData.button.SetActive(true);
                yield return null;
            }
        }

        private static IEnumerator EnableRoundButtons(SaveManager.GameInfoData gameInfoData)
        {
            foreach (var saveData in gameInfoData.gameSaves.Where(saveData => saveData.button != null))
            {
                saveData.button.SetActive(true);
                yield return null;
            }
        }

        private static IEnumerator EnablePlayerAssets(SaveManager.SaveData saveData)
        {
            if (saveData.display != null) saveData.display.SetActive(true);
            foreach (var playerData in saveData.players.Where(playerData => playerData.display != null))
            {
                playerData.display.SetActive(true);
                yield return null;
            }
        }

        private void DisableGameButtons()
        {
            foreach (var gameInfoData in SaveManager.orderedGames)
            {
                if (gameInfoData.gameData.button == null) continue;
                gameInfoData.gameData.button.SetActive(false);
            }
        }

        private static void DisableRoundButtons()
        {
            foreach (var gameInfoData in SaveManager.orderedGames)
            {
                if (gameInfoData.gameData.button == null) continue;
                foreach (var saveData in gameInfoData.gameSaves)
                {
                    if (saveData.button == null) continue;
                    saveData.button.SetActive(false);
                }
            }
        }

        private static void DisablePlayerAssets()
        {
            foreach (var gameInfoData in SaveManager.orderedGames)
            {
                if (gameInfoData.gameData.button == null) continue;
                foreach (var saveData in gameInfoData.gameSaves)
                {
                    if (saveData.display != null) saveData.display.SetActive(false);
                    if (saveData.button == null) continue;
                    foreach (var playerData in saveData.players)
                    {
                        if (playerData.display == null) continue;
                        playerData.display.SetActive(false);
                    }
                }
            }
        }

        private static IEnumerator Swoop(GameObject obj, int moveWidth, int moveHeight, bool back, Action onFinished = null)
        {
            var rect = obj.GetComponent<RectTransform>();
            float t = 0;
            var startPos = rect.anchoredPosition;
            var endPos = back ? rect.anchoredPosition + new Vector2(moveWidth, moveHeight) : rect.anchoredPosition - new Vector2(moveWidth, 0);
            while (t < 0.25f)
            {
                t += Time.deltaTime;
                rect.anchoredPosition = Vector2.Lerp(startPos, endPos, t * 4);
                yield return null;
            }
            onFinished?.Invoke();
        }
    }
}
