using MischievousPlushies.PlushCode;
using Unity.Netcode;
using HarmonyLib;
using UnityEngine;
using MischievousPlushies;
using System.IO;
using System.Reflection;

namespace MischevoiusPlushies
{
    public class NetworkerPatch
    {
        static GameObject networkerPrefab;
        [HarmonyPostfix, HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
        public static void AddPrefab(ref GameNetworkManager __instance)
        {
            if (networkerPrefab == null)
            {
                networkerPrefab = MischievousPlushies.MischievousPlushies.networkerPrefab;
                networkerPrefab.AddComponent<PlushNetworker>();
                NetworkManager.Singleton.AddNetworkPrefab(networkerPrefab);
                MischievousPlushies.MischievousPlushies.Logger.LogInfo("xˬx PlushNet launchning. xˬx");
            }

            //UnityNetcodePatcher stuff
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }

        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Awake))]
        static void SpawnNetworkHandler()
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                var networkHandlerHost = Object.Instantiate(networkerPrefab, Vector3.zero, Quaternion.identity);
                networkHandlerHost.GetComponent<NetworkObject>().Spawn();
            }
        }

    }
}