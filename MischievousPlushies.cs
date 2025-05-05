using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using System.IO;
using System.Reflection;
using MischevoiusPlushies;
using MischievousPlushies.PlushCode;
using System.Linq;
using GameNetcodeStuff;
using MischievousPlushies.Patches;
using System;
namespace MischievousPlushies;

[BepInPlugin(GUID, NAME, VERSION)]
[BepInDependency(LethalLib.Plugin.ModGUID)]
[BepInDependency("qwbarch.Mirage",BepInDependency.DependencyFlags.SoftDependency)]
public class MischievousPlushies : BaseUnityPlugin
{
    public const string GUID = "Kuodos.MischievousPlushies";
    public const string NAME = "MischievousPlushies";
    public const string VERSION = "0.5.1";
    private static AssetBundle plushieAssets { get; set; } = null!;
    public static GameObject networkerPrefab { get; set; } = null!;
    public static MischievousPlushies Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger { get; private set; } = null!;
    internal static Harmony harmony { get; set; }  = null!;
    internal static PlushConfig PlushConfig { get; private set; } = null!; 
    private void Awake()
    {
        PlushConfig = new PlushConfig(base.Config);
        harmony = new Harmony(GUID);
        Logger = base.Logger;
        Instance = this;
        string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        plushieAssets = AssetBundle.LoadFromFile(Path.Combine(sAssemblyLocation, "plushiesbundle"));
        if (plushieAssets == null)
        {
            Logger.LogError("Failed to load custom assets.");
            return;
        }
        int iRarity = PlushConfig.plushRarity.Value;
        Item[] Plushies = plushieAssets.LoadAllAssets<Item>();

        foreach(Item item in Plushies){
            item.minValue=(int)Math.Round(PlushConfig.plushValueMin.Value*2.5f); //since it will be multiplied by 0.4 later
            item.maxValue=(int)Math.Round(PlushConfig.plushValueMax.Value*2.5f);
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(item.spawnPrefab);
            LethalLib.Modules.Items.RegisterScrap(item, iRarity, LethalLib.Modules.Levels.LevelTypes.All);
        }
        PlushCosplayer.PlushieAllowList=Plushies.ToList();
        
        networkerPrefab=plushieAssets.LoadAsset<GameObject>("PlushNetworker");
        Logger.LogInfo(harmony.Id);
        //harmony.PatchAll();
        Harmony.CreateAndPatchAll(typeof(NetworkerPatch));
        Harmony.CreateAndPatchAll(typeof(ReportDeathsPatch));
        Harmony.CreateAndPatchAll(typeof(EnemySpawnReportPatch));
        Logger.LogInfo("MischievousPlushies is loaded! xˬx ");
    }
    public static void LogInfo(string text){
        if(PlushConfig.logAll.Value) Logger.LogInfo(text);
    }
    public static void LogError(string text){
        if(PlushConfig.logAll.Value) Logger.LogError(text);
    }
    internal static void Unpatch()
    {
        Logger.LogDebug("Unpatching...");

        harmony.UnpatchSelf();

        Logger.LogDebug("Finished unpatching!");
    }
}
