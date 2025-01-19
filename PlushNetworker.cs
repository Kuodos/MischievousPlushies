using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MischievousPlushies.PlushCode;
using Unity.Netcode;

namespace MischievousPlushies
{
    public class PlushNetworker : NetworkBehaviour
    {

        public static PlushNetworker? Instance { get; private set; }
        public override void OnNetworkSpawn()
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
                Instance?.gameObject.GetComponent<NetworkObject>().Despawn();
            Instance = this;
            MischievousPlushies.Logger.LogInfo("xˬx PlushNet operational. Launching nuclear missiles. xˬx");
            if (!IsHost)
            {
            }
            base.OnNetworkSpawn();
        }

        [ClientRpc]
        public void ConvertClientRPC(ulong source, ulong target){
            NetworkObject src = NetworkManager.Singleton.SpawnManager.SpawnedObjects[source];
            NetworkObject objRef = NetworkManager.Singleton.SpawnManager.SpawnedObjects[target];
            GrabbableObject obj = objRef.GetComponent<GrabbableObject>();
            src.GetComponent<PlushCosplayer>().PlushConvert(obj);
        }
    }
}