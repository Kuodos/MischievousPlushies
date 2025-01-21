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
namespace MischievousPlushies;

[BepInPlugin(GUID, NAME, VERSION)]
[BepInDependency(LethalLib.Plugin.ModGUID)]
[BepInDependency("qwbarch.Mirage",BepInDependency.DependencyFlags.SoftDependency)]
public class MischievousPlushies : BaseUnityPlugin
{
    public const string GUID = "Kuodos.MischievousPlushies";
    public const string NAME = "MischievousPlushies";
    public const string VERSION = "0.3.0";
    private static AssetBundle plushieAssets { get; set; } = null!;
    public static GameObject networkerPrefab { get; set; } = null!;
    public static MischievousPlushies Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger { get; private set; } = null!;
    internal static Harmony harmony { get; set; }  = null!;

    private void Awake()
    {
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
        int iRarity = 25;
        Item[] Plushies = plushieAssets.LoadAllAssets<Item>();

        foreach(Item item in Plushies){
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(item.spawnPrefab);
            LethalLib.Modules.Items.RegisterScrap(item, iRarity, LethalLib.Modules.Levels.LevelTypes.All);
        }
        PlushCosplayer.PlushieAllowList=Plushies.ToList();
        
        networkerPrefab=plushieAssets.LoadAsset<GameObject>("PlushNetworker");
        Logger.LogInfo(harmony.Id);
        //harmony.PatchAll();
        Harmony.CreateAndPatchAll(typeof(NetworkerPatch));
        Harmony.CreateAndPatchAll(typeof(ReportDeathsPatch));
        Logger.LogInfo("MischievousPlushies is loaded! xˬx ");
    }

    internal static void Unpatch()
    {
        Logger.LogDebug("Unpatching...");

        harmony.UnpatchSelf();

        Logger.LogDebug("Finished unpatching!");
    }
}
