using System.Collections.Generic;
using System.Linq;

namespace GameSaver.Util;

internal class SteamManager
{
    public static Dictionary<int, ulong> steamIds;

    public static Player GetPlayerFromSteamId(ulong steamId)
    {
        foreach (var player in PlayerManager.instance.players)
        {
            if (!steamIds.ContainsKey(player.data.view.OwnerActorNr)) continue;
            if (steamIds[player.data.view.OwnerActorNr] != steamId) continue;
            return player;
        }

        return null;
    }
}