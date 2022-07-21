using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using Photon.Pun;
using Steamworks;
using UnboundLib.Extensions;
using UnboundLib.GameModes;
using UnboundLib.Utils;
using UnityEngine;

namespace GameSaver.Util
{
    internal class SaveHandler
    {
        private List<GameInfoData> _games;
        private static SaveData _selectedSave;
        private static int _round;
        private static Guid _gameGuid;
        private static readonly string SavesPath = Paths.ConfigPath + "/Saves";
        private static string _gameSavesPath;

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

        internal static IEnumerator LoadSave()
        {
            if (_selectedSave is null) yield break;

            // _gameGuid = _selectedSave;
            // _gameSavesPath = SavesPath + "/Game-" + _gameGuid;
        }

        internal static IEnumerator RoundCounter(IGameModeHandler gm)
        {
            _round += 1;
            yield break;
        }
        
        internal static IEnumerator PickStart(IGameModeHandler gm)
        {
            // if (!ConfigController.GameSaverSave) yield break;
            // if (!ConfigController.GameSaverSaveAsHost) yield break;
            
            yield return Save(SaveType.PickStart);
        }

        internal static IEnumerator PickEnd(IGameModeHandler gm)
        {
            // if (!ConfigController.GameSaverSave) yield break;
            // if (!ConfigController.GameSaverSaveAsHost) yield break;
            
            yield return Save(SaveType.PickEnd);
        }

        internal static IEnumerator PreSave()
        {
            Directory.CreateDirectory(SavesPath);
            _gameSavesPath = SavesPath + "/Game-" + _gameGuid;
            Directory.CreateDirectory(_gameSavesPath);

            string gameMode = GameModeManager.CurrentHandlerID;
            GameType gameType = PhotonNetwork.OfflineMode || PhotonNetwork.CurrentRoom is null ? GameType.Local : GameType.Online;
            GameData gameData = new GameData(DateTime.Now, PlayerManager.instance.players.Count, gameMode, gameType);
            string jsonSave = JsonUtility.ToJson(gameData);
            File.WriteAllText(_gameSavesPath + "/game.json", jsonSave);

            yield break;
        }

        internal static IEnumerator Save(SaveType saveType)
        {
            List<string> playersData = new List<string>();
            int i = 1;
            foreach (Player player in PlayerManager.instance.players)
            {
                string name = PhotonNetwork.OfflineMode || PhotonNetwork.CurrentRoom is null ? SteamFriends.GetPersonaName() + "-" + i : PhotonNetwork.CurrentRoom.GetPlayer(player.playerID).NickName;
                int colorId = player.colorID();
                List<CardInfo> cards = new List<CardInfo>(player.data.currentCards);
                var teamScore = GameModeManager.CurrentHandler.GetTeamScore(player.teamID);
                int points = teamScore.points;
                int rounds = teamScore.rounds;

                PlayerData playerData = new PlayerData(name, colorId, cards, points, rounds);
                playersData.Add(JsonUtility.ToJson(playerData));
                i++;
            }
            int roundsToWin = (int) GameModeManager.CurrentHandler.Settings["roundsToWinGame"];

            SaveData saveData = new SaveData(DateTime.Now, saveType, _round, roundsToWin, playersData);
            string jsonSave = JsonUtility.ToJson(saveData);
            string fileName = _gameGuid + "-" + Guid.NewGuid() + ".json";

            File.WriteAllText(_gameSavesPath + "/" + fileName, jsonSave);
            GameSaver.Instance.Log("Saved: " + jsonSave);

            yield break;
        }

        public class GameInfoData
        {
            public GameData gameData;
            public List<SaveData> gameSaves;

            public GameInfoData(GameData gameData, List<SaveData> gameSaves)
            {
                this.gameData = gameData;
                this.gameSaves = gameSaves;
            }
        }

        [Serializable]
        public class GameData
        {
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
            public DateTime time => DateTime.FromBinary(_serializedDateTime);
            public long _serializedDateTime;
            public SaveType saveType;
            public int round;
            public int pointsToWin;
            public List<string> players;

            public SaveData(DateTime time, SaveType saveType, int round, int pointsToWin, List<string> players)
            {
                this._serializedDateTime = time.ToBinary();
                this.saveType = saveType;
                this.round = round;
                this.pointsToWin = pointsToWin;
                this.players = players;
            }
        }

        [Serializable]
        public class PlayerData {
            public string name;
            public int color;
            public List<CardInfo> cards => CardManager.GetCardsInfoWithNames(serializedCards.ToArray()).ToList();
            public List<string> serializedCards;
            public int rounds;
            public int points;

            public PlayerData(string name, int color, List<CardInfo> cards, int points, int rounds)
            {
                this.name = name;
                this.color = color;
                this.serializedCards = new List<string>();
                foreach (var cardInfo in cards)
                {
                    this.serializedCards.Add(cardInfo.gameObject.name);
                }
                this.points = points;
                this.rounds = rounds;
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
