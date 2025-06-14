using System.IO;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Events;

namespace MischievousPlushies.PlushCode
{
    public class PlushGrabbableObject : GrabbableObject
    {
        private static string recordings;
        public AudioClip defaultNoise;
        public UnityEvent onGrabEvent = new UnityEvent(), onDiscardEvent = new UnityEvent(), onItemActivateEvent = new UnityEvent();

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            //todo play random voice recording on use if Mirage is installed
            recordings ??= Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName)+"\\Recordings\\"; 
            onGrabEvent ??= new UnityEvent();
            onItemActivateEvent ??= new UnityEvent();
            if(GetComponent<PlushLifeboundExploder>()) GetComponent<PlushLifeboundExploder>().Init();
            
        }
        public override void GrabItem()
        {
            base.GrabItem();
            onGrabEvent?.Invoke();
        }
        public override void DiscardItem()
        {
            base.DiscardItem();
            onDiscardEvent?.Invoke();
        }
        

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            onItemActivateEvent?.Invoke();
        }
        public override void OnDestroy()
        {
            onGrabEvent?.RemoveAllListeners();
            base.OnDestroy();
        }
    }
}