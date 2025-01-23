using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;

namespace MischievousPlushies.Patches
{
    public class EnemySpawnReportPatch
    {
        [HarmonyPostfix, HarmonyPatch(typeof(RoundManager), nameof(RoundManager.AdvanceHourAndSpawnNewBatchOfEnemies))]
        public static void ReportEnemySpawn(){
            PlushNetworker.OnEnemySpawn();
        }
    }
}