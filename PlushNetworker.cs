using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameNetcodeStuff;
using MischievousPlushies.PlushCode;
using Unity.Netcode;
using UnityEngine;

namespace MischievousPlushies
{
    public class PlushNetworker : NetworkBehaviour
    {
        public static PlushNetworker Instance { get; private set; }
        public static List<ulong>? AlivePlayersNetworkObjects { get; private set; }
        public override void OnNetworkSpawn()
        {
            Instance = this;
            MischievousPlushies.Logger.LogInfo("xˬx PlushNet operational. Launching nuclear missiles. xˬx");
            AlivePlayersNetworkObjects = new List<ulong>();
            UpdateAlivePlayerListServerRPC();
            if (!IsHost)
            {
            }
            base.OnNetworkSpawn();
        }
        [ServerRpc(RequireOwnership = false)]
        public void UpdateAlivePlayerListServerRPC()
        {
            if (AlivePlayersNetworkObjects == null) return;
            AlivePlayersNetworkObjects.Clear();
            PlayerControllerB[] players = StartOfRound.Instance.allPlayerScripts;
            foreach (PlayerControllerB player in players)
            {
                if (!player.isPlayerDead && player.isPlayerControlled)
                {
                    AlivePlayersNetworkObjects.Add(player.GetComponent<NetworkObject>().NetworkObjectId);
                }
            }
            LifeboundExploderCheckOwners();
        }
        [ServerRpc(RequireOwnership = false)]
        public void RequestCosplayerListServerRPC(ulong clientId)
        {
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientId }
                }
            };
            PlushCosplayer.PruneConvertedList();
            List<GrabbableObject> cosplayerList = PlushCosplayer.ConvertedPlushies;
            List<ulong> cosplayerIds=new List<ulong>();
            foreach (GrabbableObject cosplayer in cosplayerList){
                cosplayerIds.Add(cosplayer.NetworkObjectId);
            }
            SendCosplayerListClientRPC(cosplayerIds.ToArray(), clientRpcParams);
        }

        [ClientRpc]
        public void SendCosplayerListClientRPC(ulong[] CosplayerList, ClientRpcParams clientRpcParams)
        {
            if(IsOwner) return;
            PlushCosplayer.SetConvertedPlushies(CosplayerList);
            MischievousPlushies.Logger.LogInfo("xˬx PlushNet: Sent convertees list. xˬx");
        }

        [ClientRpc]
        public void CosplayClientRPC(ulong source, ulong target)
        {
            NetworkObject src = NetworkManager.Singleton.SpawnManager.SpawnedObjects[source];
            NetworkObject objRef = NetworkManager.Singleton.SpawnManager.SpawnedObjects[target];
            GrabbableObject obj = objRef.GetComponent<GrabbableObject>();
            src.GetComponent<PlushCosplayer>().PlushConvert(obj);
        }
        [ServerRpc(RequireOwnership = false)]
        public void SetLifeboundExploderServerRPC(ulong sourceId, ulong targetId)
        {
            SetLifeboundExploderClientRPC(sourceId, targetId);
        }
        [ClientRpc]
        public void SetLifeboundExploderClientRPC(ulong sourceId, ulong targetId)
        {
            NetworkObject source = NetworkManager.Singleton.SpawnManager.SpawnedObjects[sourceId];
            NetworkObject target = NetworkManager.Singleton.SpawnManager.SpawnedObjects[targetId];
            source.GetComponent<PlushLifeboundExploder>().SetOwner(target.GetComponent<PlayerControllerB>());
        }
        [ClientRpc]
        public void ExplodeLifeboundExploderClientRPC(ulong sourceId)
        {
            NetworkObject source = NetworkManager.Singleton.SpawnManager.SpawnedObjects[sourceId];
            source.GetComponent<PlushLifeboundExploder>().Explode();
        }
        public void LifeboundExploderCheckOwners()
        {
            foreach (PlushLifeboundExploder exploder in GameObject.FindObjectsByType<PlushLifeboundExploder>(FindObjectsSortMode.None))
            {
                if (exploder.isOwnerDead())
                {
                    ExplodeLifeboundExploderClientRPC(exploder.GetComponent<NetworkObject>().NetworkObjectId);
                }
            }
        }
    }
}