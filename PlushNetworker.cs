using System.Collections.Generic;
using System.Linq;
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
        private static bool isHost => RoundManager.Instance.NetworkManager.IsHost;
        public override void OnNetworkSpawn()
        {
            Instance = this;
            MischievousPlushies.LogInfo("xˬx PlushNet operational. Launching nuclear missiles. xˬx");
            AlivePlayersNetworkObjects = new List<ulong>();
            UpdateAlivePlayerListServerRPC();
            if (IsHost)
            {

            }
            base.OnNetworkSpawn();
        }
        public static void OnEnemySpawn()
        {
            //if(!StartOfRound.Instance.shipHasLanded) return;
            if (isHost) Instance.UpdateBeeHivesClientRPC();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
        }
        [ServerRpc(RequireOwnership = false)]
        public void DiscardHeldItemMinifierServerRPC(ulong source)
        {
            DiscardHeldItemMinifierClientRPC(source);
        }
        [ClientRpc]
        public void DiscardHeldItemMinifierClientRPC(ulong source)
        {
            NetworkObject src = NetworkManager.Singleton.SpawnManager.SpawnedObjects[source];
            src.GetComponentInChildren<MinifyingObjectSurface>().DiscardHeldItem();
        }

        [ClientRpc]
        public void SetAutoObjectParentSyncClientRPC(ulong source, ulong grabbableObject, bool state)
        {
            MinifyingObjectSurface src = NetworkManager.Singleton.SpawnManager.SpawnedObjects[source].GetComponentInChildren<MinifyingObjectSurface>();
            GrabbableObject grabbable = NetworkManager.Singleton.SpawnManager.SpawnedObjects[grabbableObject].GetComponent<GrabbableObject>();
            src.prevParentSync = grabbable.NetworkObject.AutoObjectParentSync;
            grabbable.NetworkObject.AutoObjectParentSync = state;
        }
        [ClientRpc]
        public void AnimatorSyncBoolClientRPC(ulong source, string var, bool val)
        {
            Animator src = NetworkManager.Singleton.SpawnManager.SpawnedObjects[source].GetComponentInChildren<Animator>();
            src.SetBool(var, val);
        }
        [ClientRpc]
        public void AnimatorSyncPlayClientRPC(ulong source, string var)
        {
            Animator src = NetworkManager.Singleton.SpawnManager.SpawnedObjects[source].GetComponentInChildren<Animator>();
            src.Play(var);
        }
        [ServerRpc(RequireOwnership = false)]
        public void PlaceObjectMinifierServerRPC(ulong source, ulong player, ulong grabbableObject, bool sendToOwner)
        {
            MischievousPlushies.LogInfo("PlaceObjectMinifierServerRPC to "+sendToOwner);
            MinifyingObjectSurface src = NetworkManager.Singleton.SpawnManager.SpawnedObjects[source].GetComponentInChildren<MinifyingObjectSurface>();
            PlayerControllerB playerobj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[player].GetComponent<PlayerControllerB>();
            GrabbableObject grabbable = NetworkManager.Singleton.SpawnManager.SpawnedObjects[grabbableObject].GetComponent<GrabbableObject>();
            if (sendToOwner)
            {
                SetAutoObjectParentSyncClientRPC(source, grabbableObject, false);
                ClientRpcParams clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { playerobj.actualClientId }
                    }
                };

                MischievousPlushies.LogInfo("Sent to "+playerobj.actualClientId);
                PlaceObjectMinifierClientRPC(source, player, grabbableObject, clientRpcParams);
            }
            else
            {
                List<ulong> targets = new List<ulong>();
                foreach (PlayerControllerB plr in StartOfRound.Instance.allPlayerScripts)
                {
                    MischievousPlushies.LogInfo("ClienID added "+plr.actualClientId);
                    targets.Add(plr.actualClientId);
                }
                targets=targets.Distinct().ToList();
                targets.Remove(playerobj.actualClientId); //send to everyone but the owner
                ClientRpcParams clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = targets
                    }
                };
                foreach(ulong target in targets){
                    MischievousPlushies.LogInfo("Sent to "+target);
                }
                PlaceObjectMinifierClientRPC(source, player, grabbableObject, clientRpcParams);
            }
        }
        [ClientRpc]
        public void PlaceObjectMinifierClientRPC(ulong source, ulong player, ulong grabbableObject, ClientRpcParams clientRpcParams)
        {
            MinifyingObjectSurface src = NetworkManager.Singleton.SpawnManager.SpawnedObjects[source].GetComponentInChildren<MinifyingObjectSurface>();
            PlayerControllerB playerobj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[player].GetComponent<PlayerControllerB>();
            GrabbableObject grabbable = NetworkManager.Singleton.SpawnManager.SpawnedObjects[grabbableObject].GetComponent<GrabbableObject>();
            
            StartCoroutine(src.PlaceObject(playerobj, grabbable));
        }
        [ClientRpc]
        public void WindupShovelClientRPC(ulong source)
        {
            PlushShovelWindup plush = NetworkManager.Singleton.SpawnManager.SpawnedObjects[source].GetComponent<PlushShovelWindup>();
            plush.WindupShovel();
        }
        [ClientRpc]
        public void WindupShovelHitClientRPC(ulong source, ulong target)
        {   
            IHittable hittable = NetworkManager.Singleton.SpawnManager.SpawnedObjects[target].GetComponentInChildren<IHittable>();
            PlushShovelWindup plush = NetworkManager.Singleton.SpawnManager.SpawnedObjects[source].GetComponent<PlushShovelWindup>();
            
            MischievousPlushies.LogInfo(plush.gameObject.name + " hits " + hittable.ToString());
            plush.StartAttackSequence(hittable);
        }
        [ClientRpc]
        public void UpdateBeeHivesClientRPC()
        {
            PlushBeeMagnet.UpdateBeeHives();
        }
        [ClientRpc]
        public void SetTargetClientRPC(ulong agent, Vector3 target)
        {
            NetworkObject src = NetworkManager.Singleton.SpawnManager.SpawnedObjects[agent];
            src.GetComponent<GrabbableNavMeshAgent>().SetTargetPosition(target);
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
            List<ulong> cosplayerIds = new List<ulong>();
            foreach (GrabbableObject cosplayer in cosplayerList)
            {
                cosplayerIds.Add(cosplayer.NetworkObjectId);
            }
            SendCosplayerListClientRPC(cosplayerIds.ToArray(), clientRpcParams);
        }

        [ClientRpc]
        public void SendCosplayerListClientRPC(ulong[] CosplayerList, ClientRpcParams clientRpcParams)
        {
            if (IsOwner) return;
            PlushCosplayer.SetConvertedPlushies(CosplayerList);
            MischievousPlushies.LogInfo("xˬx PlushNet: Sent convertees list. xˬx");
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

        [ClientRpc]
        public void TeleportPlayerClientRPC(ulong playerId, Vector3 pos)
        {
            NetworkObject source = NetworkManager.Singleton.SpawnManager.SpawnedObjects[playerId];
            source.GetComponent<PlayerControllerB>().TeleportPlayer(pos);
        }
        [ClientRpc]
        public void TeleportItemClientRPC(ulong itemId, Vector3 pos)
        {
            GrabbableObject item = NetworkManager.Singleton.SpawnManager.SpawnedObjects[itemId].GetComponent<GrabbableObject>();
            //if(item.playerHeldBy==GameNetworkManager.Instance.localPlayerController) item.DiscardItemClientRpc();

            item.targetFloorPosition = pos;
        }
        [ClientRpc]
        public void TeleportNavMeshItemClientRPC(ulong itemId, Vector3 pos)
        {
            GrabbableNavMeshAgent item = NetworkManager.Singleton.SpawnManager.SpawnedObjects[itemId].GetComponent<GrabbableNavMeshAgent>();
            item.Teleport(pos);
        }
        [ClientRpc]
        public void StopPathingClientRPC(ulong agent)
        {
            NetworkObject source = NetworkManager.Singleton.SpawnManager.SpawnedObjects[agent];
            source.GetComponent<GrabbableNavMeshAgent>().StopPathing();
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