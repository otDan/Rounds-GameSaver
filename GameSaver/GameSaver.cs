using System;
using BepInEx;
using GameSaver.Network;
using GameSaver.Util;
using HarmonyLib;
using UnboundLib;
using UnboundLib.GameModes;
using UnboundLib.Networking;

namespace GameSaver
{
    [BepInDependency("com.willis.rounds.unbound")]
    [BepInDependency("pykess.rounds.plugins.moddingutils")]
    [BepInDependency("io.olavim.rounds.rwf")]
    [BepInPlugin(ModId, CompatibilityModName, Version)]
    [BepInProcess("Rounds.exe")]
    public class GameSaver : BaseUnityPlugin
    {
        private const string ModId = "ot.dan.rounds.gamesaver";
        private const string ModName = "Game Saver";
        public const string Version = "1.0.1";
        public const string ModInitials = "";
        private const string CompatibilityModName = "GameSaver";
        public static GameSaver Instance { get; private set; }
        private const bool DEBUG = false;

        private void Awake()
        {
            Instance = this;

            var harmony = new Harmony(ModId);
            harmony.PatchAll();

            SaveManager.Initialize();
        }

        private void Start()
        {
            GameModeManager.AddHook(GameModeHooks.HookGameStart, SaveManager.GameStart);
            GameModeManager.AddHook(GameModeHooks.HookRoundEnd, SaveManager.RoundCounter);
            GameModeManager.AddHook(GameModeHooks.HookPickStart, SaveManager.PickStart);
            GameModeManager.AddHook(GameModeHooks.HookPickEnd, SaveManager.PickEnd);
            gameObject.AddComponent<LobbyMonitor>();
        }

        public void Log(string debug)
        {
            if (DEBUG) Logger.LogInfo(debug);
        }
    }
}