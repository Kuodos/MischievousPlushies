using GameNetcodeStuff;
using UnityEngine;

namespace MischievousPlushies.PlushCode
{
    public class PlushWalking : MonoBehaviour
    {
        private static System.Random random;
        private PlushGrabbableObject plushObj { get; set; } = null!;
        private static bool isHost => RoundManager.Instance.NetworkManager.IsHost;
        private static bool isInActivePhase => !StartOfRound.Instance.inShipPhase && StartOfRound.Instance.shipHasLanded && !StartOfRound.Instance.shipIsLeaving;
        private Animator anim { get; set; } = null!;
        private MinifyingObjectSurface miniObj { get; set; } = null!;
        private float CheckAnimTimer = 10f;
        private GrabbableNavMeshAgent agent { get; set; } = null!;
        private static float CheckAnimMax = 1f, plushSpeed = 5f;
        private bool active = false;
        private PlayerControllerB? playerTarget;

        private void Start()
        {
            random ??= new System.Random();
            plushObj = GetComponent<PlushGrabbableObject>();
            anim = GetComponentInChildren<Animator>();
            miniObj = GetComponentInChildren<MinifyingObjectSurface>();

            agent = gameObject.AddComponent<GrabbableNavMeshAgent>();
            agent.Init(plushSpeed, 0.05f, true, 1f);
            agent.StopPathing();

            miniObj.onItemStored.AddListener(OnItemStored);
            miniObj.onItemDiscarded.AddListener(OnItemDiscarded);

            if (isHost)
            {
                plushObj.onGrabEvent.AddListener(ForceDiscardItem);
            }
        }

        private void OnItemStored(PlayerControllerB storedBy)
        {
            if (!isInActivePhase) return;
            active = true;
            playerTarget = storedBy;
            anim.SetBool("isActive", active);
            CheckAnimTimer = anim.GetCurrentAnimatorStateInfo(0).length;
        }
        private void ForceDiscardItem()
        {
            miniObj.DiscardHeldItemSendRPC();
            active = false;
        }
        private void OnItemDiscarded()
        {
            if (active) PlushNetworker.Instance.StopPathingClientRPC(plushObj.NetworkObjectId);
            active = false;
            playerTarget = null;
            anim.SetBool("isActive", active);
            plushObj.isInFactory = (plushObj.transform.position.y < -80); //should work?
            plushObj.isInShipRoom = StartOfRound.Instance.shipBounds.bounds.Contains(plushObj.transform.position);
            plushObj.isInElevator = plushObj.isInShipRoom;
            if (plushObj.isInShipRoom)
            {
                plushObj.transform.SetParent(StartOfRound.Instance.elevatorTransform);
                plushObj.targetFloorPosition=plushObj.transform.parent.InverseTransformPoint(plushObj.transform.position);
            }
            CheckAnimTimer = anim.GetCurrentAnimatorStateInfo(0).length;
        }
        public void AnimatorPlaySyncedSendRPC(string animation)
        {
            PlushNetworker.Instance.AnimatorSyncPlayClientRPC(plushObj.NetworkObjectId, animation);
        }
        public void AnimatorSetBoolSyncedSendRPC(string var, bool value)
        {
            PlushNetworker.Instance.AnimatorSyncBoolClientRPC(plushObj.NetworkObjectId, var, value);
        }
        private void Update()
        {
            if (!isHost) return;
            CheckAnimTimer -= Time.deltaTime;
            float dist = 0;
            if (playerTarget != null)
            {
                if (!playerTarget.isPlayerDead)
                {
                    dist = Vector3.Distance(transform.position, playerTarget.thisPlayerBody.position);
                }
                else dist = Vector3.Distance(transform.position, playerTarget.placeOfDeath);
            }
            if (dist < 3f)
            {
                if(anim.GetBool("isRunning")==true) AnimatorSetBoolSyncedSendRPC("isRunning", false);
            }
            if (CheckAnimTimer < 0)
            {
                int roll = random.Next(0, 200);
                CheckAnimTimer = CheckAnimMax;
                if (active && !isInActivePhase)
                {
                    ForceDiscardItem();
                }
                if (active && playerTarget != null && isInActivePhase)
                {
                    if (dist < 3f)
                    {
                        if (playerTarget.isPlayerDead)
                        {
                            ForceDiscardItem();
                            MischievousPlushies.LogInfo("Player died, discarding");
                        }
                        if (roll < 3)
                        {
                            AnimatorPlaySyncedSendRPC("DanceStanding");
                            CheckAnimTimer += anim.GetCurrentAnimatorStateInfo(0).length;
                        }
                        else if (roll < 5)
                        {
                            AnimatorPlaySyncedSendRPC("LookWatch");
                        }
                        else if (roll < 7)
                        {
                            AnimatorPlaySyncedSendRPC("LookAround");
                        }
                    }
                    else
                    {
                        AnimatorSetBoolSyncedSendRPC("isRunning", true);
                        Vector3 targetPosition;
                        if (playerTarget.isInsideFactory != plushObj.isInFactory)
                        {
                            EntranceTeleport? targetDoor = NearestEntranceTeleport();
                            if (targetDoor == null) return;
                            targetPosition = targetDoor.entrancePoint.position;
                            if (Vector3.Distance(targetPosition, plushObj.transform.position) < 3f)
                            {
                                Vector3 otherSide = targetDoor.exitPoint.position;
                                PlushNetworker.Instance.TeleportNavMeshItemClientRPC(plushObj.NetworkObjectId, otherSide);

                                targetDoor.PlayAudioAtTeleportPositions();
                                if (!playerTarget.isPlayerDead) targetPosition = Vector3.MoveTowards(playerTarget.thisPlayerBody.position, transform.position, 2f);
                                else targetPosition = Vector3.MoveTowards(playerTarget.placeOfDeath, transform.position, 2f);
                            }
                        }
                        else
                        {
                            if (!playerTarget.isPlayerDead) targetPosition = Vector3.MoveTowards(playerTarget.thisPlayerBody.position, transform.position, 2f);
                            else targetPosition = Vector3.MoveTowards(playerTarget.placeOfDeath, transform.position, 2f);
                        }
                        PlushNetworker.Instance.SetTargetClientRPC(plushObj.NetworkObjectId, targetPosition);
                    }
                }
                else
                {
                    if (isInActivePhase && !plushObj.isHeld)
                    {
                        if (roll < 1)
                        {
                            AnimatorPlaySyncedSendRPC("Breakdance");
                            CheckAnimTimer += anim.GetCurrentAnimatorStateInfo(0).length;
                        }
                        else if (roll < 2)
                        {
                            AnimatorPlaySyncedSendRPC("DanceSitting");
                            CheckAnimTimer += anim.GetCurrentAnimatorStateInfo(0).length;
                        }
                    }
                }
                // CheckAnimTimer += anim.GetCurrentAnimatorStateInfo(0).length;
            }
        }

        private EntranceTeleport? NearestEntranceTeleport()
        {
            EntranceTeleport[] entrances = FindObjectsByType<EntranceTeleport>(FindObjectsSortMode.None);
            float dist = float.MaxValue;
            EntranceTeleport? entr = null;
            foreach (EntranceTeleport entrance in entrances)
            {

                if (!entrance.gotExitPoint)
                {
                    if (!entrance.FindExitPoint()) continue;
                }
                float newDist = Vector3.Distance(plushObj.transform.position, entrance.entrancePoint.position);
                if (newDist < dist)
                {
                    entr = entrance;
                    dist = newDist;
                }
            }
            MischievousPlushies.LogInfo("Nearest entrance:" + entr?.name + " at " + entr?.entrancePoint.position);
            return entr;
        }
    }
}