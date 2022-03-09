using System;
using Hazel;
using HarmonyLib;
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
    public class Puppeteer : RoleBase<Puppeteer>
    {
        public static Color color = Palette.Purple;
        public static int counter = 0;
        public static int numKills {get {return (int)CustomOptionHolder.puppeteerNumKills.getFloat();}}
        public static float sampleDuration {get {return CustomOptionHolder.puppeteerSampleDuration.getFloat();}}
        public static bool canControlDummyEvenIfDead {get {return CustomOptionHolder.puppeteerCanControlDummyEvenIfDead.getBool();}}
        public static int penaltyOnDeath {get {return (int)CustomOptionHolder.puppeteerPenaltyOnDeath.getFloat();}}
        public static bool losesSenriganOnDeath {get {return CustomOptionHolder.puppeteerLosesSenriganOnDeath.getBool();}}
        public static bool triggerPuppeteerWin = false;
        public static bool isActive = false;
        public static bool canSpawn = true;
        public static PlayerControl dummy = null;
        public static PlayerControl target = null;
        public static PlayerControl currentTarget = null;
        public static PlayerControl tmpTarget = null;
        public static bool stealthed = false;
        public static CustomButton sampleButton;
        public static Sprite sampleButtonSprite;
        public static CustomButton puppeteerButton;
        public static Sprite puppeteerButtonSprite;
        public static List<Arrow> arrows = new List<Arrow>();
        public static float arrowUpdateInterval = 0.5f;
        public static float updateTimer = 0f;
        public static float posUpdateTimer = 0f;
        public static AudioClip laugh;
        public static bool soundFlag;
        
        public static TMPro.TMP_Text puppeteerText;

        public Puppeteer()
        {
            RoleType = roleId = RoleType.Puppeteer;
        }

        public override void OnMeetingStart()
        {
            bool isAlive = Puppeteer.allPlayers.FindAll(x=> x.isAlive()).Count >= 1;
            if(soundFlag && isAlive)
            {
                SoundManager._Instance.PlaySound(laugh, false, 1f);
            }
            soundFlag = false;
            if(!isAlive && (PlayerControl.LocalPlayer.isImpostor() || PlayerControl.LocalPlayer.isRole(RoleType.Jackal)))
            {
                string msg = $"人形遣いのカウント数 {counter}/{numKills}";
                if (AmongUsClient.Instance.AmClient && DestroyableSingleton<HudManager>.Instance)
                {
                    DestroyableSingleton<HudManager>.Instance.Chat.AddChat(PlayerControl.LocalPlayer, msg);
                }
            }

        }

        public override void OnMeetingEnd()
        {
            target = null;
            canSpawn = false;
            isActive = false;
            if(PlayerControl.LocalPlayer.isRole(RoleType.Puppeteer))
            {
                switchStealth(false);
            }
        }
        public override void FixedUpdate() 
        {
            if(PlayerControl.LocalPlayer.isRole(RoleType.Puppeteer))
            {
                currentTarget = setTarget();
                setPlayerOutline(currentTarget, Puppeteer.color);
                arrowUpdate();
                syncDummyPos();
            }
        }
        public override void OnKill(PlayerControl target) { }
        public override void OnDeath(PlayerControl killer = null)
        {
            counter -= penaltyOnDeath;
        } 
        public override void HandleDisconnect(PlayerControl player, DisconnectReasons reason) { }

        public static Sprite getSampleButtonSprite()
        {
            if (sampleButtonSprite) return sampleButtonSprite;
            sampleButtonSprite = ModTranslation.getImage("SampleButton.png", 115f);
            return sampleButtonSprite;
        }
        public static Sprite getPuppeteerButtonSprite()
        {
            if (puppeteerButtonSprite) return puppeteerButtonSprite;
            puppeteerButtonSprite = ModTranslation.getImage("PuppeteerButton.png", 115f);
            return puppeteerButtonSprite;
        }
        public static void MakeButtons(HudManager hm) 
        {
            sampleButton = new CustomButton(
                // OnClick
                () =>
                {
                    if (currentTarget != null)
                    {
                        tmpTarget = currentTarget;
                        sampleButton.HasEffect = true;
                        puppeteerButton.MaxTimer = 0f;
                        puppeteerButton.Timer = 0f;
                    }
                },
                // HasButton
                () => { return PlayerControl.LocalPlayer.isRole(RoleType.Puppeteer)  && (PlayerControl.LocalPlayer.isAlive() || canControlDummyEvenIfDead); },
                // CouldUse
                () =>
                {
                    if (sampleButton.isEffectActive && tmpTarget != currentTarget)
                    {
                        tmpTarget = null;
                        sampleButton.Timer = 0f;
                        sampleButton.isEffectActive = false;
                    }

                    return PlayerControl.LocalPlayer.CanMove && currentTarget != null;
                },
                // OnMeetingEnds
                () =>
                {
                    sampleButton.Timer = sampleButton.MaxTimer;
                    sampleButton.isEffectActive = false;
                    target = null;
                    tmpTarget = null;
                },
                getSampleButtonSprite(), 
                new Vector3(-0.9f, 1f, 0),
                hm,
                hm.KillButton,
                KeyCode.G,
                true,
                sampleDuration,
                // OnEffectsEnd
                () =>
                {
                    if (tmpTarget != null)
                    {
                        target = tmpTarget;
                        canSpawn = true;
                    }

                    tmpTarget = null;
                    sampleButton.Timer = sampleButton.MaxTimer;

                }
            );
            sampleButton.buttonText = "";

            puppeteerButton = new CustomButton(
                // OnClick
                () =>
                {
                    if(canSpawn)
                    {
                        spawnDummy();
                        switchStealth(true);
                    }
                    else
                    {
                        switchStealth(!stealthed);
                    }
                },
                // HasButton
                () => { return PlayerControl.LocalPlayer.isRole(RoleType.Puppeteer)  && (PlayerControl.LocalPlayer.isAlive() || canControlDummyEvenIfDead) && target != null; },
                // CouldUse
                () =>
                {
                    if(puppeteerText != null)
                    {
                        puppeteerText.text = $"{counter}/{numKills}";
                    }
                    return true;
                },
                // OnMeetingEnds
                () =>
                {
                    puppeteerButton.Timer = puppeteerButton.MaxTimer;
                },
                getPuppeteerButtonSprite(), 
                new Vector3(0.0f, 1f, 0),
                hm,
                hm.UseButton,
                KeyCode.F,
                false
            );
            puppeteerButton.buttonText = "";
            puppeteerText = GameObject.Instantiate(puppeteerButton.actionButton.cooldownTimerText, puppeteerButton.actionButton.cooldownTimerText.transform.parent);
            puppeteerText.text = "";
            puppeteerText.enableWordWrapping = false;
            puppeteerText.transform.localScale = Vector3.one * 0.5f;
            puppeteerText.transform.localPosition += new Vector3(-0.05f, 0.7f, 0);

        }
        public static void SetButtonCooldowns()
        {
            sampleButton.MaxTimer = 10f;
            puppeteerButton.MaxTimer = 0f;
        }

        public static void Clear()
        {
            laugh = FileImporter.ImportWAVAudio("TheOtherRoles.Resources.PuppeteerLaugh.wav", false);
            soundFlag = false;
            players = new List<Puppeteer>();
            if(dummy != null) GameData.Instance.RemovePlayer(dummy.PlayerId);
            dummy = null;
            stealthed = false;
            isActive = false;
            canSpawn = false;
            triggerPuppeteerWin = false;
            target = null;
            counter = 0;
            foreach(Arrow arrow in arrows){
                if(arrow != null && arrow.arrow != null)
                {
                    arrow.arrow.SetActive(false);
                    UnityEngine.Object.Destroy(arrow.arrow);
                }
            }
            arrows = new List<Arrow>();
            originalZoom = 0;
        }

        public static void spawnDummy()
        {
            MessageWriter writer;
            if(dummy == null)
            {
                var playerId = (byte) GameData.Instance.GetAvailableId();
                writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SpawnDummy, Hazel.SendOption.Reliable, -1);
                writer.Write(playerId);
                writer.Write(PlayerControl.LocalPlayer.transform.position.x);
                writer.Write(PlayerControl.LocalPlayer.transform.position.y);
                writer.Write(PlayerControl.LocalPlayer.transform.position.z);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                RPCProcedure.spawnDummy(playerId, PlayerControl.LocalPlayer.transform.position);
            }
            writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.MoveDummy, Hazel.SendOption.Reliable, -1);
            writer.Write(PlayerControl.LocalPlayer.transform.position.x);
            writer.Write(PlayerControl.LocalPlayer.transform.position.y);
            writer.Write(PlayerControl.LocalPlayer.transform.position.z);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            // 暫定遅延実行　何故か透明化が解除されないため
            DestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(0.25f, new Action<float>( p =>{
                if(p== 1)
                {
                    RPCProcedure.moveDummy(PlayerControl.LocalPlayer.transform.position);
                }
            })));
            if(target != null)
            {
                writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.PuppeteerMorph, Hazel.SendOption.Reliable, -1);
                writer.Write(target.PlayerId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                RPCProcedure.puppeteerMorph(target.PlayerId);
            }
            canSpawn = false;
            isActive = true;
        }
        
        public static float originalZoom = 0f;
        public static void senrigan(bool toggle)
        {
            // 初回呼び出し時にカメラのズーム率を保持しておく
            if(originalZoom ==0) originalZoom = Camera.main.orthographicSize;
            if(!toggle)
            {
                Camera.main.orthographicSize = originalZoom;
                DestroyableSingleton<HudManager>.Instance.UICamera.orthographicSize = originalZoom;
                if(PlayerControl.LocalPlayer.isAlive())
                {
                    DestroyableSingleton<HudManager>.Instance.ShadowQuad.gameObject.SetActive(true);
                }
            }
            else
            {
                Camera.main.orthographicSize = originalZoom * 3;
                DestroyableSingleton<HudManager>.Instance.UICamera.orthographicSize =originalZoom * 3;
                DestroyableSingleton<HudManager>.Instance.ShadowQuad.gameObject.SetActive(false);
            }
        }
        public static void switchStealth(bool flag)
        {
            if(!flag)
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.PuppeteerStealth, Hazel.SendOption.Reliable, -1);
                writer.Write(false);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                RPCProcedure.puppeteerStealth(false);
                var hudManager = DestroyableSingleton<HudManager>.Instance;
                hudManager.PlayerCam.SetTarget(PlayerControl.LocalPlayer);
                senrigan(false);
                var player = PlayerControl.LocalPlayer;
                player.myLight = UnityEngine.Object.Instantiate<LightSource>(player.LightPrefab);
                player.myLight.transform.SetParent(player.transform);
                player.myLight.transform.localPosition = player.Collider.offset;
                PlayerControl.LocalPlayer.moveable = true;
            }
            else
            {
                // 常に自身の位置から人形をスタートさせる
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.MoveDummy, Hazel.SendOption.Reliable, -1);
                writer.Write(PlayerControl.LocalPlayer.transform.position.x);
                writer.Write(PlayerControl.LocalPlayer.transform.position.y);
                writer.Write(PlayerControl.LocalPlayer.transform.position.z);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                RPCProcedure.moveDummy(PlayerControl.LocalPlayer.transform.position);

                writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.PuppeteerStealth, Hazel.SendOption.Reliable, -1);
                writer.Write(true);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                RPCProcedure.puppeteerStealth(true);
                var hudManager = DestroyableSingleton<HudManager>.Instance;
                var dummy = Puppeteer.dummy;
                hudManager.PlayerCam.SetTarget(dummy);
                if(losesSenriganOnDeath)
                {
                    bool isAlive = Puppeteer.allPlayers.FindAll(x=> x.isAlive()).Count >= 1;
                    senrigan(isAlive);
                }
                else
                {
                    senrigan(true);
                }
                dummy.myLight = UnityEngine.Object.Instantiate<LightSource>(dummy.LightPrefab);
                dummy.myLight.transform.SetParent(dummy.transform);
                dummy.myLight.transform.localPosition = dummy.Collider.offset;
                PlayerControl.LocalPlayer.NetTransform.Halt();
                PlayerControl.LocalPlayer.moveable = false;

            }
        }

        public static void setStealthed(bool stealthed = true)
        {
                Puppeteer.stealthed = stealthed;
                if(Puppeteer.stealthed)
                {
                    KeyboardJoystickUpdatePatch.up = false;
                    KeyboardJoystickUpdatePatch.down = false;
                    KeyboardJoystickUpdatePatch.left= false;
                    KeyboardJoystickUpdatePatch.right= false;
                }
        }

        public static void OnTargetExiled()
        {
            bool isAlive = Puppeteer.allPlayers.FindAll(x=> x.isAlive()).Count >= 1;
            if(!target.isImpostor() && !target.isRole(RoleType.Jackal) && isAlive)
            {
                counter += 1;
            }
            if(counter >= numKills && PlayerControl.LocalPlayer.isRole(RoleType.Puppeteer))
            {
                MessageWriter winWriter = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.PuppeteerWin, Hazel.SendOption.Reliable, -1);
                AmongUsClient.Instance.FinishRpcImmediately(winWriter);
                RPCProcedure.puppeteerWin();
            }
        }

        public static void OnDummyDeath(PlayerControl killer)
        {
            // クルーがダミーを殺した場合は本体が死ぬのでカウント対象外とする
            if(!killer.isCrew())
                counter += 1;
                soundFlag = true;

            // 人形遣い死亡時は空キルになるのでクールダウンにしない
            bool isAlive = Puppeteer.allPlayers.FindAll(x=> x.isAlive()).Count >= 1;
            if(!isAlive)
            {
                killer.SetKillTimer(0f);
            }

            // 人形遣い専用の処理なので人形遣い以外はreturn
            if(!PlayerControl.LocalPlayer.isRole(RoleType.Puppeteer)) return;

            // 勝利条件を満たしていたら勝利
            if(counter >= numKills)
            {
                MessageWriter winWriter = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.PuppeteerWin, Hazel.SendOption.Reliable, -1);
                AmongUsClient.Instance.FinishRpcImmediately(winWriter);
                RPCProcedure.puppeteerWin();
            }

            // ダミー死亡時に連動して発動するキル処理
            if(target.isAlive() && isAlive && !killer.isCrew())
            {
                MessageWriter killWriter = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.PuppeteerKill, Hazel.SendOption.Reliable, -1);
                killWriter.Write(killer.PlayerId);
                killWriter.Write(target.PlayerId);
                AmongUsClient.Instance.FinishRpcImmediately(killWriter);
                RPCProcedure.puppeteerKill(killer.PlayerId, target.PlayerId);
            }
            else if(isAlive && killer.isCrew()) // ダミーをクルーがキルした場合は人形遣いが死亡する
            {
                MessageWriter killWriter = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.PuppeteerKill, Hazel.SendOption.Reliable, -1);
                killWriter.Write(killer.PlayerId);
                killWriter.Write(PlayerControl.LocalPlayer.PlayerId);
                AmongUsClient.Instance.FinishRpcImmediately(killWriter);
                RPCProcedure.puppeteerKill(killer.PlayerId, PlayerControl.LocalPlayer.PlayerId);
            }


            isActive = false;
            canSpawn = false;
            switchStealth(false);
        }

        static void arrowUpdate(){

            // 前フレームからの経過時間をマイナスする
            updateTimer -= Time.fixedDeltaTime;

            // 1秒経過したらArrowを更新
            if(updateTimer <= 0.0f){

                // 前回のArrowをすべて破棄する
                foreach(Arrow arrow in arrows){
                    if (arrow != null)
                    {
                        arrow.arrow.SetActive(false);
                        UnityEngine.Object.Destroy(arrow.arrow);
                    }
                }

                // Arrorw一覧
                arrows = new List<Arrow>();

                // インポスターの位置を示すArrorwを描画
                foreach(PlayerControl p in PlayerControl.AllPlayerControls){
                    if(p.Data.IsDead) continue;
                    Arrow arrow;
                    if(p.Data.Role.IsImpostor || p.isRole(RoleType.Jackal) || p.isRole(RoleType.Sheriff) || p == target){
                        if(p.Data.Role.IsImpostor){
                            arrow = new Arrow(Color.red);
                        }
                        else if(p.isRole(RoleType.Jackal) || (p.isRole(RoleType.SchrodingersCat) && SchrodingersCat.jackalFlag)){
                            arrow = new Arrow(Jackal.color);
                        }else if(p.isRole(RoleType.Sheriff)){
                            arrow = new Arrow(Color.white);
                        }else if(p==target)
                        {
                            arrow = new Arrow(Puppeteer.color);
                        }else{
                            arrow = new Arrow(Color.black);
                        }
                        arrow.arrow.SetActive(true);
                        arrow.Update(p.transform.position);
                        arrows.Add(arrow);
                    }
                }

                // タイマーに時間をセット
                updateTimer = arrowUpdateInterval;
            }
        }
        static void syncDummyPos()
        {

            // 前フレームからの経過時間をマイナスする
            posUpdateTimer -= Time.fixedDeltaTime;

            // 1秒経過したらArrowを更新
            if(updateTimer <= 0.0f){

                if(dummy != null)
                {
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.MoveDummy, Hazel.SendOption.Reliable, -1);
                    writer.Write(Puppeteer.dummy.transform.position.x);
                    writer.Write(Puppeteer.dummy.transform.position.y);
                    writer.Write(Puppeteer.dummy.transform.position.z);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                }

                // タイマーに時間をセット
                posUpdateTimer = 1f;
            }
        }

        // 投票画面にダミーを表示させない
        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.PopulateButtons))]
        public static class MeetingHudPopulateButtonsPatch
        {
            public static bool Prefix(MeetingHud __instance, byte reporter)
            {
                if(dummy == null) return true;
                __instance.playerStates = new PlayerVoteArea[GameData.Instance.PlayerCount-1];
                int counter = 0;
                for (int i = 0; i < __instance.playerStates.Length + 1; i++)
                {
                    if(GameData.Instance.AllPlayers[i].PlayerId == dummy.PlayerId)
                    {
                        continue;
                    }
                    GameData.PlayerInfo playerInfo = GameData.Instance.AllPlayers[i];
                    PlayerVoteArea playerVoteArea = __instance.playerStates[counter] = __instance.CreateButton(playerInfo);
                    playerVoteArea.Parent = __instance;
                    playerVoteArea.SetTargetPlayerId(playerInfo.PlayerId);
                    playerVoteArea.SetDead(reporter == playerInfo.PlayerId, playerInfo.Disconnected || playerInfo.IsDead, playerInfo.Role.Role == RoleTypes.GuardianAngel);
                    playerVoteArea.UpdateOverlay();
                    counter++;
                }
                foreach (PlayerVoteArea playerVoteArea2 in __instance.playerStates)
                {
                    ControllerManager.Instance.AddSelectableUiElement(playerVoteArea2.PlayerButton, false);
                }
                __instance.SortButtons();
                return false;
            }

        }

        public static void setOpacity(PlayerControl player, float opacity)
        {
            // Sometimes it just doesn't work?
            var color = Color.Lerp(Palette.ClearWhite, Palette.White, opacity);
            try
            {
                if (player.MyPhysics?.rend != null)
                    player.MyPhysics.rend.color = color;

                if (player.MyPhysics?.Skin?.layer != null)
                    player.MyPhysics.Skin.layer.color = color;

                if (player.HatRenderer != null)
                    player.HatRenderer.color = color;

                if (player.CurrentPet?.rend != null)
                    player.CurrentPet.rend.color = color;

                if (player.CurrentPet?.shadowRend != null)
                    player.CurrentPet.shadowRend.color = color;

                if (player.VisorSlot != null)
                    player.VisorSlot.color = color;
            }
            catch { }
        }

        [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.FixedUpdate))]
        public static class PlayerPhysicsPatch
        {
            public static void Postfix(PlayerPhysics __instance)
            {

                if (isRole(__instance.myPlayer))
                {
                    var puppeteer = __instance.myPlayer;
                    if (puppeteer == null || puppeteer.isDead()) return;

                    bool canSee = 
                        PlayerControl.LocalPlayer.isRole(RoleType.Puppeteer) ||
                        PlayerControl.LocalPlayer.isDead();

                    var opacity = canSee ? 0.1f : 0.0f;

                    if (stealthed)
                    {
                        puppeteer.myRend.material.SetFloat("_Outline", 0f);
                    }
                    else
                    {
                        opacity = 1.0f;
                    }

                    setOpacity(puppeteer, opacity);
                }
                else if (__instance.myPlayer == dummy)
                {
                    var dummy = __instance.myPlayer;
                    if (dummy == null || dummy.isDead()) return;

                    bool canSee = 
                        PlayerControl.LocalPlayer.isRole(RoleType.Puppeteer) ||
                        PlayerControl.LocalPlayer.isDead();

                    var opacity = canSee ? 0.1f : 0.0f;

                    if (!stealthed)
                    {
                        dummy.myRend.material.SetFloat("_Outline", 0f);
                    }
                    else
                    {
                        opacity = 1.0f;
                    }
                    setOpacity(dummy, opacity);
                }
            }
        }

        [HarmonyPatch(typeof(ExileController), nameof(ExileController.Begin))]
        class ExileControllerBeginPatch {
            public static void Prefix(ExileController __instance, [HarmonyArgument(0)]ref GameData.PlayerInfo exiled, [HarmonyArgument(1)]bool tie) {
                if (exiled != null && exiled.Object == target)
                {
                    OnTargetExiled();
                }
            }
        }

        [HarmonyPatch(typeof(KeyboardJoystick), nameof(KeyboardJoystick.Update))]
        public static class KeyboardJoystickUpdatePatch
        {
            public static bool up = false;
            public static bool down = false;
            public static bool right = false;
            public static bool left = false;
            public static void Postfix(KeyboardJoystick __instance)
            {
                if(!PlayerControl.LocalPlayer.isRole(RoleType.Puppeteer)) return;

                if (stealthed)
                {
                    // 梯子を使う/ドアを開ける
                    if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
                    {
                        PlainDoor[] doors;
                        if(PlayerControl.GameOptions.MapId == 4)
                        {
                            doors = DestroyableSingleton<AirshipStatus>.Instance.GetComponentsInChildren<PlainDoor>();
                        }
                        else if(PlayerControl.GameOptions.MapId == 2)
                        {
                            doors = DestroyableSingleton<PolusShipStatus>.Instance.GetComponentsInChildren<PlainDoor>();
                        }
                        else if(PlayerControl.GameOptions.MapId == 1)
                        {
                            doors = DestroyableSingleton<MiraShipStatus>.Instance.GetComponentsInChildren<PlainDoor>();
                        }
                        else
                        {
                            doors = DestroyableSingleton<SkeldShipStatus>.Instance.GetComponentsInChildren<PlainDoor>();
                        }
                        PlainDoor t = null;
                        foreach(var door in doors)
                        {
                            float distance = Vector2.Distance(door.transform.position, dummy.transform.position);
                            if(distance < 1.5f)
                            {
                                t = door;
                                break;
                            }
                        }
                        if(t != null)
                        {
                            DestroyableSingleton<ShipStatus>.Instance.RpcRepairSystem(SystemTypes.Doors, t.Id | 64);
                            t.SetDoorway(true);
                        }


                        Ladder[] ladders = DestroyableSingleton<AirshipStatus>.Instance.GetComponentsInChildren<Ladder>();
                        Ladder target = null;
                        foreach(var ladder in ladders)
                        {
                            float distance = Vector2.Distance(ladder.transform.position, dummy.transform.position);
                            if(distance < 0.5f)
                            {
                                target = ladder;
                                break;
                            }
                        }
                        if (target != null)
                        {
                            MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.PuppeteerClimbRadder, Hazel.SendOption.Reliable, -1);
                            messageWriter.Write(dummy.PlayerId);
                            messageWriter.Write(target.Id);
                            AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
                            RPCProcedure.puppeteerClimbRadder(dummy.PlayerId, target.Id);
                        }
                    }

                    if (Input.GetKeyDown(KeyCode.D))
                        right = true;
                    if (Input.GetKeyUp(KeyCode.D))
                        right = false;
                    if (Input.GetKeyDown(KeyCode.A))
                        left = true;
                    if (Input.GetKeyUp(KeyCode.A))
                        left = false;
                    if (Input.GetKeyDown(KeyCode.W))
                        up = true;
                    if (Input.GetKeyUp(KeyCode.W))
                        up = false;
                    if (Input.GetKeyDown(KeyCode.S))
                        down = true;
                    if (Input.GetKeyUp(KeyCode.S))
                        down = false;

                    if(Puppeteer.dummy != null)
                    {
                        Vector2 pos = Puppeteer.dummy.transform.position;
                        Vector2 offset = Vector2.zero;
                        if(up) offset += new Vector2(0f, 0.5f);
                        if(down) offset += new Vector2(0f, -0.5f);
                        if(left) offset += new Vector2(-0.5f, 0.0f);
                        if(right) offset += new Vector2(0.5f, 0.0f);
                        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.WalkDummy, Hazel.SendOption.Reliable, -1);
                        writer.Write(offset.x);
                        writer.Write(offset.y);
                        AmongUsClient.Instance.FinishRpcImmediately(writer);
                        RPCProcedure.walkDummy(offset);
                        if(!(up||down||right||left))
                        {
                            writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.MoveDummy, Hazel.SendOption.Reliable, -1);
                            writer.Write(Puppeteer.dummy.transform.position.x);
                            writer.Write(Puppeteer.dummy.transform.position.y);
                            writer.Write(Puppeteer.dummy.transform.position.z);
                            AmongUsClient.Instance.FinishRpcImmediately(writer);
                            // RPCProcedure.moveDummy(Puppeteer.dummy.transform.position);
                        } 
                    }

                }
            }
        }
    }
}