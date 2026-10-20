using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LittleFoxHome
{
    /// <summary>
    /// 页面基类：挂载整页原画上的交互逻辑。
    /// 提供：顶部导航栏（return/read/sound/progress）、通用热区按钮、坐标换算、逐帧动画辅助。
    /// </summary>
    public abstract class LF_PageBase : MonoBehaviour
    {
        public Transform Root { get; private set; }
        public RectTransform PageImage { get; private set; }
        public PageId Page { get; private set; }
        public float Aspect { get; private set; }

        bool _navEnabled;
        bool _done;

        public void Setup(Transform root, RectTransform pageImage, PageId page, float aspect)
        {
            Root = root;
            PageImage = pageImage;
            Page = page;
            Aspect = aspect;
            OnSetup();
        }

        /// <summary>子类初始化入口（此时 Root/Page/Aspect 已就绪）。</summary>
        protected virtual void OnSetup() { }

        // ----------------------------------------------------------------
        // 坐标换算（画布单位）
        // ----------------------------------------------------------------
        protected Vector2 ArtToCanvas(Vector2 art) => LF_Util.ArtToCanvas(art, Aspect);
        protected Rect ArtRectToCanvas(Rect art) => LF_Util.ArtRectToCanvas(art, Aspect);

        // ----------------------------------------------------------------
        // 导航栏（故事页顶部 return / read / sound / progress）
        // ----------------------------------------------------------------
        protected void EnableNav()
        {
            if (_navEnabled) return;
            _navEnabled = true;
            AddZoneButton(LF_Zones.NavReturn, () =>
            {
                LF_Sfx.Tap();
                LF_GameManager.I.ShowPage(PageId.Home);
            });
            AddZoneButton(LF_Zones.NavRead, () =>
            {
                var s = StoryText();
                LF_GameManager.I.ToggleCaption(s.Key, s.Value);
            });
            AddZoneButton(LF_Zones.NavSound, () =>
            {
                var gm = LF_GameManager.I;
                gm.SfxOn = (gm.SfxOn == 0) ? 1 : 0;
                // 同时切换背景音乐
                if (gm.SfxOn == 0) LF_Music.Toggle();   // 音效关→音乐也关
                else LF_Music.Toggle();                  // 音效开→音乐也开
                LF_Sfx.ForceTap();
                bool on = gm.SfxOn != 0;
                gm.Toast(on ? "Sound ON · 声音开" : "Sound OFF · 声音关",
                    on ? "All sound enabled" : "All sound muted", 1.6f);
            });
            AddZoneButton(LF_Zones.NavProgress, () => LF_GameManager.I.OpenProgress());
        }

        /// <summary>每页的朗读文案（中文 + 英文）。</summary>
        protected virtual KeyValuePair<string, string> StoryText()
        {
            return new KeyValuePair<string, string>("", "");
        }

        // ----------------------------------------------------------------
        // 热区按钮
        // ----------------------------------------------------------------
        protected Button AddZoneButton(Rect artRect, UnityAction onClick)
        {
            Rect rc = ArtRectToCanvas(artRect);
            var b = LF_Util.MakeButton("zone_" + artRect.x + "_" + artRect.y, Root, rc, onClick);
            return b;
        }

        protected Button AddCircleZone(Vector2 artCenter, float artRadius, UnityAction onClick)
        {
            Rect art = new Rect(artCenter.x - artRadius, artCenter.y - artRadius, artRadius * 2, artRadius * 2);
            Rect rc = ArtRectToCanvas(art);
            float size = Mathf.Max(rc.width, rc.height) * 1.6f;
            Rect big = new Rect(rc.center.x - size * 0.5f, rc.center.y - size * 0.5f, size, size);
            return LF_Util.MakeButton("circleZone", Root, big, onClick);
        }

        // ----------------------------------------------------------------
        // 简易动画辅助（子类在 Update 中调用或配合协程）
        // ----------------------------------------------------------------
        protected System.Collections.IEnumerator ScalePunch(RectTransform rt, float power, float dur)
        {
            if (rt == null) yield break;
            float t = 0f;
            while (t < dur && rt != null)
            {
                t += Time.deltaTime;
                float k = Mathf.Sin(Mathf.Clamp01(t / dur) * Mathf.PI) * power;
                rt.localScale = Vector3.one * (1f + k);
                yield return null;
            }
            if (rt != null) rt.localScale = Vector3.one;
        }

        protected System.Collections.IEnumerator MoveTo(RectTransform rt, Vector2 target, float dur, UnityAction onEnd = null)
        {
            if (rt == null) yield break;
            float t = 0f;
            Vector2 start = rt.anchoredPosition;
            while (t < dur && rt != null)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur));
                rt.anchoredPosition = Vector2.Lerp(start, target, k);
                yield return null;
            }
            if (rt != null) rt.anchoredPosition = target;
            onEnd?.Invoke();
        }

        // ----------------------------------------------------------------
        // 通用装饰动画（原画小元素 + 程序小物共用）
        // ----------------------------------------------------------------

        /// <summary>左右摇摆（花/草），angle 为最大摆角。</summary>
        protected System.Collections.IEnumerator Sway(RectTransform rt, float angle, float dur)
        {
            if (rt == null) yield break;
            float t = 0f;
            while (rt != null && this != null && enabled)
            {
                t += Time.deltaTime;
                rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 2.2f) * angle);
                yield return null;
                if (dur > 0f && t >= dur) break;
            }
            if (rt != null) rt.localRotation = Quaternion.identity;
        }

        /// <summary>轻跳一下（蘑菇/石头）：缩放脉冲两次。</summary>
        protected System.Collections.IEnumerator BounceOnce(RectTransform rt)
        {
            if (rt == null) yield break;
            float t = 0f;
            const float dur = 0.45f;
            while (t < dur && rt != null)
            {
                t += Time.deltaTime;
                float ph = Mathf.Abs(Mathf.Sin(t * 14f)) * (1f - t / dur * 0.4f);
                rt.localScale = Vector3.one * (1f + 0.22f * ph);
                yield return null;
            }
            if (rt != null) rt.localScale = Vector3.one;
        }

        /// <summary>蝴蝶飘飞：绕原点左右摆动 + 上下浮动，可无限循环（hitStop 秒后点击暂停动画）。</summary>
        protected System.Collections.IEnumerator FloatButterfly(RectTransform rt, Vector2 origin, float ampX, float ampY, float speed)
        {
            if (rt == null) yield break;
            float t = Random.value * 6.28f;
            while (rt != null && this != null && enabled)
            {
                t += Time.deltaTime * speed;
                float sway = Mathf.Sin(t);
                float bob = Mathf.Sin(t * 1.7f);
                rt.anchoredPosition = origin + new Vector2(sway * ampX, bob * ampY);
                rt.localRotation = Quaternion.Euler(0f, 0f, sway * 8f);
                yield return null;
            }
        }

        /// <summary>花瓣飘落：从 startPos 飘到 endPos（带横向摆动），结束后回调。</summary>
        protected System.Collections.IEnumerator FallPetal(RectTransform rt, Vector2 startPos, Vector2 endPos, float dur, UnityAction onEnd = null)
        {
            if (rt == null) yield break;
            float t = 0f;
            while (t < dur && rt != null)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                float sway = Mathf.Sin(t * 5f) * 22f;
                rt.anchoredPosition = Vector2.Lerp(startPos, endPos, k) + Vector2.right * sway * (1f - k * 0.6f);
                rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 4f) * 30f);
                yield return null;
            }
            if (rt != null) rt.anchoredPosition = endPos;
            onEnd?.Invoke();
        }

        // ----------------------------------------------------------------
        // 可点击小装饰：星星 / 花朵，点击弹跳 + 音效 + 加分
        // ----------------------------------------------------------------
        protected void AddClickyStar(Vector2 art)
        {
            Vector2 c = ArtToCanvas(art);
            const float size = 70f;
            var go = LF_Util.NewUI("star", Root);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = c;
            rt.sizeDelta = new Vector2(size, size);
            var img = go.AddComponent<Image>();
            img.sprite = LF_Util.CircleSprite();
            img.color = new Color(1f, 0.95f, 0.4f, 0.9f);
            img.raycastTarget = true;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() =>
            {
                LF_Sfx.Chime();
                LF_GameManager.I.AddScore(5);
                StartCoroutine(PopDecor(rt));
            });
        }

        protected void AddClickyFlower(Vector2 art)
        {
            Vector2 c = ArtToCanvas(art);
            const float size = 60f;
            var go = LF_Util.NewUI("flower", Root);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = c;
            rt.sizeDelta = new Vector2(size, size);
            var img = go.AddComponent<Image>();
            img.sprite = LF_Util.CircleSprite();
            img.color = new Color(1f, 0.6f, 0.75f, 0.9f);
            img.raycastTarget = true;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() =>
            {
                LF_Sfx.Pop();
                LF_GameManager.I.AddScore(5);
                StartCoroutine(PopDecor(rt));
            });
        }

        IEnumerator PopDecor(RectTransform rt)
        {
            float t = 0f;
            while (t < 0.5f && rt != null)
            {
                t += Time.deltaTime;
                float k = Mathf.Sin(t / 0.5f * Mathf.PI);
                rt.localScale = Vector3.one * (1f + k * 0.6f);
                rt.localRotation = Quaternion.Euler(0, 0, k * 20f);
                yield return null;
            }
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.localRotation = Quaternion.identity;
            }
        }

        protected IEnumerator BatFlyAway(Vector2 artStart)
        {
            Vector2 c = ArtToCanvas(artStart);
            const float size = 160f;
            var go = LF_Util.NewUI("bat", Root);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = c;
            rt.sizeDelta = new Vector2(size, size);
            var img = go.AddComponent<Image>();
            img.sprite = LF_Util.CircleSprite();
            img.color = new Color(0.1f, 0.05f, 0.15f, 1f);
            img.raycastTarget = false;

            float t = 0f;
            const float dur = 2.8f;
            Vector2 target = new Vector2(c.x + 400f, c.y + 800f);
            while (t < dur && rt != null)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / dur);
                rt.anchoredPosition = Vector2.Lerp(c, target, k);
                rt.localScale = Vector3.one * (0.6f + k * 0.9f);
                img.color = new Color(0.1f, 0.05f, 0.15f, 1f - k * 0.95f);
                rt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 14f) * 25f);
                yield return null;
            }
            if (go != null) Destroy(go);
        }
    }

    // ----------------------------------------------------------------
    // 通用组件：拖拽（用于主页蝴蝶 / 过河木头）
    // ----------------------------------------------------------------
    public class LF_Draggable : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        RectTransform _rt;
        Canvas _canvas;
        bool _dragging;
        Vector2 _grabOffset;   // 按下时指针相对物体左下角的偏移（画布坐标），保证抓取点始终跟手
        System.Action<Vector2> _onDragMove;   // 参数：画布坐标
        System.Action _onDrop;

        void Awake()
        {
            _rt = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
        }

        public void Configure(System.Action<Vector2> onDragMove, System.Action onDrop)
        {
            _onDragMove = onDragMove;
            _onDrop = onDrop;
        }

        public bool Dragging => _dragging;

        public void OnPointerDown(PointerEventData e)
        {
            _dragging = true;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.transform as RectTransform, e.position, _canvas.worldCamera, out Vector2 local))
            {
                _grabOffset = local - _rt.anchoredPosition;
            }
            LF_Sfx.Pop();
        }

        public void OnDrag(PointerEventData e)
        {
            if (!_dragging) return;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.transform as RectTransform, e.position, _canvas.worldCamera, out Vector2 local))
            {
                _onDragMove?.Invoke(local - _grabOffset);
            }
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (!_dragging) return;
            _dragging = false;
            _onDrop?.Invoke();
        }
    }
}
