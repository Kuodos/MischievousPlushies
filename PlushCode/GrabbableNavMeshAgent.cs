using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace MischievousPlushies.PlushCode
{
    public class GrabbableNavMeshAgent : MonoBehaviour
    {
        private GrabbableObject obj;
        private NavMeshAgent agent;
        private bool stopPathing=false;
        public void Init(float speed){
            obj = GetComponent<GrabbableObject>();
            if(obj.GetComponent<NavMeshAgent>() != null){
                agent = obj.GetComponent<NavMeshAgent>();
            }
            else {
                agent = obj.gameObject.AddComponent<NavMeshAgent>();
            }
            agent.areaMask=NavMesh.AllAreas;
            agent.speed=speed;
            agent.updatePosition=false;
            agent.updateRotation=false;
            agent.baseOffset=0.4f;
            agent.height=1.5f;
            agent.obstacleAvoidanceType=ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            agent.stoppingDistance=0f;
        }

        public void SetTarget(Vector3 target){
            if(!agent.enabled) return;
            agent.ResetPath();
            agent.nextPosition=RoundManager.Instance.GetNavMeshPosition(obj.transform.position, RoundManager.Instance.navHit, 2f, NavMesh.AllAreas);
            bool gotPath = agent.SetDestination(target);
            if(gotPath) {
               // MischievousPlushies.LogInfo(obj.name+ " got destination: "+target);
                stopPathing=false;
            }
            else MischievousPlushies.LogInfo(obj.name+ " - No path found!");
        }
        public void StopPathing(){
            stopPathing = true;
            agent.ResetPath();
        }
        private void LateUpdate(){
            if(stopPathing){
                return;
            }

            if(obj.isHeld) StopPathing();
            if(obj.transform.parent!=null) {
                obj.targetFloorPosition=obj.transform.parent.InverseTransformPoint(agent.nextPosition); //queue Duster - Me and the birds
            }
            else {
                obj.targetFloorPosition = agent.nextPosition;
            }
        }
    }
}