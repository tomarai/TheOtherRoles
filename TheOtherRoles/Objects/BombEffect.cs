using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace TheOtherRoles{
    class BombEffect {
        public static List<BombEffect> bombeffects = new List<BombEffect>();

        public GameObject bombeffect;
        private GameObject background = null;

        private static Sprite bombeffectSprite;
        public static Sprite getBombEffectSprite() {
            if (bombeffectSprite) return bombeffectSprite;
            bombeffectSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.BombEffect.png", 300f);
            return bombeffectSprite;
        }

        public BombEffect(PlayerControl player) {
            bombeffect = new GameObject("BombEffect");
            Vector3 position = new Vector3(player.transform.localPosition.x, player.transform.localPosition.y, player.transform.localPosition.z - 0.001f); // just behind player
            bombeffect.transform.position = position;
            bombeffect.transform.localPosition = position;

            var bombeffectRenderer = bombeffect.AddComponent<SpriteRenderer>();
            bombeffectRenderer.sprite = getBombEffectSprite();
            bombeffect.SetActive(true);
            bombeffects.Add(this);
        }

        public static void clearBombEffects() {
            foreach(BombEffect bombeffect in bombeffects){
                bombeffect.bombeffect.SetActive(false);
            }
            bombeffects = new List<BombEffect>();
        }

        public static void UpdateAll() {
            foreach (BombEffect bombeffect in bombeffects) {
                if (bombeffect != null)
                    bombeffect.Update();
            }
        }

        public void Update() {
            if (background != null)
                background.transform.Rotate(Vector3.forward * 6 * Time.fixedDeltaTime);
        }
    }
}