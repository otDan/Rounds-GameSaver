using System.Collections.Generic;
using System.Linq;

namespace GameSaver.Util;

internal class SteamManager
{
    public static Dictionary<int, ulong> steamIds;

    public static Player GetPlayerFromSteamId(ulong steamId)
    {
        return PlayerManager.instance.players.FirstOrDefault(player => steamIds[player.data.view.OwnerActorNr] == steamId);
    }
}