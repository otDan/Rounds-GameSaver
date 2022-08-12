using BepInEx.Configuration;

namespace GameSaver.Util
{
    internal class ConfigManager
    {
        public static ConfigEntry<bool> SaveEnabledConfig;
        public static ConfigEntry<bool> SaveAsHostEnabledConfig;
        
        public static bool GameSaverSave;
        public static bool GameSaverSaveAsHost;
    }
}
