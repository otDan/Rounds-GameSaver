using BepInEx;
using GameSaver.Network;
using GameSaver.Util;
using HarmonyLib;
using UnboundLib.GameModes;

namespace GameSaver;

[BepInDependency("com.willis.rounds.unbound")]
[BepInDependency("pykess.rounds.plugins.moddingutils")]
[BepInDependency("io.olavim.rounds.rwf")]
[BepInPlugin(ModId, CompatibilityModName, Version)]
[BepInProcess("Rounds.exe")]
public class GameSaver : BaseUnityPlugin
{
    private const string ModId = "ot.dan.rounds.gamesaver";
    private const string ModName = "Game Saver";
    public const string Version = "1.0.6";
    public const string ModInitials = "GS";
    private const string CompatibilityModName = "GameSaver";
    public static GameSaver Instance { get; private set; }

    internal void Awake()
    {
        Instance = this;

        var harmony = new Harmony(ModId);
        harmony.PatchAll();

        SaveManager.Initialize();
    }

    internal void Start()
    {
        GameModeManager.AddHook(GameModeHooks.HookGameStart, SaveManager.GameStart, 0);
        GameModeManager.AddHook(GameModeHooks.HookGameEnd, SaveManager.GameEnd);
        GameModeManager.AddHook(GameModeHooks.HookRoundStart, SaveManager.RoundStart);
        GameModeManager.AddHook(GameModeHooks.HookRoundEnd, SaveManager.RoundEnd);
        GameModeManager.AddHook(GameModeHooks.HookPickStart, SaveManager.PickStart);
        // GameModeManager.AddHook(GameModeHooks.HookPickEnd, SaveManager.PickEnd);
            
        gameObject.AddComponent<LobbyMonitor>();
    }

    public void Log(string debug)
    {
        Logger.LogInfo(debug);
    }
}