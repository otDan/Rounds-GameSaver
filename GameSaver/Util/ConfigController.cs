using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Configuration;

namespace GameSaver.Util
{
    internal class ConfigController
    {
        public static ConfigEntry<bool> SaveEnabledConfig;
        public static ConfigEntry<bool> SaveAsHostEnabledConfig;
        
        public static bool GameSaverSave;
        public static bool GameSaverSaveAsHost;
    }
}
