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
        private static float checkAFK_timer = 30f, checkAFK_timerMax = 30f;
        private static PlayerControllerB[] Players { get; set; } = null!;
        private static Dictionary<PlayerControllerB, Vector3> playerRots { get; set; } = null!;
        private static List<PlayerControllerB> afkPlayers { get; set; } = null!;

        private void Awake()
        {
            plushObj = GetComponent<GrabbableObject>();
            if (isHost)
            {
                shipLights = GameObject.FindFirstObjectByType<ShipLights>();
                lightSwitch = GameObject.Find("LightSwitchContainer").GetComponentInChildren<InteractTrigger>();
                afkPlayers = new List<PlayerControllerB>();
                playerRots = new Dictionary<PlayerControllerB, Vector3>();
                Players = StartOfRound.Instance.allPlayerScripts;
                foreach (var player in Players)
                {
                    playerRots.Add(player, player.transform.rotation.eulerAngles);
                }
            }
        }
        private void Update()
        {
            if (isHost)
            {
                checkAFK_timer -= Time.deltaTime;
                if (checkAFK_timer < 0)
                {
                    checkAFK_timer = checkAFK_timerMax;
                    ScanAFK();
                }
            }
        }
        private void ScanAFK()
        {
            isActivePlayerOnShip = false;
            if (StartOfRound.Instance.inShipPhase) return;
            if (plushObj.isInShipRoom)
            {
                MischievousPlushies.Logger.LogInfo("Looking for AFK players...");
                foreach (var player in Players)
                {
                    if (isPlayerAFK(player))
                    {
                        if (afkPlayers.Contains(player))
                        {
                            if (!isActivePlayerOnShip)
                            {
                                MischievousPlushies.Logger.LogInfo("xˬx target locked xˬx");
                                StartCoroutine(TeleportPlayerSequence(player));
                            }
                            afkPlayers.Clear();
                        }
                        else afkPlayers.Add(player);
                    }
                    else if (afkPlayers.Contains(player)) afkPlayers.Remove(player);
                    playerRots[player] = player.transform.rotation.eulerAngles;
                }
            }
        }
        bool isPlayerAFK(PlayerControllerB player)
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
                float deltaRot = (playerRots[player] - player.transform.rotation.eulerAngles).magnitude;
                if (player.timeSincePlayerMoving > checkAFK_timerMax)
                {
                    if (deltaRot < 0.5f)
                    {
                        MischievousPlushies.Logger.LogInfo(player.name + " is AFK! ");
                        return true;
                    }
                    else
                    {
                        //SquishCompany.Logger.LogInfo(player.name + " not AFK - moved mouse:" + deltaRot);
                    }
                }
                else
                {
                    //SquishCompany.Logger.LogInfo(player.name + " not AFK - moved");
                }
                isActivePlayerOnShip = true;
            }
            //else SquishCompany.Logger.LogInfo(player.name + " not inside ship");
            return false;
        }

        IEnumerator TeleportPlayerSequence(PlayerControllerB player)
        {
            ShipTeleporter? teleporter = null;
            foreach (ShipTeleporter tp in GameObject.FindObjectsOfType<ShipTeleporter>())
            {
                if (tp.isInverseTeleporter && tp.CanUseInverseTeleporter())
                {
                    teleporter = tp;
                }
            }
            if (teleporter != null)
            {

                if (shipLights.areLightsOn)
                {
                    lightSwitch.Interact(player.transform);
                }
                yield return new WaitForSeconds(15f);
                if (!isPlayerAFK(player)) yield break;
                MischievousPlushies.Logger.LogInfo("xˬx teleporting xˬx");
                plushObj.targetFloorPosition=teleporter.transform.Find("RedButton").position-transform.parent.position+Vector3.up*0.3f;
                player.TeleportPlayer(teleporter.teleporterPosition.position);
                teleporter.PressTeleportButtonServerRpc();
            }
        }
    }
}