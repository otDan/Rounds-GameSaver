﻿using UnityEngine;
using UnityEngine.UI;

namespace GameSaver.Asset;

public static class AssetManager
{
    private static readonly AssetBundle GameSaverAssetsBundle = Jotunn.Utils.AssetUtils.LoadAssetBundleFromResources("gamesaver_assets", typeof(GameSaver).Assembly);

    public static readonly GameObject Saving = GameSaverAssetsBundle.LoadAsset<GameObject>("Saving");
    public static readonly GameObject ElementSection = GameSaverAssetsBundle.LoadAsset<GameObject>("ElementSection");
    public static readonly GameObject Section = GameSaverAssetsBundle.LoadAsset<GameObject>("Section");
    public static readonly GameObject SaveInfo = GameSaverAssetsBundle.LoadAsset<GameObject>("SaveInfo");

    public static readonly GameObject GameButton = GameSaverAssetsBundle.LoadAsset<GameObject>("GameButton");
    public static readonly GameObject RoundButton = GameSaverAssetsBundle.LoadAsset<GameObject>("RoundButton");

    public static readonly GameObject Player = GameSaverAssetsBundle.LoadAsset<GameObject>("Player");
    public static readonly GameObject Card = GameSaverAssetsBundle.LoadAsset<GameObject>("Card");
    public static readonly GameObject Point = GameSaverAssetsBundle.LoadAsset<GameObject>("Point");

    public static readonly GameObject CardPreview = GameSaverAssetsBundle.LoadAsset<GameObject>("CardPreview");

    public static readonly GameObject Delete = GameSaverAssetsBundle.LoadAsset<GameObject>("Delete");
    public static readonly Image Trash = GameSaverAssetsBundle.LoadAsset<Image>("Trash");
    public static readonly Image TrashOpen = GameSaverAssetsBundle.LoadAsset<Image>("TrashOpen");
}