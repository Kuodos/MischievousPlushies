using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BepInEx.Logging;
using GameNetcodeStuff;
using UnityEngine;

namespace MischievousPlushies.PlushCode
{    //extremely mischevious
    public class PlushAFKTeleporter : MonoBehaviour
    {
        private static bool isActivePlayerOnShip = false;
        private GrabbableObject plushObj { get; set; } = null!;
        private static bool isHost => RoundManager.Instance.NetworkManager.IsHost;
        private static ShipLights shipLights { get; set; } = null!;
        private static InteractTrigger lightSwitch { get; set; } = null!;
        private float checkAFK_Cur=60f;
        private static float checkAFK_Timer = 30f;
        private static PlayerControllerB[] Players { get; set; } = null!;
        private static Dictionary<PlayerControllerB, Vector3> PlayerRots { get; set; } = null!;
        private static List<PlayerControllerB> AfkPlayers { get; set; } = null!;
        private static bool teleporting = false, setup = false;

        private void Awake()
        {
            plushObj = GetComponent<GrabbableObject>();
            if (isHost && !setup)
            {
                Init();
            }
        }
        private void Init()
        {
            setup = true;
            shipLights = GameObject.FindFirstObjectByType<ShipLights>();
            lightSwitch = GameObject.Find("LightSwitchContainer").GetComponentInChildren<InteractTrigger>();
            AfkPlayers = new List<PlayerControllerB>();
            PlayerRots = new Dictionary<PlayerControllerB, Vector3>();
            Players = StartOfRound.Instance.allPlayerScripts;
            foreach (var player in Players)
            {
                PlayerRots.Add(player, player.transform.rotation.eulerAngles);
            }
        }
        private void Update(){
            if(isHost&&plushObj.isInShipRoom){
                checkAFK_Cur-=Time.deltaTime;
                if(checkAFK_Cur<0){
                    checkAFK_Cur=checkAFK_Timer;
                    ScanAFK();
                }
            }
        }
        private static void ScanAFK()
        {
            isActivePlayerOnShip = false;
            GrabbableObject? firstPlush = null;
            if (StartOfRound.Instance.inShipPhase || StartOfRound.Instance.shipIsLeaving || teleporting) return;
            foreach (PlushAFKTeleporter telObj in GameObject.FindObjectsByType<PlushAFKTeleporter>(FindObjectsSortMode.None))
            {
                if (telObj.plushObj.isInShipRoom && !telObj.plushObj.isHeld)
                {
                    firstPlush = telObj.plushObj;
                    break;
                }
            }
            if (firstPlush != null)
            {
                //MischievousPlushies.Logger.LogInfo("Looking for AFK players...");
                foreach (var player in Players)
                {
                    if (isPlayerAFK(player))
                    {
                        if (AfkPlayers.Contains(player))
                        {
                            if (!isActivePlayerOnShip)
                            {
                                MischievousPlushies.Logger.LogInfo("xˬx target locked xˬx");
                                firstPlush.GetComponent<PlushAFKTeleporter>().StartTeleportSequence(player);
                            }
                            AfkPlayers.Clear();
                        }
                        else AfkPlayers.Add(player);
                    }
                    else if (AfkPlayers.Contains(player)) AfkPlayers.Remove(player);
                    PlayerRots[player] = player.transform.rotation.eulerAngles;
                }
            }
        }
        static bool isPlayerAFK(PlayerControllerB player)
        {
            if (player.isPlayerDead)
            {
                //SquishCompany.Logger.LogInfo(player.name + " dead");
                return false;
            }
            if (player.inTerminalMenu)
            {
                //SquishCompany.Logger.LogInfo(player.name + " gaming");
                return false;
            }
            if (player.isInHangarShipRoom)
            {
                float deltaRot = (PlayerRots[player] - player.transform.rotation.eulerAngles).magnitude;
                if (player.timeSincePlayerMoving > checkAFK_Timer)
                {
                    if (deltaRot < 0.5f)
                    {
                        //MischievousPlushies.Logger.LogInfo(player.name + " is AFK! ");
                        return true;
                    }
                }
                isActivePlayerOnShip = true;
            }
            return false;
        }
        public void StartTeleportSequence(PlayerControllerB player)
        {
            teleporting = true;
            StartCoroutine(TeleportPlayerSequence(player));
        }
        IEnumerator TeleportPlayerSequence(PlayerControllerB player)
        {
            ShipTeleporter? teleporter = null;
            foreach (ShipTeleporter tp in GameObject.FindObjectsByType<ShipTeleporter>(FindObjectsSortMode.None))
            {
                if (tp.isInverseTeleporter && tp.CanUseInverseTeleporter())
                {
                    teleporter = tp;
                }
            }
            if (teleporter != null)
            {
                if (shipLights.areLightsOn) lightSwitch.Interact(player.transform);
                yield return new WaitForSeconds(15f);
                if (!isPlayerAFK(player))
                {
                    teleporting = false;
                    yield break;
                }
                MischievousPlushies.Logger.LogInfo("xˬx teleporting xˬx");
                //ButtonContainer->ButtonAnimContainer->RedButton
                Vector3 buttonPos = teleporter.transform.GetChild(1).GetChild(0).GetChild(0).position - transform.parent.position + Vector3.up * 0.05f;
                PlushNetworker.Instance.TeleportItemClientRPC(plushObj.NetworkObjectId, buttonPos);
                PlushNetworker.Instance.TeleportPlayerClientRPC(player.NetworkObjectId, teleporter.teleporterPosition.position);
                teleporter.PressTeleportButtonServerRpc();
                teleporting = false;
            }
            else teleporting = false;
        }
        
    }
}