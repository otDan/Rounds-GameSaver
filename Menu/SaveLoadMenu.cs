using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GameSaver.Asset;
using GameSaver.Component;
using GameSaver.Util;
using GameSaver.Util.Web;
using TMPro;
using UnboundLib;
using UnboundLib.GameModes;
using UnboundLib.Utils;
using UnityEngine;
using UnityEngine.UI;
using static GameSaver.Util.SaveManager;

namespace GameSaver.Menu;

internal class SaveLoadMenu : MonoBehaviour
{
    public static SaveLoadMenu instance;

    public Camera gameCamera;

    private Canvas canvas;
    private CanvasGroup canvasGroup;

    public GameObject lobbyUi;
    public ListMenuButton listMenuButton;

    private bool open; // false

    private GameObject _linksUi;
    private GameObject _pingUi;
    private GameObject _codeUi;
    private GameObject _timerUi;
    private GameObject _roundUi;

    private TextMeshProUGUI _selectedText;
    private GameInfoData _selectedGame;
    private SaveData _selectedSave;

    public static GameObject deleteObject;

    private List<Coroutine> menuCoroutines = new();

    private TextMeshProUGUI savedGamesText;
    private int lastGameCount = -1;

    private void Start()
    {
        instance = this;
        gameCamera = Camera.main;
        deleteObject = Instantiate(AssetManager.Delete, transform);

        gameObject.GetComponent<RectTransform>().localScale = Vector3.one;
        canvas = gameObject.GetComponent<Canvas>();
        canvas.worldCamera = gameCamera;
        canvasGroup = gameObject.GetComponent<CanvasGroup>();

        GameObject loadButton = gameObject.transform.Find("Buttons/LoadButton").gameObject;
        loadButton.GetComponent<Button>().onClick.AddListener(() =>
        {
            var loadText = loadButton.transform.GetComponentInChildren<TextMeshProUGUI>();
            if (_selectedSave == null)
            {
                loadText.text = "SAVE NOT SELECTED";
                Unbound.Instance.ExecuteAfterSeconds(1, () => { loadText.text = "LOAD"; });
            }
            else
            {
                if (SaveManager._selectedSave != null)
                {
                    SaveManager._selectedSave = null;
                    _selectedText.text = "";
                    loadText.text = "UNLOADED";
                    Unbound.Instance.ExecuteAfterSeconds(1, () => { loadText.text = "LOAD"; });
                    return;
                }
                // if (_selectedText != null) 
                    
                var selectedGameData = _selectedGame.gameData;
                if (!GameModeManager.GameModes.ContainsKey(selectedGameData.gameMode))
                {
                    loadText.text = "UNKNOWN GAMEMODE";
                    Unbound.Instance.ExecuteAfterSeconds(1, () => { loadText.text = "LOAD"; });
                    return;
                }

                SelectSave(selectedGameData, _selectedSave);
                _selectedText = _selectedSave.loaded;
                _selectedSave.loaded.text = "LOADED";
                loadText.text = "UNLOAD SAVE: " + _selectedSave.Time;
            }
        });

        GameObject backButton = gameObject.transform.Find("Buttons/BackButton").gameObject;
        backButton.GetComponent<Button>().onClick.AddListener(Close);

        var uiExportImport = transform.GetChild(4);
        var uiExportImportCanvas = uiExportImport.GetComponent<Canvas>();

        var spinner = uiExportImport.GetChild(4).gameObject;

        var uiExport = uiExportImport.GetChild(2);
        var uiExportCanvas = uiExport.GetComponent<Canvas>();
        var uiExportText = uiExport.GetComponentInChildren<TMP_InputField>();
        var uiExportButton = uiExport.GetComponentInChildren<Button>();
        uiExportButton.onClick.AddListener(() =>
        {
            spinner.SetActive(true);
            GameSaver.Instance.StartCoroutine(WebManager.SendSaves(uiExportText, spinner));
        });

        var uiImport = uiExportImport.GetChild(3);
        var uiImportCanvas = uiImport.GetComponent<Canvas>();
        var uiImportText = uiImport.GetComponentInChildren<TMP_InputField>();
        var uiImportButton = uiImport.GetComponentInChildren<Button>();
        uiImportButton.onClick.AddListener(() =>
        {
            var placeholderText = uiImportText.placeholder.GetComponent<TextMeshProUGUI>();
            var initialColor = placeholderText.color;
            var errorColor = new Color(255, 0, 0, 0.65f);
            
            var useText = uiImportText.text;
            uiImportText.text = "";

            useText = useText.Replace(" ", "");
            if (useText.Length != 5)
            {
                
                placeholderText.color = errorColor;
                placeholderText.text = "INVALID CODE";
                this.ExecuteAfterSeconds(1, () =>
                {
                    placeholderText.color = initialColor;
                    placeholderText.text = "CODE HERE...";

                });
                return;
            }

            spinner.SetActive(true);
            uiImportText.interactable = false;
            GameSaver.Instance.StartCoroutine(WebManager.GetSaves(uiExportImportCanvas, uiImportText, useText, spinner, placeholderText, initialColor, errorColor));
        });

        var topBarTransform = transform.GetChild(0);

        var exportButtonContainer = topBarTransform.GetChild(0);
        var exportButton = exportButtonContainer.GetComponent<Button>();
        exportButton.onClick.AddListener(() =>
        {
            uiExportImportCanvas.enabled = true;
            uiImportCanvas.enabled = false;
            uiExportCanvas.enabled = true;
        });

        var importButtonContainer = topBarTransform.GetChild(1);
        var importButton = importButtonContainer.GetComponent<Button>();
        importButton.onClick.AddListener(() =>
        {
            uiExportImportCanvas.enabled = true;
            uiExportCanvas.enabled = false;
            uiImportCanvas.enabled = true;
        });

        var savedGamesDisplay = transform.GetChild(3).GetChild(0);
        savedGamesText = savedGamesDisplay.GetComponentInChildren<TextMeshProUGUI>();

        Active(false);
    }

    public void Open()
    {
        _linksUi ??= GameObject.Find("Links(Clone)");
        _pingUi ??= GameObject.Find("UIHolder");
        _codeUi ??= GameObject.Find("LobbyImprovementsBG");
        _timerUi ??= GameObject.Find("TimerLobbyUI(Clone)");
        _roundUi ??= GameObject.Find("RoundCounterSmall");

        if (_linksUi) _linksUi.gameObject.SetActive(false);
        if (_pingUi) _pingUi.gameObject.SetActive(false);
        if (_codeUi) _codeUi.gameObject.SetActive(false);
        if (_timerUi) _timerUi.gameObject.SetActive(false);
        if (_roundUi) _roundUi.gameObject.SetActive(false);

        GameSaver.Instance.StartCoroutine(Swoop(lobbyUi, 0, Screen.height * 2, true));
        GameSaver.Instance.StartCoroutine(Swoop(gameObject, 0, -Screen.height * 2, false, () =>
        {
            listMenuButton.OnPointerEnter(null);
            Active(true);
            LoadGames();
            menuCoroutines.Add(GameSaver.Instance.StartCoroutine(LoadButtons()));
        }));

        open = true;
    }

    public void Close()
    {
        if (!open) return;
        foreach (var menuCoroutine in menuCoroutines)
        {
            GameSaver.Instance.StopCoroutine(menuCoroutine);
        }

        GameSaver.Instance.StartCoroutine(Swoop(lobbyUi, 0, -Screen.height * 2, true));
        GameSaver.Instance.StartCoroutine(Swoop(gameObject, 0, Screen.height * 2, false));

        if (_linksUi) _linksUi.gameObject.SetActive(true);
        if (_pingUi) _pingUi.gameObject.SetActive(true);
        if (_codeUi) _codeUi.gameObject.SetActive(true);
        if (_timerUi) _timerUi.gameObject.SetActive(true);
        if (_roundUi) _roundUi.gameObject.SetActive(true);

        Active(false);
    }

    public void Reset()
    {
        foreach (var menuCoroutine in menuCoroutines)
        {
            GameSaver.Instance.StopCoroutine(menuCoroutine);
        }
        menuCoroutines = new List<Coroutine>();

        foreach (var gameInfoData in orderedGames.Where(gameInfoData => gameInfoData != null))
        {
            RemoveGameSaveButtons(gameInfoData);
        }
    }

    public void RemoveGameSaveButtons(GameInfoData gameInfoData)
    {
        if (gameInfoData.gameData.button)
        {
            Destroy(gameInfoData.gameData.button);
        }
        foreach (var saveData in gameInfoData.gameSaves.Where(saveData => saveData != null))
        {
            if (saveData.button)
            {
                Destroy(saveData.button);
            }
            foreach (var playerData in saveData.players.Where(playerData => playerData != null))
            {
                if (playerData.display)
                {
                    Destroy(playerData.display);
                }
            }
        }
    }

    private void Update()
    {
        if (_games.Count != lastGameCount)
            UpdateGamesCount(_games.Count);
    }

    public void UpdateGamesCount(int amount)
    {
        savedGamesText.text = $"SAVED GAMES: {amount}";
        lastGameCount = amount;
    }

    private IEnumerator LoadSaveDataUi(SaveData saveData, Transform playerContainer)
    {
        foreach (var playerData in saveData.players.Where(playerData => playerData != null))
        {
            if (!playerData.display)
            {
                var playerAsset = Instantiate(AssetManager.Player, playerContainer);
                playerAsset.transform.GetChild(0).GetChild(0).GetChild(0).gameObject.SetActive(playerData.host);
                playerAsset.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text = playerData.name;
                playerAsset.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = ExtraPlayerSkins.GetTeamColorName(playerData.serializedColor).ToUpper();
                playerAsset.transform.GetChild(2).GetComponent<TextMeshProUGUI>().color = playerData.Color.color;

                var cardScrollView = playerAsset.transform.GetChild(1).GetChild(0).gameObject;
                var cardContainer = cardScrollView.transform.GetChild(0).GetChild(0);
                foreach (var card in playerData.Cards)
                {
                    var cardAsset = Instantiate(AssetManager.Card, cardContainer);
                    if (card)
                    {
                        cardAsset.transform.GetComponentInChildren<TextMeshProUGUI>().text = CardInitials(card);
                        cardAsset.transform.GetComponent<Image>().color = playerData.Color.color;
                    }
                    else
                    {
                        cardAsset.transform.GetComponentInChildren<TextMeshProUGUI>().text = "???";
                        cardAsset.transform.GetComponent<Image>().color = playerData.Color.color;
                    }

                    var displayMono = cardAsset.GetOrAddComponent<CardDisplayMono>();
                    displayMono.container = playerAsset.transform;
                    displayMono.card = card;
                    displayMono.color = playerData.Color.color;
                }

                var pointContainer = playerAsset.transform.GetChild(3);
                for (int i = 0; i < saveData.pointsToWin; i++)
                {
                    GameObject pointObject = Instantiate(AssetManager.Point, pointContainer);
                    Image image = pointObject.GetComponentInChildren<Image>();
                    image.color = playerData.Color.color;

                    if (i == playerData.rounds)
                    {
                        var fillAmount = playerData.points == 0
                            ? 0
                            : (float) playerData.points / saveData.pointsToWinRound;
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
            yield return null;
        }
    }

    private IEnumerator LoadGameInfoDataUi(GameInfoData gameInfoData, Transform roundDisplayContainer, Transform roundButtonContainer, Transform playerContainer)
    {
        foreach (var saveData in gameInfoData.gameSaves.Where(saveData => saveData != null))
        {
            if (!saveData.button)
            {
                var roundDisplay = Instantiate(AssetManager.SaveInfo, roundDisplayContainer);

                roundDisplay.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = $"ROUND {saveData.round}";
                roundDisplay.transform.GetChild(1).GetChild(1).GetComponent<TextMeshProUGUI>().text = $"{saveData.players.Count}/32";
                roundDisplay.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = gameInfoData.gameData.gameMode.ToUpper();
                saveData.loaded = roundDisplay.transform.GetChild(3).GetComponent<TextMeshProUGUI>();
                saveData.loaded.text = "";
                saveData.display = roundDisplay;
                roundDisplay.SetActive(false);

                var roundButton = Instantiate(AssetManager.RoundButton, roundButtonContainer);

                var text = $"{saveData.Time}".Split(' ');
                roundButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = $"{text[1]}\n{text[0]}";
                roundButton.transform.GetChild(1).GetChild(0).GetComponent<TextMeshProUGUI>().text = $"ROUND {saveData.round}";
                roundButton.transform.GetChild(3).GetComponent<TextMeshProUGUI>().text = Regex.Replace(saveData.saveType.ToString(), "([a-z])_?([A-Z])", "$1\n$2").ToUpper();

                roundButton.GetComponent<Button>().onClick.AddListener(() =>
                {
                    menuCoroutines.Add(GameSaver.Instance.StartCoroutine(LoadSaveDataUi(saveData, playerContainer)));
                    if (_selectedSave == saveData) return;
                    _selectedSave = saveData;
                    DisablePlayerAssets();
                    menuCoroutines.Add(GameSaver.Instance.StartCoroutine(EnablePlayerAssets(saveData)));
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
    }

    private IEnumerator LoadButtons()
    {
        var genericSection = transform.GetChild(1);
        var gameButtonContainer = genericSection.GetChild(0).GetChild(0).GetChild(0);

        var displaySection = genericSection.GetChild(1);
        var scrollViewSection = displaySection.GetChild(0);
        var roundButtonContainer = scrollViewSection.GetChild(0).GetChild(0).GetChild(0);
        
        var section = scrollViewSection.GetChild(1);
        var roundDisplayContainer = section.GetChild(0);
        var playerContainer = section.GetChild(0).GetChild(0).GetChild(0);

        foreach (var gameInfoData in orderedGames.Where(gameInfoData => gameInfoData != null))
        {
            if (gameInfoData.rounds == 0) continue;
            if (gameInfoData.gameData.button)
            {
                gameInfoData.gameData.button.SetActive(true);
                continue;
            }

            var gameButton = Instantiate(AssetManager.GameButton, gameButtonContainer);

            var gameButtonTransform = gameButton.transform;
            var text = $"{gameInfoData.gameData.StartTime}".Split(' ');
            gameButtonTransform.GetChild(0).GetComponent<TextMeshProUGUI>().text = $"{text[1]} {text[0]}";
            gameButtonTransform.GetChild(1).GetChild(1).GetComponent<TextMeshProUGUI>().text = gameInfoData.rounds.ToString();
            gameButtonTransform.GetChild(2).GetChild(1).GetComponent<TextMeshProUGUI>().text = $"{gameInfoData.gameData.playerAmount}/32";
            gameButtonTransform.GetChild(3).GetComponent<TextMeshProUGUI>().text = gameInfoData.gameData.gameMode.ToUpper();

            gameButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                menuCoroutines.Add(GameSaver.Instance.StartCoroutine(LoadGameInfoDataUi(gameInfoData, roundDisplayContainer, roundButtonContainer, playerContainer)));
                if (_selectedGame == gameInfoData) return;
                _selectedGame = gameInfoData;
                _selectedSave = null;
                DisableRoundButtons();
                DisablePlayerAssets();
                menuCoroutines.Add(GameSaver.Instance.StartCoroutine(EnableRoundButtons(gameInfoData)));
            });
            gameInfoData.gameData.button = gameButton;

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
        foreach (var gameInfoData in orderedGames.Where(gameInfoData => gameInfoData.gameData.button))
        {
            gameInfoData.gameData.button.SetActive(true);
            yield return null;
        }
    }

    private static IEnumerator EnableRoundButtons(GameInfoData gameInfoData)
    {
        foreach (var saveData in gameInfoData.gameSaves.Where(saveData => saveData.button))
        {
            saveData.button.SetActive(true);
            yield return null;
        }
    }

    private static IEnumerator EnablePlayerAssets(SaveData saveData)
    {
        if (saveData.display) 
            saveData.display.SetActive(true);
        foreach (var playerData in saveData.players.Where(playerData => playerData.display))
        {
            playerData.display.SetActive(true);
            yield return null;
        }
    }

    private void DisableGameButtons()
    {
        foreach (var gameInfoData in orderedGames.Where(gameInfoData => gameInfoData.gameData.button))
        {
            gameInfoData.gameData.button.SetActive(false);
        }
    }

    private static void DisableRoundButtons()
    {
        foreach (var saveData in from gameInfoData in orderedGames where gameInfoData.gameData.button from saveData in gameInfoData.gameSaves where saveData.button select saveData)
        {
            saveData.button.SetActive(false);
        }
    }

    private static void DisablePlayerAssets()
    {
        foreach (var saveData in orderedGames.Where(gameInfoData => gameInfoData.gameData.button).SelectMany(gameInfoData => gameInfoData.gameSaves))
        {
            if (saveData.display) 
                saveData.display.SetActive(false);
            if (!saveData.button) continue;
            foreach (var playerData in saveData.players.Where(playerData => playerData.display))
            {
                playerData.display.SetActive(false);
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

    private void Active(bool active)
    {
        canvas.enabled = active;
        canvasGroup.blocksRaycasts = active;
    }
}