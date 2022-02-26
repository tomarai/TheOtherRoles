using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TheOtherRoles.Objects;
using TheOtherRoles.Patches;
using static TheOtherRoles.TheOtherRoles;
using static TheOtherRoles.GameHistory;

namespace TheOtherRoles
{
    [HarmonyPatch]
    public class Munou2nd: RoleBase<Munou2nd>
    {
        public static Color color = Color.grey;
        public static bool endGameFlag = false;
        public static bool randomColorFlag = false;
        public static int probability {get {return (int)CustomOptionHolder.munou2ndProbability.getFloat();}}
        public static int numShufflePlayers {get {return (int)CustomOptionHolder.munou2ndNumShufflePlayers.getFloat();}}
        public static Dictionary<byte, byte> randomPlayers = new Dictionary<byte, byte>();


        public Munou2nd()
        {
            RoleType = roleId = RoleType.Munou2nd;
        }

        public override void OnMeetingStart()
        {
            resetColors();
        }
        public override void OnMeetingEnd()
        {
            if(PlayerControl.LocalPlayer.isRole(RoleType.Munou2nd) && PlayerControl.LocalPlayer.isAlive())
            {
                randomColors();
            }
        }

        public override void FixedUpdate()
        {
            // if(PlayerControl.LocalPlayer.isRole(RoleType.Munou2nd) && PlayerControl.LocalPlayer.isAlive())
            // {
            //     if(!randomColorFlag)
            //     {
            //         randomColors();
            //     }
            // }
        }
        public override void OnKill(PlayerControl target) { }
        public override void OnDeath(PlayerControl killer = null)
        {
            if(PlayerControl.LocalPlayer.isRole(RoleType.Munou2nd)) resetColors();
        }
        public override void HandleDisconnect(PlayerControl player, DisconnectReasons reason) { }

        public static void MakeButtons(HudManager hm) { }
        public static void SetButtonCooldowns() { }

        public static void Clear()
        {
            players = new List<Munou2nd>();
            randomPlayers = new Dictionary<byte, byte>();
            endGameFlag = false;
            resetColors();
        }

        public static void randomColors(){
            // 発生確率
            int random = rnd.Next(100);
            if(random > probability) return;

            var allPlayers = PlayerControl.AllPlayerControls;
            List<byte> alivePlayers = new List<byte>();
            List<int> tempList = new List<int>();
            foreach(var p in allPlayers)
            {
                if(p.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
                if(p == Puppeteer.dummy) continue;
                if(p.isAlive()) alivePlayers.Add(p.PlayerId);
            }
            alivePlayers.shuffle();
            List<byte> shuffleTargets = alivePlayers.Count > numShufflePlayers ? alivePlayers.Take(numShufflePlayers).ToList() : alivePlayers;
            foreach(byte id in shuffleTargets)
            {
                if(id == PlayerControl.LocalPlayer.PlayerId) continue;
                var p = Helpers.playerById(id);
                int rnd;
                int coutner = 0;
                while(true){
                    rnd = TheOtherRoles.rnd.Next(shuffleTargets.Count);
                    if(shuffleTargets[rnd] == PlayerControl.LocalPlayer.PlayerId) continue;
                    if(!tempList.Contains(rnd))
                    {
                        tempList.Add(rnd);
                        break;
                    }
                    coutner++;
                }
                var to =Helpers.playerById((byte)shuffleTargets[rnd]);
                MorphHandler.morphToPlayer(p, to);
            }
            randomColorFlag = true;
        }
        public static void resetColors(){
            var allPlayers = PlayerControl.AllPlayerControls;
            foreach(var p in allPlayers)
            {
                MorphHandler.morphToPlayer(p, p);
            }
            randomColorFlag = false;
        }

        [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
        public class OnGameEndPatch
        {

            public static void Prefix(AmongUsClient __instance, [HarmonyArgument(0)] ref EndGameResult endGameResult)
            {
                    Munou2nd.endGameFlag = true;
            }
        }
    }
}