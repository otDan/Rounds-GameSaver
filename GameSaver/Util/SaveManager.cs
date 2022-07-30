using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx;
using GameSaver.Network;
using Landfall.Network;
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

namespace GameSaver.Util
{
    internal class SaveManager
    {
        private static readonly string SavesPath = Path.Combine(Paths.ConfigPath, "Saves");
        private static string _gameSavesPath;

        private static int _round;
        private static Guid _gameGuid;

        public static List<GameInfoData> orderedGames => _games.OrderByDescending(game => game.gameData._serializedStartTime).ToList();
        public static List<GameInfoData> _games = new();
        private static SaveData _selectedSave;

        internal static void Initialize()
        {
            Directory.CreateDirectory(SavesPath);
            LoadGames();
        }

        public static void LoadGames()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (var directoryPath in Directory.GetDirectories(SavesPath))
            {
                if (!Directory.Exists(directoryPath)) continue;
                if (Path.GetDirectoryName(directoryPath)!.Contains("Game-")) continue;

                var gameInfoPath = Path.Combine(directoryPath, "game.json");
                if (!File.Exists(gameInfoPath)) continue;
                var gameContent = File.ReadAllText(gameInfoPath);
                GameData gameData = JsonUtility.FromJson<GameData>(gameContent);
                // GameSaver.Instance.Log("Game found: " + gameData.startTime);
                
                List<SaveData> saves = (from filePath in Directory.GetFiles(directoryPath) 
                    where Path.GetFileName(filePath).Contains("save") 
                    select File.ReadAllText(filePath) into fileContent 
                    select JsonUtility.FromJson<SaveData>(fileContent)).ToList();
                // GameSaver.Instance.Log("Total saves found: " + saves.Count);

                GameInfoData game = new GameInfoData(gameData, saves);
                if (ContainsGame(game)) continue;
                _games.Add(game);
            }
            stopwatch.Stop();
            GameSaver.Instance.Log($"Total games found: {_games.Count} in {stopwatch.ElapsedMilliseconds}ms");
        }

        private static bool ContainsGame(GameInfoData gameInfoData)
        {
            bool found = false;
            foreach (var game in _games.Where(game => game.gameData._serializedStartTime == gameInfoData.gameData._serializedStartTime))
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
            // GameSaver.Instance.Log($"Loading save...");
            _round = _selectedSave.round;
            // GameSaver.Instance.Log($"Round set to {_round}");
            
            // GameSaver.Instance.Log($"Rounds to win game set to {_selectedSave.pointsToWin}");
            // GameSaver.Instance.Log($"Points to win round set to {_selectedSave.pointsToWinRound}\n");
            foreach (var playerData in _selectedSave.players)
            {
                // var player = SteamManager.GetPlayerFromSteamId(playerData.steamId);
                // if (player == null)
                var player = PlayerManager.instance.players.Find(loopPlayer => PhotonNetwork.CurrentRoom.GetPlayer(loopPlayer.data.view.OwnerActorNr).NickName == playerData.name);
                if (player == null) continue;

                // GameSaver.Instance.Log($"Loading player {PhotonNetwork.CurrentRoom.GetPlayer(player.data.view.OwnerActorNr).NickName}");
                var cards = playerData.cards;
                var floatArray = Enumerable.Repeat(2f, cards.Count).ToArray();
                ModdingUtils.Utils.Cards.instance.AddCardsToPlayer(player, cards.ToArray(), true, Enumerable.Repeat("", cards.Count).ToArray(), floatArray, floatArray, true);
                // GameSaver.Instance.Log($"Added cards {cards.ToArray()}");
                GameModeManager.CurrentHandler.SetTeamScore(player.teamID, new TeamScore(playerData.points, playerData.rounds));
                // GameSaver.Instance.Log($"Set team points to {playerData.rounds} rounds and {playerData.points} points");
                ShareSaveTeamSettings(player.teamID, playerData.points, playerData.rounds);
                yield return null;
            }
            
            ShareSaveGameSettings(_selectedSave.pointsToWin, _selectedSave.pointsToWinRound);
            _selectedSave = null;
            // GameSaver.Instance.Log($"Save loaded!");
        }

        public static void ShareSaveTeamSettings(int teamId, int points, int rounds)
        {
            NetworkingManager.RPC_Others(typeof(SaveManager), nameof(LoadSaveTeamSettings), teamId, points, rounds);
        }

        public static void ShareSaveGameSettings(int pointsToWin, int pointsToWinRound)
        {
            NetworkingManager.RPC_Others(typeof(SaveManager), nameof(LoadSaveGameSettings), pointsToWin, pointsToWinRound);
        }

        [UnboundRPC]
        private static void LoadSaveTeamSettings(int teamId, int points, int rounds)
        {
            GameModeManager.CurrentHandler.SetTeamScore(teamId, new TeamScore(points, rounds));
        }

        [UnboundRPC]
        private static void LoadSaveGameSettings(int pointsToWin, int pointsToWinRound)
        {
            GameModeManager.CurrentHandler.ChangeSetting("roundsToWinGame", pointsToWin);
            GameModeManager.CurrentHandler.ChangeSetting("pointsToWinRound", pointsToWinRound);
        }

        internal static IEnumerator GameStart(IGameModeHandler gm)
        {
            _round = 1;
            _gameGuid = Guid.NewGuid();
             
            yield return LoadSave();
            yield return PreSave();
        }

        internal static void SelectSave(SaveData selectedSave)
        {
            _selectedSave = selectedSave;
        }

        internal static IEnumerator RoundCounter(IGameModeHandler gm)
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

        internal static IEnumerator PickEnd(IGameModeHandler gm)
        {
            // if (!ConfigController.GameSaverSave) yield return null;
            // if (!ConfigController.GameSaverSaveAsHost) yield return null;
            
            yield return Save(SaveType.PickEnd);
        }

        internal static IEnumerator Save(SaveType saveType)
        {
            try
            {
                // GameSaver.Instance.Log("Starting save");
                List<string> playersData = new List<string>();
                int i = 1;
                foreach (Player player in PlayerManager.instance.players)
                {
                    if (player == null) continue;
                    string name = PhotonNetwork.OfflineMode || PhotonNetwork.CurrentRoom is null ? SteamFriends.GetPersonaName() + "-" + i : PhotonNetwork.CurrentRoom.GetPlayer(player.data.view.OwnerActorNr).NickName;
                    // GameSaver.Instance.Log($"Name: {name}");
                    int colorId = player.colorID();
                    // GameSaver.Instance.Log($"Color: {colorId}");
                    List<CardInfo> cards = new(player.data.currentCards);
                    // GameSaver.Instance.Log($"Cards: {cards.Count}");
                    var teamScore = GameModeManager.CurrentHandler.GetTeamScore(player.teamID);
                    // GameSaver.Instance.Log($"Score: {teamScore}");
                    int points = teamScore.points;
                    // GameSaver.Instance.Log($"Points: {points}");
                    int rounds = teamScore.rounds;
                    // GameSaver.Instance.Log($"Rounds: {rounds}");
                    bool host = player.data.view.IsMine;
                    // GameSaver.Instance.Log($"Host: {host}");
                    ulong steamId = SteamManager.steamIds.ContainsKey(player.data.view.OwnerActorNr) ? SteamManager.steamIds[player.data.view.OwnerActorNr] : 1;// SteamUser.GetSteamID().m_SteamID;

                    PlayerData playerData = new PlayerData(name, colorId, cards, points, rounds, host, steamId);
                    // GameSaver.Instance.Log($"Player data: {playerData}");
                    playersData.Add(JsonUtility.ToJson(playerData));
                    i++;
                }
                // GameSaver.Instance.Log($"Player save part finished");
                int pointsToWinRound = (int) GameModeManager.CurrentHandler.Settings["pointsToWinRound"];
                int roundsToWin = (int) GameModeManager.CurrentHandler.Settings["roundsToWinGame"];
                // GameSaver.Instance.Log($"Win rounds: {roundsToWin}");
                SaveData saveData = new SaveData(DateTime.Now, saveType, _round, pointsToWinRound, roundsToWin, playersData);
                // GameSaver.Instance.Log($"Save data: {saveData}");
                string jsonSave = JsonUtility.ToJson(saveData, true);
                string fileName = "save" + DateTime.Now.ToBinary() + ".json";

                File.WriteAllText(Path.Combine(_gameSavesPath, fileName), jsonSave);
                // GameSaver.Instance.Log("Saved: " + jsonSave);
            }
            catch (NullReferenceException nullReferenceException)
            {
                GameSaver.Instance.Log($"Null reference: {nullReferenceException}");
            }

            yield return null;
        }
        
        public class GameInfoData
        {
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

            public GameInfoData(GameData gameData, List<SaveData> gameSaves)
            {
                this.gameData = gameData;
                this.gameSaves = gameSaves;
            }
        }

        [Serializable]
        public class GameData
        {
            [NonSerialized] public GameObject button;
            public DateTime startTime => DateTime.FromBinary(_serializedStartTime);

            public long _serializedStartTime;
            public int playerAmount;
            public string gameMode;
            public GameType gameType;

            public GameData(DateTime startTime, int playerAmount, string gameMode, GameType gameType)
            {
                this._serializedStartTime = startTime.ToBinary();
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

            public DateTime time => DateTime.FromBinary(_serializedDateTime);

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
            public List<CardInfo> cards
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

            public PlayerSkin color => PlayerSkinBank.GetPlayerSkinColors(serializedColor);

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
                this.serializedColor = color;
                this.serializedCards = new List<string>();
                foreach (var cardInfo in cards)
                {
                    this.serializedCards.Add(cardInfo.gameObject.name);
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
            Manual
        }

        public enum GameType
        {
            Local,
            Online
        }
    }
}
