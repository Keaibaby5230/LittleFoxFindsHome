using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LittleFoxHome
{
    /// <summary>
    /// 故事2 · 过河：
    /// A) 点木头 → 木头滑到河中间搭桥，狐狸走过去；
    /// B) 按顺序点大石头→小石头，狐狸依次跳过石头。
    /// </summary>
    public class LF_Story2Page : LF_PageBase
    {
        bool _done;
        bool _stone1Tapped;
        int _mistakes;

        protected override void OnSetup()
        {
            EnableNav();

            // 方式 A：点木头（art 左上原点 y 向下，木头在画面下方 art y≈0.77）
            AddZoneButton(new Rect(0.18f, 0.74f, 0.22f, 0.08f), () =>
            {
                if (_done) return;
                StartCoroutine(CrossByBridge());
            });

            // 方式 B：先点大石头，再点小石头
            AddCircleZone(new Vector2(0.57f, 0.76f), 0.06f, () =>
            {
                if (_done) return;
                if (_stone1Tapped) { _mistakes++; LF_Sfx.Error(); return; }
                _stone1Tapped = true;
                LF_Sfx.Pop();
                LF_GameManager.I.Toast("Good! Now the small stone! 很好，再点小石头！", "Almost there!", 1.4f);
            });
            AddCircleZone(new Vector2(0.57f, 0.86f), 0.05f, () =>
            {
                if (_done) return;
                if (!_stone1Tapped)
                {
                    _mistakes++;
                    LF_Sfx.Error();
                    LF_GameManager.I.Toast("Try the big stone first! 先点大石头！", "Start with the bigger stone.", 1.4f);
                    return;
                }
                StartCoroutine(CrossByStones());
            });

            // 装饰
            AddClickyFlower(new Vector2(0.10f, 0.85f));
            AddClickyFlower(new Vector2(0.90f, 0.85f));
            AddClickyStar(new Vector2(0.20f, 0.15f));
            AddClickyStar(new Vector2(0.80f, 0.15f));
        }

        // A) 木头滑到河中间
        IEnumerator CrossByBridge()
        {
            _done = true;
            LF_Sfx.Snap();

            // 建一个木头精灵（棕色圆角矩形）
            Vector2 startC = ArtToCanvas(new Vector2(0.28f, 0.77f));
            Vector2 midC = ArtToCanvas(new Vector2(0.50f, 0.55f));
            var log = LF_Util.MakeImage("log", Root, Rect.MinMaxRect(startC.x - 80, startC.y - 25, startC.x + 80, startC.y + 25),
                LF_Util.MakeRoundRectSprite(160, 50, 12, new Color(0.55f, 0.35f, 0.15f, 1f)), false);

            float t = 0f;
            while (t < 1.0f && log != null)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t);
                Vector2 p = Vector2.Lerp(startC, midC, k);
                log.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                log.rectTransform.anchoredPosition = p;
                log.rectTransform.localRotation = Quaternion.Euler(0, 0, 0);
                yield return null;
            }

            LF_Sfx.Pop();
            LF_GameManager.I.Toast("Timber! Bridge is ready! 木头搭桥啦！", "Now walk across!", 1.6f);
            yield return new WaitForSeconds(0.6f);
            LF_Sfx.Success();
            LF_GameManager.I.PageComplete(Page, 3, 20, PageId.Story3);
        }

        // B) 狐狸依次跳石头
        IEnumerator CrossByStones()
        {
            _done = true;
            LF_Sfx.Pop();

            // 建一个小狐狸精灵（橙色圆）
            Vector2 foxStart = ArtToCanvas(new Vector2(0.20f, 0.63f));
            Vector2 stone1 = ArtToCanvas(new Vector2(0.57f, 0.76f));
            Vector2 stone2 = ArtToCanvas(new Vector2(0.57f, 0.86f));
            Vector2 otherSide = ArtToCanvas(new Vector2(0.85f, 0.63f));

            var fox = LF_Util.MakeImage("fox", Root, Rect.MinMaxRect(foxStart.x - 40, foxStart.y - 40, foxStart.x + 40, foxStart.y + 40),
                LF_Util.CircleSprite(), false);
            fox.color = new Color(1f, 0.6f, 0.2f, 1f);

            yield return StartCoroutine(HopTo(fox.rectTransform, stone1));
            LF_Sfx.Pop();
            yield return StartCoroutine(HopTo(fox.rectTransform, stone2));
            LF_Sfx.Pop();
            yield return StartCoroutine(HopTo(fox.rectTransform, otherSide));

            Destroy(fox.gameObject);
            LF_Sfx.Success();
            LF_GameManager.I.Toast("Hop hop! Crossed! 跳过来啦！", "Two stones, one brave fox!", 1.8f);
            int stars = (_mistakes <= 1) ? 3 : (_mistakes <= 3 ? 2 : 1);
            yield return new WaitForSeconds(0.5f);
            LF_GameManager.I.PageComplete(Page, stars, 20, PageId.Story3);
        }

        IEnumerator HopTo(RectTransform rt, Vector2 target)
        {
            Vector2 start = rt.anchoredPosition;
            float t = 0f;
            const float dur = 0.5f;
            while (t < dur && rt != null)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / dur);
                rt.anchoredPosition = Vector2.Lerp(start, target, k);
                rt.localScale = Vector3.one * (1f + Mathf.Sin(k * Mathf.PI) * 0.2f);
                yield return null;
            }
            if (rt != null) { rt.anchoredPosition = target; rt.localScale = Vector3.one; }
        }

        protected override KeyValuePair<string, string> StoryText()
        {
            return new KeyValuePair<string, string>(
                "一条小河挡住了去路。点木头搭座桥，或者按顺序点两块石头跳过去！",
                "A river blocks the way. Tap the wood to build a bridge, or tap the big stone then the small stone to hop across!");
        }
    }
}
