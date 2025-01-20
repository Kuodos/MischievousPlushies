using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        public UnityEvent onGrabEvent;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            recordings ??= Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            onGrabEvent ??= new UnityEvent();
            if(GetComponent<PlushLifeboundExploder>()) GetComponent<PlushLifeboundExploder>().Init();
        }
        public override void GrabItem()
        {
            base.GrabItem();
            onGrabEvent.Invoke();
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            
        }
        public override void OnDestroy()
        {
            onGrabEvent.RemoveAllListeners();
            base.OnDestroy();
        }
    }
}