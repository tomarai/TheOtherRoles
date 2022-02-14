using HarmonyLib;
using Hazel;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TheOtherRoles.Objects;
using TheOtherRoles.Patches;
using static TheOtherRoles.TheOtherRoles;
using static TheOtherRoles.GameHistory;
using static TheOtherRoles.Patches.PlayerControlFixedUpdatePatch;

namespace TheOtherRoles
{
    [HarmonyPatch]
    public class BomberB : RoleBase<BomberB>
    {
        public static Color color = Palette.ImpostorRed;

        public static CustomButton bomberButton;
        public static CustomButton releaseButton;

        public static PlayerControl bombTarget;
        public static PlayerControl currentTarget;
        public static PlayerControl tmpTarget;
        public static float duration {get {return CustomOptionHolder.bomberDuration.getFloat();}}
        public static float cooldown {get {return CustomOptionHolder.bomberCooldown.getFloat();}}
        public static bool ifOneDiesBothDie {get {return CustomOptionHolder.bomberIfOneDiesBothDie.getBool();}}
        public static Sprite bomberButtonSprite;
        public static Sprite releaseButtonSprite;
        public static float updateTimer = 0f;
        public static List<Arrow> arrows = new List<Arrow>();
        public static float arrowUpdateInterval = 0.5f;

        public BomberB()
        {
            RoleType = roleId = RoleId.BomberB;
        }

        public override void OnMeetingStart() { }
        public override void OnMeetingEnd()
        {
            bombTarget = null;
        }
        public override void FixedUpdate()
        {
            if (player == PlayerControl.LocalPlayer)
            {
                currentTarget = setTarget();
                setPlayerOutline(currentTarget, BomberB.color);
                arrowUpdate();

                foreach (PoolablePlayer pp in MapOptions.playerIcons.Values) pp.gameObject.SetActive(false);
                if(player.isAlive() && BomberA.isAlive() && bombTarget != null )
                {
                    if (MapOptions.playerIcons.ContainsKey(bombTarget.PlayerId) && MapOptions.playerIcons[bombTarget.PlayerId].gameObject != null)
                    {
                        Vector3 bottomLeft = new Vector3(-HudManager.Instance.UseButton.transform.localPosition.x, HudManager.Instance.UseButton.transform.localPosition.y, HudManager.Instance.UseButton.transform.localPosition.z);
                        var icon = MapOptions.playerIcons[bombTarget.PlayerId];
                        icon.gameObject.SetActive(true);
                        icon.transform.localPosition = bottomLeft + new Vector3(-0.25f, 0f, 0);
                        icon.transform.localScale = Vector3.one * 0.4f;
                    }
                }
            }
        }
        public override void OnKill(PlayerControl target) { }
        public override void OnDeath(PlayerControl killer = null) 
        {
            if(ifOneDiesBothDie)
            {
                var partner = BomberA.players.FirstOrDefault().player;
                if (!partner.Data.IsDead)
                {
                    if (killer != null)
                    {
                        partner.MurderPlayer(partner);
                    }
                    else
                    {
                        partner.Exiled();
                    }

                    finalStatuses[partner.PlayerId] = FinalStatus.Suicide;
                }
            }

         }
        public override void HandleDisconnect(PlayerControl player, DisconnectReasons reason) { }

        public static void MakeButtons(HudManager hm) 
        {

            // Bomber button
            bomberButton = new CustomButton(
                // OnClick
                () =>
                {
                    if (currentTarget != null)
                    {
                        tmpTarget = currentTarget;
                        bomberButton.HasEffect = true;
                    }
                },
                // HasButton
                () => { return PlayerControl.LocalPlayer.isRole(RoleId.BomberB)  && PlayerControl.LocalPlayer.isAlive() && BomberA.isAlive(); },
                // CouldUse
                () =>
                {
                    if (bomberButton.isEffectActive && tmpTarget != currentTarget)
                    {
                        tmpTarget = null;
                        bomberButton.Timer = 0f;
                        bomberButton.isEffectActive = false;
                    }

                    return PlayerControl.LocalPlayer.CanMove && currentTarget != null;
                },
                // OnMeetingEnds
                () =>
                {
                    bomberButton.Timer = bomberButton.MaxTimer;
                    bomberButton.isEffectActive = false;
                    tmpTarget = null;
                },
                getBomberButtonSprite(), 
                new Vector3(-1.8f, -0.06f, 0),
                hm,
                hm.KillButton,
                KeyCode.F,
                true,
                duration,
                // OnEffectsEnd
                () =>
                {
                    if (tmpTarget != null)
                    {
                        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.PlantBomb, Hazel.SendOption.Reliable, -1);
                        writer.Write(tmpTarget.PlayerId);
                        AmongUsClient.Instance.FinishRpcImmediately(writer);
                        BomberB.bombTarget = tmpTarget;
                    }

                    tmpTarget = null;
                    bomberButton.Timer = bomberButton.MaxTimer;

                }
            );
            bomberButton.buttonText = "爆弾設置";
            // Bomber button
            releaseButton = new CustomButton(
                // OnClick
                () =>
                {
                    var bomberA = BomberA.allPlayers.FirstOrDefault();
                    float distance = Vector2.Distance(PlayerControl.LocalPlayer.transform.localPosition, bomberA.transform.localPosition);

                    if (PlayerControl.LocalPlayer.CanMove && BomberA.bombTarget != null && BomberB.bombTarget != null && bomberA.isAlive() && distance < 1)
                    {
                        var target = BomberB.bombTarget;
                        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ReleaseBomb, Hazel.SendOption.Reliable, -1);
                        writer.Write(PlayerControl.LocalPlayer.PlayerId);
                        writer.Write(target.PlayerId);
                        AmongUsClient.Instance.FinishRpcImmediately(writer);
                        RPCProcedure.releaseBomb(PlayerControl.LocalPlayer.PlayerId, target.PlayerId);
                    }
                },
                // HasButton
                () => { return PlayerControl.LocalPlayer.isRole(RoleId.BomberB)  && PlayerControl.LocalPlayer.isAlive() && BomberA.isAlive(); },
                // CouldUse
                () =>
                {
                    var bomberA = BomberA.allPlayers.FirstOrDefault();
                    float distance = Vector2.Distance(PlayerControl.LocalPlayer.transform.localPosition, bomberA.transform.localPosition);

                    return PlayerControl.LocalPlayer.CanMove && BomberA.bombTarget != null && BomberB.bombTarget != null && bomberA.isAlive() && distance < 1;
                },
                // OnMeetingEnds
                () =>
                {
                    releaseButton.Timer = releaseButton.MaxTimer;
                },
                getReleaseButtonSprite(), 
                new Vector3(-2.7f, -0.06f, 0),
                hm,
                hm.KillButton,
                KeyCode.F,
                false
            );
            releaseButton.buttonText = "爆破";

         }
        public static void SetButtonCooldowns() {
            bomberButton.MaxTimer = cooldown;
            bomberButton.EffectDuration = duration;
            releaseButton.MaxTimer = 0f;
         }

        public static void Clear()
        {
            bombTarget = null;
            currentTarget = null;
            tmpTarget = null;
            arrows = new List<Arrow>();
            players = new List<BomberB>();
        }
        public static bool isAlive()
        {
            foreach(var bomber in players)
            {
                if(bomber.player.isAlive())
                    return true;
            }
            return false;
        }
        public static Sprite getBomberButtonSprite()
        {
            if (bomberButtonSprite) return bomberButtonSprite;
            bomberButtonSprite = ModTranslation.getImage("PlantBombButton.png", 115f);
            return bomberButtonSprite;
        }
        public static Sprite getReleaseButtonSprite()
        {
            if (releaseButtonSprite) return releaseButtonSprite;
            releaseButtonSprite = ModTranslation.getImage("ReleaseButton.png", 115f);
            return releaseButtonSprite;
        }
        static void arrowUpdate()
        {
            if(BomberA.bombTarget == null || BomberB.bombTarget == null) return;

            // 前フレームからの経過時間をマイナスする
            updateTimer -= Time.fixedDeltaTime;

            // 1秒経過したらArrowを更新
            if(updateTimer <= 0.0f){

                // 前回のArrowをすべて破棄する
                foreach(Arrow arrow in arrows){
                    if(arrow != null)
                    {
                        arrow.arrow.SetActive(false);
                        UnityEngine.Object.Destroy(arrow.arrow);
                    }
                }

                // Arrorw一覧
                arrows = new List<Arrow>();

                // 相方の位置を示すArrorwを描画
                foreach(PlayerControl p in PlayerControl.AllPlayerControls){
                    if(p.Data.IsDead) continue;
                    if(p.isRole(RoleId.BomberA))
                    {
                        Arrow arrow;
                        arrow = new Arrow(Color.red);
                        arrow.arrow.SetActive(true);
                        arrow.Update(p.transform.position);
                        arrows.Add(arrow);
                    }
                }

                // タイマーに時間をセット
                updateTimer = arrowUpdateInterval;
            }
        }
    }
}