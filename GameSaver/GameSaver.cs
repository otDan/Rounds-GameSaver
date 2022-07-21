using BepInEx;
using GameSaver.Util;
using HarmonyLib;
using UnboundLib.GameModes;

namespace GameSaver
{
    [BepInDependency("com.willis.rounds.unbound")]
    [BepInDependency("pykess.rounds.plugins.moddingutils")]
    [BepInDependency("io.olavim.rounds.rwf")]
    [BepInPlugin(ModId, ModName, Version)]
    [BepInProcess("Rounds.exe")]
    public class GameSaver : BaseUnityPlugin
    {
        private const string ModId = "ot.dan.rounds.gamesaver";
        private const string ModName = "Game Saver";
        public const string Version = "1.0.0";
        public const string ModInitials = "";
        private const string CompatibilityModName = "GameSaver";
        public static GameSaver Instance { get; private set; }
        private const bool DEBUG = true;

        private void Awake()
        {
            Instance = this;
            
            var harmony = new Harmony(ModId);
            harmony.PatchAll();
        }

        private void Start()
        {
            GameModeManager.AddHook(GameModeHooks.HookGameStart, SaveHandler.GameStart);
            GameModeManager.AddHook(GameModeHooks.HookRoundEnd, SaveHandler.RoundCounter);
            GameModeManager.AddHook(GameModeHooks.HookPickStart, SaveHandler.PickStart);
            GameModeManager.AddHook(GameModeHooks.HookPickEnd, SaveHandler.PickEnd);
        }

        public void Log(string debug)
        {
            if (DEBUG) UnityEngine.Debug.Log(debug);
        }
    }
}