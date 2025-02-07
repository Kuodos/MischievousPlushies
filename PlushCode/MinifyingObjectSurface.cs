using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using System.Collections;
using UnityEngine.Events;

namespace MischievousPlushies.PlushCode
{
    public class MinifyingObjectSurface : NetworkBehaviour
    {
        public NetworkObject parentTo;
        public Collider placeableBounds;

        public InteractTrigger triggerScript;
        private GrabbableObject? objectHeld;
        private static float sizeMultiplier = 0.6f;
        private static bool matchRotation = true;
        private static bool isInActivePhase => !StartOfRound.Instance.inShipPhase&&StartOfRound.Instance.shipHasLanded&&!StartOfRound.Instance.shipIsLeaving;
        public bool prevParentSync = false, prevGrabbableToEnemies = false;
        public UnityEvent<PlayerControllerB> onItemStored;
        public UnityEvent onItemDiscarded;
        private static bool isHost => RoundManager.Instance.NetworkManager.IsHost;

       // private Transform? lastParent = null;
       // private int framesSinceParentChange = 0;
        private void OnEnable()
        {
            onItemDiscarded = new UnityEvent();
            onItemStored = new UnityEvent<PlayerControllerB>();
        }
        private void OnDisable()
        {
            onItemDiscarded.RemoveAllListeners();
            onItemStored.RemoveAllListeners();
        }
        private void Update()
        {
            if (GameNetworkManager.Instance != null && GameNetworkManager.Instance.localPlayerController != null)
            {
                triggerScript.interactable = GameNetworkManager.Instance.localPlayerController.isHoldingObject && (objectHeld == null) && isInActivePhase &&!parentTo.GetComponent<GrabbableObject>().isHeld;
                placeableBounds.enabled = triggerScript.interactable;
            }

            /*if (objectHeld != null)
            {
                framesSinceParentChange++;
                if (lastParent != objectHeld.transform.parent)
                {
                    lastParent = objectHeld.transform.parent;
                    MischievousPlushies.LogInfo(Time.time + " " + (isHost ? "host" : "client") + " " + framesSinceParentChange + " obj parent changed to:" + objectHeld.transform.parent.name + objectHeld.name);
                    framesSinceParentChange = 0;
                }
            }*/

            if (objectHeld != null && isHost)
            {
                if (objectHeld.isHeld)
                {
                    MischievousPlushies.LogInfo("Obj held by " + objectHeld.playerHeldBy.name + ", discarding");
                    DiscardHeldItemSendRPC();
                }
            }
        }
        public void DiscardHeldItemSendRPC()
        {
            PlushNetworker.Instance.DiscardHeldItemMinifierServerRPC(parentTo.NetworkObjectId);
        }
        public void DiscardHeldItem()
        {
            if (objectHeld == null) return;
            objectHeld.grabbableToEnemies=prevGrabbableToEnemies;
            MischievousPlushies.LogInfo("Obj discarded from: " + objectHeld.transform.parent);
            objectHeld.transform.SetParent(null, true);

            objectHeld.isInFactory = (objectHeld.transform.position.y<-80); //should work?
            objectHeld.isInShipRoom = StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(objectHeld.transform.position);
            objectHeld.isInElevator = objectHeld.isInShipRoom;

            if(objectHeld.isInShipRoom){
                objectHeld.transform.SetParent(StartOfRound.Instance.elevatorTransform);
                objectHeld.targetFloorPosition = objectHeld.transform.parent.InverseTransformPoint(objectHeld.transform.position);
            }

            if (!objectHeld.isHeld) //plush was picked up or end of round
            {
                objectHeld.targetFloorPosition = transform.position;
                objectHeld.startFallingPosition = objectHeld.targetFloorPosition + Vector3.up;
                objectHeld.FallToGround();
                objectHeld.PlayDropSFX();
            }

            onItemDiscarded.Invoke();
            objectHeld.transform.localScale = objectHeld.originalScale;
            objectHeld.NetworkObject.AutoObjectParentSync = prevParentSync;

            objectHeld = null;
        }

        public void PlaceObjectSendRPC(PlayerControllerB playerWhoTriggered)
        {
            if (!playerWhoTriggered.isHoldingObject || playerWhoTriggered.isGrabbingObjectAnimation || !(playerWhoTriggered.currentlyHeldObjectServer != null))
            {
                return;
            }
            PlushNetworker.Instance.PlaceObjectMinifierServerRPC(parentTo.NetworkObjectId, playerWhoTriggered.NetworkObjectId, playerWhoTriggered.currentlyHeldObjectServer.NetworkObjectId, true);
        }

        public IEnumerator PlaceObject(PlayerControllerB playerWhoTriggered, GrabbableObject obj)
        {
            obj.isHeld = false; //to prevent instant dropping by host
            objectHeld = obj;

            prevGrabbableToEnemies=obj.grabbableToEnemies;
            obj.grabbableToEnemies=false;

            if (GameNetworkManager.Instance.localPlayerController == playerWhoTriggered)
            {
                playerWhoTriggered.DiscardHeldObject(true, parentTo, Vector3.zero, true);
                yield return null;
                PlushNetworker.Instance.PlaceObjectMinifierServerRPC(parentTo.NetworkObjectId, playerWhoTriggered.NetworkObjectId, obj.NetworkObjectId, false);
            }
            
            obj.transform.SetParent(transform, false);

            obj.targetFloorPosition = (obj.itemProperties.verticalOffset > 0.1f ? Vector3.up * (obj.itemProperties.verticalOffset - 0.1f) : Vector3.zero);
            obj.transform.localScale = obj.originalScale * sizeMultiplier;
            if (matchRotation)
            {
                Vector3 matchedRot = obj.itemProperties.restingRotation;
                matchedRot.y = obj.itemProperties.floorYOffset;
                if (obj.name.ToLower().Contains("flashlight")) matchedRot.y = 90;
                if (obj.name.ToLower().Contains("plush")) matchedRot.y -= 90;
                // MischievousPlushies.LogInfo("Obj rot: " + objectHeld.transform.localEulerAngles + "->" + matchedRot);
                objectHeld.transform.localRotation = Quaternion.Euler(matchedRot);
            }
            MischievousPlushies.LogInfo(Time.time + " " + (isHost ? "host" : "client") + "Obj placed:" + obj.transform.parent.name + " sync" + obj.NetworkObject.AutoObjectParentSync + " " + obj.name);
            onItemStored.Invoke(playerWhoTriggered);
        }
    }
}