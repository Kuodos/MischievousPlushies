using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;
using BepInEx.Configuration;
class PlushConfig
{
    public readonly ConfigEntry<bool> logAll;
    public readonly ConfigEntry<int> plushRarity;
    public readonly ConfigEntry<int> plushValueMin,plushValueMax;

    public PlushConfig(ConfigFile cfg)
    {
        plushRarity = cfg.Bind(
            "General",
            "PlushRarity",
            20,
            new ConfigDescription("How common plushies are")
        );

        plushValueMin = cfg.Bind(
            "General", 
            "PlushValueMin", 
            40,
            new ConfigDescription("Minimum plush scrap value")
        );
        plushValueMax = cfg.Bind(
            "General",
            "PlushValueMax", 
            110,
            new ConfigDescription("Maximum plush scrap value")
        );
        logAll = cfg.Bind(
            "Debug",
            "logAll", 
            false,
            new ConfigDescription("Send more logs")
        );

        cfg.SaveOnConfigSet = false;         
        ClearOrphanedEntries(cfg); 
        cfg.Save(); 
        cfg.SaveOnConfigSet = true; 
    }

    static void ClearOrphanedEntries(ConfigFile cfg) 
    { 
        // Find the private property `OrphanedEntries` from the type `ConfigFile`
        PropertyInfo orphanedEntriesProp = AccessTools.Property(typeof(ConfigFile), "OrphanedEntries"); 
        // And get the value of that property from our ConfigFile instance
        var orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProp.GetValue(cfg); 
        // And finally, clear the `OrphanedEntries` dictionary
        orphanedEntries.Clear(); 
    } 
}