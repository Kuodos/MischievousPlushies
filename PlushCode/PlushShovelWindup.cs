using UnityEngine;
using System.Collections;
using Unity.Netcode;
using GameNetcodeStuff;

namespace MischievousPlushies.PlushCode
{
    public class PlushShovelWindup : MonoBehaviour
    {
        private PlushGrabbableObject plushObj { get; set; } = null!;
        private Animator anim { get; set; } = null!;
        [SerializeField] private GameObject shovelTrigger;
        [SerializeField] private AudioClip windupClip, hitClip, windupDoneClip;
        private AudioSource audioSource { get; set; } = null!;
        private static bool isHost => RoundManager.Instance.NetworkManager.IsHost;
        private static int shovelHitForce = 2;
        private static bool hitSelf = false;
        private bool canWindup = true;
        private void Start()
        {
            plushObj = GetComponent<PlushGrabbableObject>();
            anim = GetComponentInChildren<Animator>();
            shovelTrigger.SetActive(false);
            
            audioSource = GetComponent<AudioSource>();
            plushObj.onItemActivateEvent.AddListener(WindupShovelSendRPC);
            plushObj.onGrabEvent.AddListener(OnGrab);
            if (isHost)
            {

            }
        }
        private void OnGrab()
        {
            string[] allLines = { "Wind up: [LMB]" };
            HUDManager.Instance.ChangeControlTipMultiple(allLines, holdingItem: true, plushObj.itemProperties);
            shovelTrigger.GetComponent<Collider>().enabled=true; //re-enable bc grabbing an item disables colliders
        }
        private void WindupShovelSendRPC()
        {
            if (!canWindup) return;

            PlushNetworker.Instance.WindupShovelClientRPC(plushObj.NetworkObjectId);
        }
        public void WindupShovel()
        {
            StartCoroutine(WindupSequence());
        }
        IEnumerator WindupSequence()
        {
           // winder=plushObj.playerHeldBy;
            audioSource.PlayOneShot(windupClip);
            canWindup = false;
            anim.SetBool("winding", true);
            yield return null;
            MischievousPlushies.LogInfo(anim.GetCurrentAnimatorStateInfo(0).m_Length.ToString() + "time to shovel");
            yield return new WaitForSeconds(anim.GetCurrentAnimatorStateInfo(0).m_Length);
            audioSource.PlayOneShot(windupDoneClip);
            anim.SetBool("winding", false);
            //MischievousPlushies.LogInfo("ready to shovel!");
            if(isHost) shovelTrigger.SetActive(true);
        }
        private void OnTriggerEnter(Collider other)
        {
            if (!isHost) return;
            if (plushObj.isPocketed) return;
            if (!shovelTrigger.activeSelf) return; //prevent late hits
            //MischievousPlushies.LogInfo("Triggered by " + other.gameObject.name);
            if (other.transform.TryGetComponent<IHittable>(out var component))
            {
                MischievousPlushies.LogInfo("IHittable: " + component.ToString());
                if(plushObj.isHeld){
                    if(plushObj.playerHeldBy==other.GetComponent<PlayerControllerB>()&&!hitSelf){
                       // MischievousPlushies.LogInfo("No hitting myself today!");
                        return;
                    }
                }
                ulong hittableID;
                
                if (other.GetComponentInChildren<NetworkObject>() != null)
                {
                    hittableID = other.GetComponentInChildren<NetworkObject>().NetworkObjectId;
                }
                else if (other.GetComponent<EnemyAICollisionDetect>()!=null)
                {
                    hittableID = other.GetComponent<EnemyAICollisionDetect>().mainScript.GetComponentInChildren<NetworkObject>().NetworkObjectId;
                }
                else {
                    MischievousPlushies.LogInfo("no NetworkObject on IHittable!");
                    return;
                }
                shovelTrigger.SetActive(false);
                PlushNetworker.Instance.WindupShovelHitClientRPC(plushObj.NetworkObjectId, hittableID);
            }
        }
        public void StartAttackSequence(IHittable hittable){
            StartCoroutine(AttackSequence(hittable));
        }
        private IEnumerator AttackSequence(IHittable hittable)
        {
            anim.SetTrigger("attack");
            yield return null;
            float attackLength = anim.GetCurrentAnimatorStateInfo(0).m_Length;
            yield return new WaitForSeconds(0.2f);
            attackLength -= 0.2f;
            audioSource.PlayOneShot(hitClip);
            if(isHost) {  
                MischievousPlushies.LogInfo("hit! for "+ shovelHitForce);
                hittable.Hit(shovelHitForce, -plushObj.transform.forward, StartOfRound.Instance.localPlayerController, playHitSFX: true, 1);
                //todo fix? specifying non-host player results in double hits
            }
            GameObject.FindAnyObjectByType<RoundManager>().PlayAudibleNoise(base.transform.position, 17f, 0.8f);
            yield return new WaitForSeconds(attackLength);
            canWindup = true;
        }
    }
}