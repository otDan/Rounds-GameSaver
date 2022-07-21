using UnityEngine;

namespace GameSaver.Asset
{
    public static class AssetManager
    {
        private static readonly AssetBundle GameSaverAssetsBundle = Jotunn.Utils.AssetUtils.LoadAssetBundleFromResources("gamesaver_assets", typeof(GameSaver).Assembly);
    }
}
