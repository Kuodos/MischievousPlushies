using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace MischievousPlushies.PlushCode
{
    public class PlushShovelStealer : MonoBehaviour
    {
        private GrabbableObject plushObj { get; set; } = null!;
        private static bool isHost => RoundManager.Instance.NetworkManager.IsHost;
        private void Start()
        {
            plushObj = GetComponent<GrabbableObject>();
            if (isHost)
            {

            }
        }
        public void ReplaceShovel(GrabbableObject shovel){
            
        }
    }
}