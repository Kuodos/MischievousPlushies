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
        private Transform? targetTransform;
        private bool stopPathing = false;
        public void Init(float speed, float baseOffset = 0.2f, bool updateRotation = false, float stoppingDistance = 0)
        {
            obj = GetComponent<GrabbableObject>();
            if (obj.GetComponent<NavMeshAgent>() != null)
            {
                agent = obj.GetComponent<NavMeshAgent>();
            }
            else
            {
                agent = obj.gameObject.AddComponent<NavMeshAgent>();
            }
            agent.areaMask = NavMesh.AllAreas;
            agent.speed = speed;
            agent.updatePosition = false;
            agent.updateRotation = updateRotation;
            agent.baseOffset = baseOffset;
            agent.height = 2f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;
            agent.stoppingDistance = stoppingDistance;
            agent.autoBraking=false;
            agent.angularSpeed=360;
            agent.acceleration=100; //todo fix jitter
            agent.autoTraverseOffMeshLink=true;
        }

        public void SetTargetPosition(Vector3 target)
        {
            if (!agent.enabled) return;
           // agent.ResetPath();
            agent.Warp(RoundManager.Instance.GetNavMeshPosition(obj.transform.position, RoundManager.Instance.navHit, 2f, NavMesh.AllAreas));
            bool gotPath = agent.SetDestination(target);
            if (gotPath)
            {
                stopPathing = false;
              //  MischievousPlushies.LogInfo(obj.name + " - path found: " + obj.transform.position + " --> " + agent.destination);
            }
            //else MischievousPlushies.LogInfo(obj.name + " - No path found!");
        }
        public void SetTarget(Transform? targetTransform)
        {
            this.targetTransform = targetTransform;
            if (targetTransform == null) StopPathing();
            else GetNewPosition();
        }
        public void Teleport(Vector3 target)
        {
            MischievousPlushies.LogInfo("Teleporting object to "+target);
            Vector3 newpos;
            transform.position=target;
            StopPathing();
            newpos=RoundManager.Instance.GetNavMeshPosition(target, RoundManager.Instance.navHit, 2f, NavMesh.AllAreas);
            agent.Warp(newpos);
            SetObjectPosition();
            obj.isInFactory = (agent.nextPosition.y<-80); //should work?
            obj.isInShipRoom = StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(newpos);
            obj.isInElevator = obj.isInShipRoom;
            if(obj.isInShipRoom){
                obj.transform.SetParent(StartOfRound.Instance.elevatorTransform);
            }
           // MischievousPlushies.LogInfo("object in factory:"+obj.isInFactory + " " + agent.nextPosition + " " + newpos);
        }
        private void GetNewPosition()
        {
            if (targetTransform == null) return;
            SetTargetPosition(targetTransform.transform.position);
        }
        public void StopPathing()
        {
            stopPathing = true;
            agent.ResetPath();
        }
        private void LateUpdate()
        {
            if (stopPathing)
            {
                return;
            }

            if (obj.isHeld) StopPathing();
            SetObjectPosition();
        }
        private void SetObjectPosition(){
            
            if (obj.transform.parent != null)
            {
                obj.targetFloorPosition = obj.transform.parent.InverseTransformPoint(agent.nextPosition); //queue Duster - Me and the birds
            }
            else
            {
                obj.targetFloorPosition = agent.nextPosition;
            }
        }
    }
}