using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace MischievousPlushies.PlushCode
{
    public class PlushCosplayer : MonoBehaviour
    {
        private GrabbableObject PlushObj { get; set; } = null!;
        private static List<GrabbableObject> PlushiesList { get; set; } = null!;
        public static List<Item> PlushieAllowList { get; set; } = null!;
        public static List<GrabbableObject> ConvertedPlushies { get; set; } = null!;
        private static bool isHost => RoundManager.Instance.NetworkManager.IsHost;
        private static float cosplayTimer = 120f, cosplayTimerMax = 60f;
        private static float cosplayRange = 5f;
        private static bool cosplayAnywhere = true, ignoreAllowList = true;
        private void Start()
        {
            PlushObj = GetComponent<GrabbableObject>();
            PlushiesList = new List<GrabbableObject>();
            ConvertedPlushies ??= new List<GrabbableObject>();
            PlushieAllowList ??= new List<Item>();
            if (isHost)
            {
                ConvertedPlushies.Add(PlushObj);
                UpdatePlushList();
            }
            else
            {
                PlushNetworker.Instance.RequestCosplayerListServerRPC(NetworkManager.Singleton.LocalClientId);
            }
            StartOfRound.Instance.StartNewRoundEvent.AddListener(MassConvert);
        }
        void OnDestroy()
        {
            StartOfRound.Instance.StartNewRoundEvent.RemoveListener(MassConvert);
        }
        public static void SetConvertedPlushies(ulong[] Cosplayers)
        {
            ConvertedPlushies.Clear();
            foreach (ulong id in Cosplayers)
            {
                ConvertedPlushies.Add(NetworkManager.Singleton.SpawnManager.SpawnedObjects[id].transform.GetComponent<GrabbableObject>());
            }
            FindAnyObjectByType<PlushCosplayer>().MassConvert();
        }
        public void MassConvert()
        {
            PruneConvertedList();
            foreach (GrabbableObject obj in ConvertedPlushies)
            {
                PlushConvert(obj);
            }
        }
        public static void UpdatePlushList()
        {
            GrabbableObject[] plushies = FindObjectsByType<GrabbableObject>(FindObjectsSortMode.None);
            PlushiesList.Clear();
            foreach (GrabbableObject obj in plushies)
            {
                if (obj.itemProperties.itemName.ToLower().Contains("plush") || obj.itemProperties.itemName.ToLower().Contains("fumo"))
                {
                    if (!ConvertedPlushies.Contains(obj) && !PlushiesList.Contains(obj))
                    {
                        if (PlushieAllowList.Contains(obj.itemProperties) || ignoreAllowList)
                        {
                            PlushiesList.Add(obj);
                        }
                    }
                }
            }
            PruneConvertedList();
            foreach (GrabbableObject obj in PlushiesList)
            {
                MischievousPlushies.LogInfo("Cosplayer found a plushie: " + obj.name);
            }
        }
        public static void PruneConvertedList()
        {
            ConvertedPlushies.RemoveAll(plush => plush == null);
        }
        private void Update()
        {
            if (!isHost) return;
            if (PlushObj.isInShipRoom || cosplayAnywhere)
            {
                cosplayTimer -= Time.deltaTime;
                if (cosplayTimer < 0)
                {
                    UpdatePlushList();
                    cosplayTimer = cosplayTimerMax;
                    GrabbableObject? plush = null;
                    float dist = cosplayRange;
                    if (PlushiesList.Count == 0) return;
                    foreach (GrabbableObject obj in PlushiesList)
                    {
                        if (obj.isInShipRoom || cosplayAnywhere)
                        {
                            float newDist = (obj.transform.position - PlushObj.transform.position).magnitude;
                            if (newDist < dist)
                            {
                                dist = newDist;
                                plush = obj;
                            }
                        }
                    }
                    if (plush != null)
                    {
                        if (PlushNetworker.Instance == null) return;
                        ConvertedPlushies.Add(plush);
                        PlushiesList.Remove(plush);
                        ulong targetID = plush.NetworkObjectId;
                        MischievousPlushies.LogInfo(targetID + "converted");
                        PlushNetworker.Instance.CosplayClientRPC(PlushObj.NetworkObjectId, targetID);
                    }
                }
            }
        }

        public void PlushConvert(GrabbableObject obj)
        {
            if (obj.GetComponentInChildren<SkinnedMeshRenderer>() != null && obj.itemProperties.itemName.ToLower().Contains("kuodos"))
            {
                obj.GetComponentInChildren<SkinnedMeshRenderer>().materials = PlushObj.GetComponent<MeshRenderer>().materials;
            }
            else
            {
                MeshFilter filter;
                if (obj.mainObjectRenderer != null)
                {
                    filter = obj.mainObjectRenderer.transform.GetComponent<MeshFilter>();
                }
                else
                {
                    filter = obj.GetComponent<MeshFilter>();
                }
                float sizeObj = filter.mesh.bounds.size.y * filter.transform.lossyScale.y;
                float sizePlush = PlushObj.GetComponent<MeshFilter>().mesh.bounds.size.y * PlushObj.GetComponent<MeshFilter>().transform.lossyScale.y;

                //disable original MeshRenderer, add a copy of PlushCosplayer MeshRenderer
                foreach (MeshRenderer rend in obj.transform.GetComponentsInChildren<MeshRenderer>())
                {
                    rend.forceRenderingOff = true;
                }

                GameObject fakePlushObj = new GameObject("fake plush");
                fakePlushObj.AddComponent<MeshRenderer>().materials = PlushObj.GetComponent<MeshRenderer>().materials;
                fakePlushObj.AddComponent<MeshFilter>().mesh = PlushObj.GetComponent<MeshFilter>().mesh;
                fakePlushObj.transform.rotation = obj.transform.rotation;
                fakePlushObj.transform.Rotate(-obj.itemProperties.restingRotation.x, 0, 0);
                fakePlushObj.transform.position = obj.transform.position;
                fakePlushObj.transform.localScale = Vector3.one * sizeObj / sizePlush * PlushObj.originalScale.y; //scaling object to match original height
                fakePlushObj.transform.parent = obj.transform;
            }
            obj.GetComponentInChildren<ScanNodeProperties>().headerText = PlushObj.GetComponentInChildren<ScanNodeProperties>().headerText;
            obj.customGrabTooltip = PlushObj.customGrabTooltip;
        }
    }
}