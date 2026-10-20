using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LittleFoxHome
{
    /// <summary>
    /// 故事4 · 回家之路：路标有三个方向箭头。妈妈的呼唤从某个方向传来——
    /// 跟着发光光圈的方向点箭头，连对 3 次，小狐狸就能找到回家的路。
    /// </summary>
    public class LF_Story4Page : LF_PageBase
    {
        bool _done;
        int _round;
        int _mistakes;
        int _hintDir;   // 0=左 1=上 2=右
        Image _glow;

        protected override void OnSetup()
        {
            EnableNav();

            // 跟随提示用的黄色呼吸光圈（盖在被选中的箭头位置上）
            _glow = LF_Util.MakeImage("Glow", Root, Rect.zero, LF_Util.RingSprite(), false);
            _glow.color = new Color(1f, 0.85f, 0.3f, 0.8f);
            _glow.raycastTarget = false;

            AddZoneButton(LF_Zones.S4ArrowLeft,  () => TryArrow(0));
            AddZoneButton(LF_Zones.S4ArrowUp,     () => TryArrow(1));
            AddZoneButton(LF_Zones.S4ArrowRight, () => TryArrow(2));

            // 可点击小装饰：路边花朵
            AddClickyFlower(new Vector2(0.10f, 0.90f));
            AddClickyFlower(new Vector2(0.90f, 0.90f));
            AddClickyStar(new Vector2(0.50f, 0.10f));

            // 点路牌会说话
            AddZoneButton(new Rect(0.40f, 0.50f, 0.20f, 0.15f), () =>
            {
                LF_Sfx.Chime();
                LF_GameManager.I.Toast("Which way home? 回家走哪边？", "Follow the glowing arrow!", 1.5f);
            });

            StartCoroutine(NextRound());
        }

        IEnumerator NextRound()
        {
            _round++;
            if (_round > 3) { Finish(); yield break; }

            // 随机选一个方向作为"妈妈呼唤的方向"
            _hintDir = Random.Range(0, 3);
            Rect art = _hintDir == 0 ? LF_Zones.S4ArrowLeft
                     : _hintDir == 1 ? LF_Zones.S4ArrowUp
                     : LF_Zones.S4ArrowRight;
            Rect rc = ArtRectToCanvas(art);
            float size = Mathf.Max(rc.width, rc.height) * 1.7f;
            _glow.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
            _glow.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
            _glow.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _glow.rectTransform.anchorMin = _glow.rectTransform.anchorMax = new Vector2(0f, 0f);
            _glow.rectTransform.anchoredPosition = rc.center;
            _glow.enabled = true;
            StartCoroutine(PulseGlow());

            LF_GameManager.I.Toast("Round " + _round + ": follow the glow arrow!",
                "跟着发光的箭头方向走", 1.6f);
            yield return null;
        }

        IEnumerator PulseGlow()
        {
            float t = 0f;
            while (_glow != null && !_done)
            {
                t += Time.deltaTime * 3f;
                _glow.rectTransform.localScale = Vector3.one * (1f + 0.12f * Mathf.Sin(t));
                yield return null;
            }
        }

        void TryArrow(int dir)
        {
            if (_done) return;
            if (dir == _hintDir)
            {
                LF_Sfx.Pop();
                LF_GameManager.I.Toast("Right way! 对啦！", "The fox trots forward.", 1.3f);
                StartCoroutine(NextRound());
            }
            else
            {
                _mistakes++;
                LF_Sfx.Error();
                LF_GameManager.I.Toast("Wrong way! Try again. 走错方向啦，再试一次！",
                    "Watch where the glow is pointing.", 1.4f);
            }
        }

        void Finish()
        {
            _done = true;
            _glow.enabled = false;
            LF_Sfx.Success();
            LF_GameManager.I.Toast("Home! Mom fox is waiting! 到家啦，妈妈在等小狐狸！",
                "Three right turns, and you're home.", 2f);
            int stars = (_mistakes <= 1) ? 3 : (_mistakes <= 3 ? 2 : 1);
            LF_GameManager.I.PageComplete(Page, stars, 30, PageId.Credits);
        }

        protected override KeyValuePair<string, string> StoryText()
        {
            return new KeyValuePair<string, string>(
                "岔路口有三个方向。跟着发光箭头的方向选路，连对 3 次就能回家！",
                "Three ways at the fork. Follow the glowing arrow and choose correctly 3 times to get home!");
        }
    }
}
