using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace MischievousPlushies.PlushCode
{
    public class PlushBeeMagnet : MonoBehaviour
    {
        private static System.Random random = new System.Random();
        private PlushGrabbableObject plushObj { get; set; } = null!;
        private static bool isHost => RoundManager.Instance.NetworkManager.IsHost;
        private static List<GrabbableObject> Hives = new();
        private float beeMagnetTimer = 10f;
        private static float beeMagnetTimerMax = 10f, beeMagnetStopRadius = 1f, beeMagnetShipStopRadius=3f, beeMagnetSpeed = 0.5f;
        //todo: check ship bounds instead of radius
        private static Vector3 shipCenter;
        private void Awake()
        {
            beeMagnetTimer+=random.Next(0,10);
            plushObj = GetComponent<PlushGrabbableObject>();
            shipCenter = RoundManager.Instance.GetNavMeshPosition(StartOfRound.Instance.shipBounds.bounds.center, RoundManager.Instance.navHit, 1.75f);
            if(isHost) PlushNetworker.Instance.UpdateBeeHivesClientRPC();
        }
        private void OnEnable(){
            if(isHost) plushObj.onDiscardEvent.AddListener(AttractBees);
        }
        private void Update()
        {
            if(!isHost|| StartOfRound.Instance.inShipPhase || !StartOfRound.Instance.shipHasLanded||plushObj.isInFactory) return;
            beeMagnetTimer -= Time.deltaTime;
            if(plushObj.isHeld) beeMagnetTimer -=Time.deltaTime*7f;//update faster if plush is held for smoother pathing
            if (beeMagnetTimer < 0)
            {
                beeMagnetTimer = beeMagnetTimerMax;
                if (plushObj)
                {
                    AttractBees();
                }
            }
        }
        public static void UpdateBeeHives()
        {
            MischievousPlushies.LogInfo("Searching for bees...");
            RedLocustBees[] bees = GameObject.FindObjectsByType<RedLocustBees>(FindObjectsSortMode.None);
            foreach (GrabbableObject hive in Hives.ToList())
            {
                if (hive == null)
                {
                    Hives.Remove(hive);
                }
            }
            if (bees.Length > 0)
            {
                foreach (RedLocustBees bee in bees)
                {
                    if(bee.hive==null) continue;
                    if (Hives.Contains(bee.hive)) continue;
                    GrabbableNavMeshAgent agent = bee.hive.gameObject.AddComponent<GrabbableNavMeshAgent>();
                    agent.Init(beeMagnetSpeed);
                    Hives.Add(bee.hive);
                }
            }
        }
        public void AttractBees()
        {
            foreach (GrabbableObject hive in Hives)
            {
                if (hive != null)
                {
                    GrabbableNavMeshAgent agent = hive.GetComponent<GrabbableNavMeshAgent>();
                    Vector3 target;
                    if(plushObj.isInShipRoom){
                        Vector3 pointNearShip = Vector3.MoveTowards(StartOfRound.Instance.shipBounds.ClosestPointOnBounds(hive.transform.position), hive.transform.position,beeMagnetShipStopRadius);

                        target = GetClosestNavMesh(pointNearShip);
                        
                        PlushNetworker.Instance.SetTargetClientRPC(hive.NetworkObjectId,target);
                    }
                    else{
                        target = GetClosestNavMesh(Vector3.MoveTowards(plushObj.transform.position, hive.transform.position, beeMagnetStopRadius));
                        PlushNetworker.Instance.SetTargetClientRPC(hive.NetworkObjectId,target);
                    }
                }
            }
        }
        private static Vector3 GetClosestNavMesh(Vector3 point){
            return RoundManager.Instance.GetNavMeshPosition(point, RoundManager.Instance.navHit, 2f, NavMesh.AllAreas);
        }
        public void StopAttractingBees()
        {
            foreach (GrabbableObject hive in Hives)
            {
                if (hive != null)
                {

                    GrabbableNavMeshAgent agent = hive.GetComponent<GrabbableNavMeshAgent>();
                    //agent.StopPathing();
                }
            }
        }
        private void OnDisable()
        {
            if (isHost)
            {
                plushObj.onDiscardEvent.RemoveListener(AttractBees);
            }
        }
    }
}