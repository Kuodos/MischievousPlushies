using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace MischievousPlushies.PlushCode
{
    public class PlushLifeboundExploder : MonoBehaviour
    {
        public AudioClip? explodeClip;
        private PlushGrabbableObject plushObj { get; set; } = null!;
        private PlayerControllerB ownerObj { get; set; } = null!;
        private static bool isHost => RoundManager.Instance.NetworkManager.IsHost;
        private bool ignoreNewOwners = false, exploded=false;
        public void Init()
        {
            if(plushObj==null) plushObj = GetComponent<PlushGrabbableObject>();
            plushObj.onGrabEvent.AddListener(OnGrab);
        }
        public void OnGrab(){
            if(ownerObj==null&&!ignoreNewOwners){
                ulong thisNetworkID = transform.GetComponent<NetworkObject>().NetworkObjectId;
                ulong ownerNetworkID = GameNetworkManager.Instance.localPlayerController.GetComponent<NetworkObject>().NetworkObjectId;
                PlushNetworker.Instance.SetLifeboundExploderServerRPC(thisNetworkID,ownerNetworkID);
                ignoreNewOwners=true;
            }
        }
        public void SetOwner(PlayerControllerB owner){
            if(ignoreNewOwners) return;
            //MischievousPlushies.Logger.LogInfo("New owner: " + owner.playerUsername);
            ownerObj=owner;
            ignoreNewOwners=true;
        }
        public bool isOwnerDead(){
            if(ownerObj==null) return false;
            if(ownerObj.isPlayerDead||!ownerObj.isPlayerControlled){
                return true;
            }
            return false;
        } 
        public void Explode(){
            if(exploded) return;
            exploded=true;
            /*if(plushObj.isHeld||plushObj.isPocketed){
                plushObj.DiscardItemClientRpc();
            }*/
            //MischievousPlushies.Logger.LogInfo("Kaboom!");
            if(explodeClip!=null) GetComponent<AudioSource>().PlayOneShot(explodeClip,1f);
            Landmine.SpawnExplosion(plushObj.transform.position,spawnExplosionEffect:true,2f,5f);
            plushObj.transform.gameObject.SetActive(false);
        }
    }
}