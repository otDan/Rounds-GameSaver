using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx;
using GameSaver.Asset;
using GameSaver.Component;
using GameSaver.Menu;
using Photon.Pun;
using RWF;
using Steamworks;
using TMPro;
using UnboundLib;
using UnboundLib.Extensions;
using UnboundLib.GameModes;
using UnboundLib.Networking;
using UnboundLib.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameSaver.Util;

public class SaveManager
{
    private static readonly string SavesPath = Path.Combine(Paths.ConfigPath, "Saves");
    private static string _gameSavesPath;

    private static int _round;
    private static Guid _gameGuid;

    public static List<GameInfoData> orderedGames => _games.OrderByDescending(game => game.gameData._serializedStartTime).ToList();

    public static List<GameInfoData> _games = new();
    public static SaveData _selectedSave;

    private static GameObject _savingObject;

    internal static void Initialize()
    {
        Directory.CreateDirectory(SavesPath);
        LoadGames();
    }

    public static void LoadGames()
    {
        foreach (var directoryPath in Directory.GetDirectories(SavesPath))
        {
            if (!Directory.Exists(directoryPath)) continue;
            if (Path.GetDirectoryName(directoryPath)!.Contains("Game-")) continue;

            var gameInfoPath = Path.Combine(directoryPath, "game.json");
            if (!File.Exists(gameInfoPath)) continue;
            var gameContent = File.ReadAllText(gameInfoPath);
            GameData gameData = JsonUtility.FromJson<GameData>(gameContent);
                
            var saves = (from filePath in Directory.GetFiles(directoryPath) 
                where Path.GetFileName(filePath).Contains("save") 
                select File.ReadAllText(filePath) into fileContent 
                select JsonUtility.FromJson<SaveData>(fileContent)).ToList();

            GameInfoData game = new GameInfoData(gameInfoPath, gameData, saves);
            if (ContainsGame(game)) continue;
            _games.Add(game);
        }
    }

    private static bool ContainsGame(GameInfoData gameInfoData)
    {
        bool found = false;
        foreach (var _ in _games.Where(game => game.gameData._serializedStartTime == gameInfoData.gameData._serializedStartTime))
        {
            found = true;
        }
        return found;
    }

    internal static IEnumerator PreSave()
    {
        _gameSavesPath = Path.Combine(SavesPath, "Game-" + _gameGuid);
        Directory.CreateDirectory(_gameSavesPath);

        string gameMode = GameModeManager.CurrentHandlerID;
        GameType gameType = PhotonNetwork.OfflineMode || PhotonNetwork.CurrentRoom is null ? GameType.Local : GameType.Online;
        GameData gameData = new GameData(DateTime.Now, PlayerManager.instance.players.Count, gameMode, gameType);
        string jsonSave = JsonUtility.ToJson(gameData);
        File.WriteAllText(Path.Combine(_gameSavesPath, "game.json"), jsonSave);

        yield return null;
    }

    internal static IEnumerator LoadSave()
    {
        if (_selectedSave == null) yield break;
        _round = _selectedSave.round;

        ShareSaveGameSettings(_selectedSave.pointsToWin, _selectedSave.pointsToWinRound);
        yield return null;

        foreach (var playerData in _selectedSave.players)
        {
            // Find the player from the save
            var player = SteamManager.GetPlayerFromSteamId(playerData.steamId);
            if (player == null) 
                player = PlayerManager.instance.players.Find(loopPlayer => PhotonNetwork.CurrentRoom.GetPlayer(loopPlayer.data.view.OwnerActorNr).NickName == playerData.name);
            if (player == null) continue;

            var cards = playerData.Cards;
            ModdingUtils.Utils.Cards.instance.AddCardsToPlayer(player, cards.ToArray(), true, null, null, null, true);
            ShareSaveTeamSettings(player.teamID, playerData.points, playerData.rounds);
            yield return null;
        }
        _selectedSave = null;
    }

    public static void ShareSaveTeamSettings(int teamId, int points, int rounds)
    {
        NetworkingManager.RPC(typeof(SaveManager), nameof(LoadSaveTeamSettings), teamId, points, rounds);
    }

    public static void ShareSaveGameSettings(int pointsToWin, int pointsToWinRound)
    {
        NetworkingManager.RPC(typeof(SaveManager), nameof(LoadSaveGameSettings), pointsToWin, pointsToWinRound, _round);
    }

    [UnboundRPC]
    private static void LoadSaveTeamSettings(int teamId, int points, int rounds)
    {
        GameModeManager.CurrentHandler.SetTeamScore(teamId, new TeamScore(points, rounds));
        UIHandler.instance.roundCounter.InvokeMethod("ReDraw");
        UIHandler.instance.roundCounterSmall.InvokeMethod("ReDraw");
    }

    [UnboundRPC]
    private static void LoadSaveGameSettings(int pointsToWin, int pointsToWinRound, int currentRound)
    {
        GameModeManager.CurrentHandler.ChangeSetting("roundsToWinGame", pointsToWin);
        GameModeManager.CurrentHandler.ChangeSetting("pointsToWinRound", pointsToWinRound);
        _round = currentRound;
    }

    internal static IEnumerator GameStart(IGameModeHandler gm)
    {
        if (SaveLoadMenu.instance != null)
            SaveLoadMenu.instance.Reset();
        SteamManager.steamIds = new Dictionary<int, ulong>();
        _savingObject = Object.Instantiate(AssetManager.Saving);
        _round = 1;
        _gameGuid = Guid.NewGuid();

        yield return LoadSave();
        yield return PreSave();
    }

    internal static IEnumerator GameEnd(IGameModeHandler gm)
    {
        Object.Destroy(_savingObject);
        yield return null;
    }

    internal static void SelectSave(GameData gameData, SaveData selectedSave)
    {
        _selectedSave = selectedSave;
        GameModeManager.SetGameMode(gameData.gameMode);
        PrivateRoomHandler.instance.InvokeMethod("UnreadyAllPlayers");
        PrivateRoomHandler.instance.ExecuteAfterGameModeInitialized(gameData.gameMode, () =>
        {
            SyncMethodStatic.SyncMethod(typeof(PrivateRoomHandler), nameof(PrivateRoomHandler.SetGameSettings), null, GameModeManager.CurrentHandlerID, GameModeManager.CurrentHandler.Settings);
            PrivateRoomHandler.instance.InvokeMethod("HandleTeamRules");
        });
        SaveLoadMenu.instance.Close();
    }

    internal static IEnumerator RoundStart(IGameModeHandler gm)
    {
        _savingObject.SetActive(false);
        yield return null;
    }

    internal static IEnumerator RoundEnd(IGameModeHandler gm)
    {
        _round += 1;
        yield return null;
    }
        
    internal static IEnumerator PickStart(IGameModeHandler gm)
    {
        // if (!ConfigController.GameSaverSave) yield return null;
        // if (!ConfigController.GameSaverSaveAsHost) yield return null;
            
        yield return Save(SaveType.PickStart);
    }

    internal static IEnumerator Save(SaveType saveType)
    {
        _savingObject.SetActive(true);
        _savingObject.GetOrAddComponent<AnimationAutoDestroy>();
        yield return null;

        var playersData = new List<string>();
        int i = 1;
        foreach (var playerData in 
                 from player in PlayerManager.instance.players 
                 where player != null 
                 let name = PhotonNetwork.OfflineMode || PhotonNetwork.CurrentRoom is null ? SteamFriends.GetPersonaName() + "-" + i : PhotonNetwork.CurrentRoom.GetPlayer(player.data.view.OwnerActorNr).NickName 
                 let colorId = player.colorID() 
                 let cards = new List<CardInfo>(player.data.currentCards) 
                 let teamScore = GameModeManager.CurrentHandler.GetTeamScore(player.teamID) 
                 let points = teamScore.points 
                 let rounds = teamScore.rounds 
                 let host = player.data.view.IsMine 
                 let steamId = SteamManager.steamIds.ContainsKey(player.data.view.OwnerActorNr) ? SteamManager.steamIds[player.data.view.OwnerActorNr] : 1 
                 select new PlayerData(name, colorId, cards, points, rounds, host, steamId))
        {
            playersData.Add(JsonUtility.ToJson(playerData));
            i++;
        }
        int pointsToWinRound = (int) GameModeManager.CurrentHandler.Settings["pointsToWinRound"];
        int roundsToWin = (int) GameModeManager.CurrentHandler.Settings["roundsToWinGame"];
        SaveData saveData = new SaveData(DateTime.Now, saveType, _round, pointsToWinRound, roundsToWin, playersData);
        string jsonSave = JsonUtility.ToJson(saveData, true);
        string fileName = "save" + DateTime.Now.ToBinary() + ".json";

        File.WriteAllText(Path.Combine(_gameSavesPath, fileName), jsonSave);
        yield return null;
    }

    public static void DeleteGameSave(GameInfoData gameInfoData)
    {
        SaveLoadMenu.instance.RemoveGameSaveButtons(gameInfoData);
        File.Delete(gameInfoData.FilePath);
    }

    public class GameInfoData
    {
        public string FilePath { get; }

        public GameData gameData;
        public List<SaveData> gameSaves;
        private int _rounds = -1;
        public int rounds
        {
            get
            {
                if (_rounds == -1)
                {
                    _rounds = gameSaves.Count == 0 ? 0 : gameSaves.OrderByDescending(save => save.round).First().round;
                }

                return _rounds;
            }
        }

        public GameInfoData(string filePath, GameData gameData, List<SaveData> gameSaves)
        {
            FilePath = filePath;
            this.gameData = gameData;
            this.gameSaves = gameSaves;
        }
    }

    [Serializable]
    public class GameData
    {
        [NonSerialized] public GameObject button;
        public DateTime StartTime => DateTime.FromBinary(_serializedStartTime);

        public long _serializedStartTime;
        public int playerAmount;
        public string gameMode;
        public GameType gameType;

        public GameData(DateTime startTime, int playerAmount, string gameMode, GameType gameType)
        {
            _serializedStartTime = startTime.ToBinary();
            this.playerAmount = playerAmount;
            this.gameMode = gameMode;
            this.gameType = gameType;
        }
    }

    [Serializable]
    public class SaveData
    {
        [NonSerialized] public GameObject button;
        [NonSerialized] public GameObject display;
        [NonSerialized] public TextMeshProUGUI loaded;

        public DateTime Time => DateTime.FromBinary(_serializedDateTime);

        public long _serializedDateTime;
        public SaveType saveType;
        public int round;
        public int pointsToWinRound = 2;
        public int pointsToWin;
        public List<string> seralizedPlayers;
        private List<PlayerData> _players;
        public List<PlayerData> players
        {
            get { return _players ??= seralizedPlayers.Select(JsonUtility.FromJson<PlayerData>).ToList(); }
            set => _players = value;
        }

        public SaveData(DateTime time, SaveType saveType, int round, int pointsToWinRound, int pointsToWin, List<string> players)
        {
            this._serializedDateTime = time.ToBinary();
            this.saveType = saveType;
            this.round = round;
            this.pointsToWinRound = pointsToWinRound;
            this.pointsToWin = pointsToWin;
            this.seralizedPlayers = players;
        }
    }

    [Serializable]
    public class PlayerData {
        [NonSerialized] public GameObject display;

        private List<CardInfo> _cards;
        public List<CardInfo> Cards
        {
            get
            {
                if (_cards != null) return _cards;
                _cards = new List<CardInfo>();
                var hiddenCards = (List<CardInfo>) ModdingUtils.Utils.Cards.instance.GetFieldValue("hiddenCards");
                foreach (var card in serializedCards)
                {
                    CardInfo cardInfo = CardManager.GetCardInfoWithName(card);
                    if (cardInfo == null)
                    {
                        cardInfo = CardChoice.instance.cards.FirstOrDefault(c => string.Equals(Regex.Replace(c.gameObject.name, @"\s+", ""), Regex.Replace(card, @"\s+", ""), StringComparison.CurrentCultureIgnoreCase));
                        if (cardInfo == null)
                        {
                            cardInfo = hiddenCards.FirstOrDefault(c => string.Equals(Regex.Replace(c.gameObject.name, @"\s+", ""), Regex.Replace(card, @"\s+", ""), StringComparison.CurrentCultureIgnoreCase));
                        }
                    }
                    _cards.Add(cardInfo);
                }
                return _cards;
            }
        }

        public PlayerSkin Color => PlayerSkinBank.GetPlayerSkinColors(serializedColor);

        public string name;
        public int serializedColor;
        public List<string> serializedCards;
        public int rounds;
        public int points;
        public bool host;
        public ulong steamId;

        public PlayerData(string name, int color, List<CardInfo> cards, int points, int rounds, bool host, ulong steamId)
        {
            this.name = name;
            serializedColor = color;
            serializedCards = new List<string>();
            foreach (var cardInfo in cards)
            {
                serializedCards.Add(cardInfo.gameObject.name);
            }
            this.points = points;
            this.rounds = rounds;
            this.host = host;
            this.steamId = steamId;
        }
    }

    public enum SaveType
    {
        PickStart,
        PickEnd,
        CardPicked,
        RoundStart,
        RoundEnd,
        Manual
    }

    public enum GameType
    {
        Local,
        Online
    }
}