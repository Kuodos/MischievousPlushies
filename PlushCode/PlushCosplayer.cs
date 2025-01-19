using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

namespace MischievousPlushies.PlushCode
{
    public class PlushCosplayer : MonoBehaviour
    {
        private GrabbableObject plushObj { get; set; } = null!;
        private List<GrabbableObject> plushiesList { get; set; } = null!;
        public static List<Item> plushieAllowList { get; set; } = null!;
        public static List<GrabbableObject> convertedPlushies { get; set; } = null!;
        private static bool isHost => RoundManager.Instance.NetworkManager.IsHost;
        private static float cosplayTimer = 120f, cosplayTimerMax = 60f;
        private static float cosplayRange = 5f;
        private static bool cosplayAnywhere = true;
        private void Awake()
        {
            plushObj = GetComponent<GrabbableObject>();
            plushiesList = new List<GrabbableObject>();
            convertedPlushies ??= new List<GrabbableObject>();
            plushieAllowList ??= new List<Item>();
            if (isHost)
            {
                convertedPlushies.Add(plushObj);
                UpdatePlushList();
            }
        }
        void UpdatePlushList()
        {
            GrabbableObject[] plushies = FindObjectsByType<GrabbableObject>(FindObjectsSortMode.None);
            foreach (GrabbableObject obj in plushies)
            {
                if (obj.itemProperties.itemName.ToLower().Contains("plush"))
                {
                    if (!convertedPlushies.Contains(obj)&&!plushiesList.Contains(obj))
                    {
                        if(plushieAllowList.Contains(obj.itemProperties)){
                            plushiesList.Add(obj);
                        }
                    }
                }
            }
           /* foreach (GrabbableObject obj in plushiesList){
                MischievousPlushies.Logger.LogInfo("Plushie found: " + obj.name);
            }*/
        }
        private void Update()
        {
            if (!isHost) return;
            if (plushObj.isInShipRoom || cosplayAnywhere)
            {
                cosplayTimer -= Time.deltaTime;
                if (cosplayTimer < 0)
                {
                    UpdatePlushList();
                    cosplayTimer = cosplayTimerMax;
                    GrabbableObject? plush = null;
                    float dist = cosplayRange;

                    if(plushiesList.Count==0) return;
                    foreach (GrabbableObject obj in plushiesList)
                    {
                        //MischievousPlushies.Logger.LogInfo("Evaluating: "+obj.name);
                        if (obj.isInShipRoom || cosplayAnywhere)
                        {
                            //MischievousPlushies.Logger.LogInfo((obj.transform.position - plushObj.transform.position).magnitude);
                            if ((obj.transform.position - plushObj.transform.position).magnitude < dist)
                            {
                                dist = (obj.transform.position - plushObj.transform.position).magnitude;
                                plush = obj;
                            }
                        }
                    }
                    if (plush != null && (dist < cosplayRange))
                    {
                        if(PlushNetworker.Instance==null) return;
                        convertedPlushies.Add(plush);
                        plushiesList.Remove(plush);
                        ulong targetID = plush.gameObject.GetComponent<NetworkObject>().NetworkObjectId;
                        MischievousPlushies.Logger.LogInfo(targetID);
                        MischievousPlushies.Logger.LogInfo(NetworkManager.Singleton.SpawnManager.SpawnedObjects[targetID]);
                        PlushNetworker.Instance.ConvertClientRPC(this.gameObject.GetComponent<NetworkObject>().NetworkObjectId,targetID);
                      //  MischievousPlushies.Logger.LogInfo("Sent RPC!");
                    }
                    else {
                        //MischievousPlushies.Logger.LogInfo("No convertees nearby :(");
                    }
                }
            }
        }
        
        public void PlushConvert(GrabbableObject obj)
        {
            MeshRenderer renderer;
            MeshFilter filter;
            if(obj.mainObjectRenderer != null){
                renderer=obj.mainObjectRenderer;
                filter=obj.mainObjectRenderer.transform.GetComponent<MeshFilter>();
            }
            else{
                renderer = obj.GetComponent<MeshRenderer>();
                filter = obj.GetComponent<MeshFilter>();
            }
            float size = filter.mesh.bounds.size.y;
            float sizeOrig = plushObj.GetComponent<MeshFilter>().mesh.bounds.size.y;

            //MischievousPlushies.Logger.LogInfo("size1: " +size + "size2: "+sizeOrig);
            //MischievousPlushies.Logger.LogInfo("loss1: " +filter.transform.lossyScale + "size2: "+plushObj.transform.lossyScale);
            renderer.materials=plushObj.GetComponent<MeshRenderer>().materials;
            filter.mesh=plushObj.GetComponent<MeshFilter>().mesh;
            filter.transform.localScale = transform.localScale*sizeOrig/size;
            //obj.originalScale=plushObj.originalScale;
            //obj.transform.localScale=obj.originalScale;

            //filter.transform.rotation=quaternion.identity;
            //MischievousPlushies.Logger.LogInfo(obj.mainObjectRenderer.GetComponentInChildren<MeshFilter>());

            obj.GetComponentInChildren<ScanNodeProperties>().headerText = plushObj.GetComponentInChildren<ScanNodeProperties>().headerText;
           // obj.itemProperties = plushObj.itemProperties;
        }
    }
}