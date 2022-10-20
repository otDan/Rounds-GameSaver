using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx;
using GameSaver.Asset;
using GameSaver.Component;
using GameSaver.Menu;
using Photon.Pun;
using RWF;
using RWF.UI;
using Steamworks;
using TMPro;
using UnboundLib;
using UnboundLib.Extensions;
using UnboundLib.GameModes;
using UnboundLib.Networking;
using UnboundLib.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

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

        private static GameObject _savingObject;

        internal static void Initialize()
        {
            Directory.CreateDirectory(SavesPath);
            LoadGames();
        }

        public static void LoadGames()
        {
            // Stopwatch stopwatch = new Stopwatch();
            // stopwatch.Start();
            foreach (var directoryPath in Directory.GetDirectories(SavesPath))
            {
                if (!Directory.Exists(directoryPath)) continue;
                if (Path.GetDirectoryName(directoryPath)!.Contains("Game-")) continue;

                var gameInfoPath = Path.Combine(directoryPath, "game.json");
                if (!File.Exists(gameInfoPath)) continue;
                var gameContent = File.ReadAllText(gameInfoPath);
                GameData gameData = JsonUtility.FromJson<GameData>(gameContent);
                // GameSaver.Instance.Log("Game found: " + gameData.startTime);
                
                var saves = (from filePath in Directory.GetFiles(directoryPath) 
                    where Path.GetFileName(filePath).Contains("save") 
                    select File.ReadAllText(filePath) into fileContent 
                    select JsonUtility.FromJson<SaveData>(fileContent)).ToList();
                // GameSaver.Instance.Log("Total saves found: " + saves.Count);

                GameInfoData game = new GameInfoData(gameData, saves);
                if (ContainsGame(game)) continue;
                _games.Add(game);
            }
            // stopwatch.Stop();
            // GameSaver.Instance.Log($"Total games found: {_games.Count} in {stopwatch.ElapsedMilliseconds}ms");
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
                var player = SteamManager.GetPlayerFromSteamId(playerData.steamId);
                if (player == null)
                    player = PlayerManager.instance.players.Find(loopPlayer => PhotonNetwork.CurrentRoom.GetPlayer(loopPlayer.data.view.OwnerActorNr).NickName == playerData.name);
                else
                    GameSaver.Instance.Log($"Found player from steam id {playerData.steamId}");
                if (player == null) continue;

                // GameSaver.Instance.Log($"Loading player {PhotonNetwork.CurrentRoom.GetPlayer(player.data.view.OwnerActorNr).NickName}");
                var cards = playerData.cards;
                var floatArray = Enumerable.Repeat(2f, cards.Count).ToArray();
                ModdingUtils.Utils.Cards.instance.AddCardsToPlayer(player, cards.ToArray(), true, Enumerable.Repeat("", cards.Count).ToArray(), floatArray, floatArray, true);
                // GameSaver.Instance.Log($"Added cards {cards.ToArray()}");
                // GameModeManager.CurrentHandler.SetTeamScore(player.teamID, new TeamScore(playerData.points, playerData.rounds));
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
            NetworkingManager.RPC(typeof(SaveManager), nameof(LoadSaveTeamSettings), teamId, points, rounds);
        }

        public static void ShareSaveGameSettings(int pointsToWin, int pointsToWinRound)
        {
            NetworkingManager.RPC_Others(typeof(SaveManager), nameof(LoadSaveGameSettings), pointsToWin, pointsToWinRound, _round);
        }

        [UnboundRPC]
        private static void LoadSaveTeamSettings(int teamId, int points, int rounds)
        {
            GameModeManager.CurrentHandler.SetTeamScore(teamId, new TeamScore(points, rounds));
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
            PrivateRoomHandler.instance.UnreadyAllPlayers();
            PrivateRoomHandler.instance.StartCoroutine(PrivateRoomHandler.instance.SyncMethodCoroutine(nameof(PrivateRoomHandler.SetGameSettings), null, GameModeManager.CurrentHandlerID, GameModeManager.CurrentHandler.Settings));
            PrivateRoomHandler.instance.HandleTeamRules();
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

        internal static IEnumerator PickEnd(IGameModeHandler gm)
        {
            // GameSaver.Instance.Log("Hook pick end");
            // if (!ConfigController.GameSaverSave) yield return null;
            // if (!ConfigController.GameSaverSaveAsHost) yield return null;
            
            yield return Save(SaveType.PickEnd);
        }

        internal static IEnumerator PlayerPickStart(IGameModeHandler gm)
        {
            // yield return CardChoice.instance.DoPick(1, player.playerID, PickerType.Player);
            // CardChoiceVisuals.instance.Show(32, true);
            
            // GameSaver.Instance.ExecuteAfterSeconds(1, () =>
            // {
            //     // CardChoice.instance.IsPicking = false;
            //     // GameSaver.Instance.StartCoroutine(CardChoice.instance.IDoEndPick());
            //     // CardChoice.instance.GetComponent<PhotonView>().RPC("RPCA_DoEndPick", RpcTarget.All, typeof(CardChoice).GetMethod("PrivateMethod", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(CardChoice.instance, null), null, null, (object) CardChoice.instance.pickrID);
            //     CardChoice.instance.Pick(null, true);
            //     // ((List<GameObject>) CardChoice.instance.GetFieldValue("spawnedCards")).Clear();
            //
            //     // UIHandler.instance.StopShowPicker();
            //     // CardChoiceVisuals.instance.Hide();
            // });
            // GameSaver.Instance.Log("Triggering PlayerPickStart");
            yield return null; //GameModeManager.TriggerHook(GameModeHooks.HookPickEnd);
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

            // var autoHide = _savingObject.GetComponent<AutoHide>();
            // if (autoHide != null) Object.Destroy(autoHide);

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
