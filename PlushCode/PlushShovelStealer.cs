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
        private void Awake()
        {
            plushObj = GetComponent<GrabbableObject>();
            if (isHost)
            {

            }
        }
    }
}