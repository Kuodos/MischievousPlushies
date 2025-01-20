using HarmonyLib;
using GameNetcodeStuff;

namespace MischievousPlushies.Patches
{
    public class ReportDeathsPatch
    {
        [HarmonyPostfix, HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.KillPlayerServerRpc))]
        private static void ReportDeath(){
           // MischievousPlushies.Logger.LogInfo("Someone died. RIP");
            PlushNetworker.Instance.UpdateAlivePlayerListServerRPC();
        }
        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.PlayerHasRevivedServerRpc))]
        private static void ReportRevive()
        {
           // MischievousPlushies.Logger.LogInfo("Someone revived. LIP");
            PlushNetworker.Instance.UpdateAlivePlayerListServerRPC();
        }
    }  
}