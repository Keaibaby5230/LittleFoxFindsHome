using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LittleFoxHome
{
    public enum PageId { Home = 0, Story1 = 1, Story2 = 2, Story3 = 3, Story4 = 4, Credits = 5 }

    /// <summary>
    /// 游戏核心管理器：页面状态机、计分/星级/解锁存档、过渡动画、
    /// 悬浮提示（Next 按钮、Toast、Caption 朗读横幅）与 进度/设置 面板。
    /// </summary>
    public class LF_GameManager : MonoBehaviour
    {
        public static LF_GameManager I { get; private set; }

        const string K_UNLOCK = "LF_Unlock";   // 最高解锁页（Story1..Story4 索引 1..4）
        const string K_SCORE  = "LF_Score";
        const string K_SFX    = "LF_SfxOn";
        const string K_STAR   = "LF_Star";     // + 页面索引，如 LF_Star1

        public Canvas Canvas { get; private set; }
        public PageId Current { get; private set; }
        public float PageAspect { get; private set; }

        RectTransform _pageHost;
        RectTransform _overlayHost;
        GameObject _currentPage;
        Image _fade;
        Image _globalBg;

        public int SfxOn
        {
            get => PlayerPrefs.GetInt(K_SFX, 1);
            set { PlayerPrefs.SetInt(K_SFX, value); PlayerPrefs.Save(); }
        }
        public int Unlock
        {
            get => PlayerPrefs.GetInt(K_UNLOCK, 0);
            set { if (value > PlayerPrefs.GetInt(K_UNLOCK, 0)) { PlayerPrefs.SetInt(K_UNLOCK, value); PlayerPrefs.Save(); } }
        }
        public int TotalScore
        {
            get => PlayerPrefs.GetInt(K_SCORE, 0);
            set { PlayerPrefs.SetInt(K_SCORE, value); PlayerPrefs.Save(); }
        }
        public void AddScore(int n) { TotalScore += n; }
        public int GetStars(PageId p) => PlayerPrefs.GetInt(K_STAR + (int)p, 0);
        public void SetStars(PageId p, int n)
        {
            if (n > GetStars(p)) { PlayerPrefs.SetInt(K_STAR + (int)p, n); PlayerPrefs.Save(); }
        }

        void Awake()
        {
            I = this;
            // 确保场景有 AudioListener，否则所有声音都听不到
            if (FindObjectOfType<AudioListener>() == null)
                gameObject.AddComponent<AudioListener>();
            Canvas = FindObjectOfType<Canvas>();
            if (Canvas == null) { Debug.LogError("[LF] 未找到 Canvas，请先执行菜单 LittleFox → Build Scene"); return; }
            BuildGlobalBg();
            _pageHost = NewHost("PageHost", 1);
            _overlayHost = NewHost("OverlayHost", 2);
            BuildFade();
            CenterShiftHosts();
            LF_Music.Ensure(gameObject);   // 启动背景音乐
        }

        /// <summary>全屏底色：画布铺满（含横屏时的两侧留白），颜色随页面切换。</summary>
        void BuildGlobalBg()
        {
            var go = LF_Util.NewUI("GlobalBG", Canvas.transform);
            var rt = LF_Util.RectOf(go);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            rt.SetAsFirstSibling();
            _globalBg = go.AddComponent<Image>();
            _globalBg.color = LetterboxColorFor(PageId.Home);
            _globalBg.raycastTarget = false;
        }

        /// <summary>横屏窗口（画布逻辑宽度 &gt; 1080）时，把页面与浮层整体水平居中，保证完整显示。</summary>
        void CenterShiftHosts()
        {
            float canvasW = Screen.width * LF_Util.RefH / Screen.height;   // match=1 时画布逻辑宽度
            float shift = (canvasW - LF_Util.RefW) * 0.5f;
            if (shift <= 0f) return;
            _pageHost.offsetMin = new Vector2(shift, 0f);
            _pageHost.offsetMax = new Vector2(shift, 0f);
            _overlayHost.offsetMin = new Vector2(shift, 0f);
            _overlayHost.offsetMax = new Vector2(shift, 0f);
        }

        RectTransform NewHost(string name, int sibling)
        {
            var go = LF_Util.NewUI(name, Canvas.transform);
            var rt = LF_Util.RectOf(go);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            rt.SetSiblingIndex(sibling);
            return rt;
        }

        void BuildFade()
        {
            var go = LF_Util.NewUI("Fade", Canvas.transform);
            var rt = LF_Util.RectOf(go);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            rt.SetAsLastSibling();
            _fade = go.AddComponent<Image>();
            _fade.color = new Color(0f, 0f, 0f, 0f);
            _fade.raycastTarget = false;
        }

        void Start()
        {
            if (Canvas == null)
            {
                Debug.LogError("[LF] 未找到 Canvas，请先执行菜单 LittleFox → Build Scene");
                return;
            }
            ShowPage(PageId.Home, true);
        }

        // ----------------------------------------------------------------
        // 页面切换
        // ----------------------------------------------------------------
        public void ShowPage(PageId page, bool instant = false)
        {
            Current = page;
            PageAspect = AspectFor(page);
            if (_globalBg != null) _globalBg.color = LetterboxColorFor(page);
            System.Action doShow = () =>
            {
                if (_currentPage != null) Destroy(_currentPage);
                _currentPage = BuildPage(page);
                ClearOverlay();
            };
            if (instant) { doShow(); }
            else
            {
                LF_Music.Duck();
                if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);   // 防止快速切换时多个 Fade 并行打架
                _fadeRoutine = StartCoroutine(FadeTransition(doShow));
            }
        }

        Coroutine _fadeRoutine;

        IEnumerator FadeTransition(System.Action mid)
        {
            _fade.raycastTarget = true;
            try
            {
                yield return FadeTo(0.92f, 0.18f);
                try { mid(); }
                catch (System.Exception e)
                {
                    Debug.LogError("[LF] 页面构建异常（已自动淡出遮罩，请查看此报错）: " + e);
                }
                yield return FadeTo(0f, 0.22f);
            }
            finally
            {
                _fade.color = new Color(0f, 0f, 0f, 0f);   // 无论如何都保证遮罩归零
                _fade.raycastTarget = false;
                _fadeRoutine = null;
            }
            LF_Sfx.Flip();
        }

        IEnumerator FadeTo(float target, float dur)
        {
            float t = 0f;
            Color c0 = _fade.color;
            while (t < dur)
            {
                t += Time.deltaTime;
                _fade.color = Color.Lerp(c0, new Color(0f, 0f, 0f, target), Mathf.Clamp01(t / dur));
                yield return null;
            }
            _fade.color = new Color(0f, 0f, 0f, target);
        }

        static float AspectFor(PageId page)
        {
            return 1536f / 2730f;   // 六页新分镜图统一尺寸 1536×2730
        }

        static Color LetterboxColorFor(PageId page)
        {
            return new Color(0.08f, 0.10f, 0.22f);   // 夜色深蓝，与新分镜图统一
        }

        GameObject BuildPage(PageId page)
        {
            var root = LF_Util.NewUI("Page_" + page, _pageHost);
            var rt = LF_Util.RectOf(root);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            // 留白底色（画布铺满，保证任何比例下无黑边）
            LF_Util.MakeImage("Letterbox", root.transform, new Rect(0, 0, LF_Util.RefW, LF_Util.RefH),
                LetterboxColorFor(page), false);

            // 页面原画（高度撑满、不裁切）——动态读取图片实际比例，不写死
            string path = "Sprites/page_" + page.ToString().ToLower();
            Sprite sp = Resources.Load<Sprite>(path);
            if (sp == null)
            {
                Debug.LogWarning("[LF] 找不到贴图: " + path + "（请运行 LittleFox → Configure Textures）");
                sp = LF_Util.CircleSprite();
            }
            PageAspect = sp.texture.width > 0 ? (float)sp.texture.width / sp.texture.height : AspectFor(page);
            Rect fit = LF_Util.PageFitRect(PageAspect);
            var img = LF_Util.MakeImage("PageImage", root.transform, fit, sp, false);

            // 挂载页面逻辑
            LF_PageBase controller = null;
            switch (page)
            {
                case PageId.Home: controller = root.AddComponent<LF_HomePage>(); break;
                case PageId.Story1: controller = root.AddComponent<LF_Story1Page>(); break;
                case PageId.Story2: controller = root.AddComponent<LF_Story2Page>(); break;
                case PageId.Story3: controller = root.AddComponent<LF_Story3Page>(); break;
                case PageId.Story4: controller = root.AddComponent<LF_Story4Page>(); break;
                case PageId.Credits: controller = root.AddComponent<LF_CreditsPage>(); break;
            }
            if (controller != null) controller.Setup(root.transform, img.rectTransform, page, PageAspect);

            LF_DebugZones.Create(root.transform, page, PageAspect);
            return root;
        }

        // ----------------------------------------------------------------
        // 完成页 / 计分
        // ----------------------------------------------------------------
        /// <summary>页面完成：保存星级、加分、出现 Next 按钮。</summary>
        public void PageComplete(PageId page, int stars, int bonus, PageId next)
        {
            SetStars(page, stars);
            TotalScore += bonus;
            Unlock = (int)next;
            if (bonus > 0) ShowFloatingText("+" + bonus);
            ShowNextButton(next);
        }

        void ShowFloatingText(string msg)
        {
            var t = LF_Util.MakeText("ScorePop", _overlayHost, new Rect(LF_Util.RefW * 0.5f - 120f, LF_Util.RefH * 0.62f, 240f, 90f),
                msg, 56, new Color(1f, 0.85f, 0.2f));
            var cg = t.gameObject.AddComponent<CanvasGroup>();
            StartCoroutine(FloatUpAndFade(cg, t.transform));
        }

        IEnumerator FloatUpAndFade(CanvasGroup cg, Transform tr)
        {
            float t = 0f;
            Vector3 basePos = tr.localPosition;
            while (t < 1.1f)
            {
                t += Time.deltaTime;
                tr.localPosition = basePos + Vector3.up * (t * 120f);
                cg.alpha = Mathf.Clamp01(1f - t / 0.9f);
                yield return null;
            }
            Destroy(cg.gameObject);
        }

        // ----------------------------------------------------------------
        // Next 按钮
        // ----------------------------------------------------------------
        GameObject _nextButton;

        public void ShowNextButton(PageId next)
        {
            HideNextButton();
            Rect rc = new Rect(LF_Util.RefW - 190f, 160f, 130f, 130f);
            var holder = LF_Util.NewUI("Next", _overlayHost);
            LF_Util.Place(LF_Util.RectOf(holder), rc);
            var circle = LF_Util.MakeImage("circle", holder.transform, new Rect(0, 0, 130f, 130f),
                LF_Util.CircleSprite(), true);
            circle.color = new Color(1f, 0.84f, 0.25f, 0.95f);
            var b = circle.gameObject.AddComponent<Button>();
            b.targetGraphic = circle;
            PageId target = next;
            b.onClick.AddListener(() => { LF_Sfx.Tap(); ShowPage(target); });
            var arrow = LF_Util.MakeText("arrow", holder.transform, new Rect(0, 0, 130f, 130f), "▶", 54,
                new Color(0.45f, 0.25f, 0f));
            _nextButton = holder;
            StartCoroutine(Pulse(holder.transform));
        }

        public void HideNextButton()
        {
            if (_nextButton != null) { Destroy(_nextButton); _nextButton = null; }
        }

        IEnumerator Pulse(Transform tr)
        {
            float t = 0f;
            while (tr != null)
            {
                t += Time.deltaTime * 3f;
                tr.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(t));
                yield return null;
            }
        }

        // ----------------------------------------------------------------
        // Toast 与 Caption
        // ----------------------------------------------------------------
        CanvasGroup _toastGroup;

        public void Toast(string zh, string en, float dur = 1.6f)
        {
            if (_toastGroup != null) Destroy(_toastGroup.gameObject);
            Rect rc = new Rect(LF_Util.RefW * 0.5f - 340f, LF_Util.RefH * 0.76f, 680f, 150f);
            var img = LF_Util.MakeImage("Toast", _overlayHost, rc, LF_Util.MakeRoundRectSprite(85, 70, 26, new Color(0f, 0f, 0f, 0.7f)), false);
            var t1 = LF_Util.MakeText("zh", img.transform, new Rect(0, 38, 680, 70), zh, 52, Color.white);
            var t2 = LF_Util.MakeText("en", img.transform, new Rect(0, -28, 680, 55), en, 38, new Color(1f, 0.9f, 0.6f));
            _toastGroup = img.gameObject.AddComponent<CanvasGroup>();
            StartCoroutine(FadeToast(_toastGroup, dur));
        }

        IEnumerator FadeToast(CanvasGroup cg, float hold)
        {
            yield return new WaitForSeconds(hold);
            float t = 0f;
            while (t < 0.4f) { t += Time.deltaTime; cg.alpha = 1f - t / 0.4f; yield return null; }
            if (cg != null) Destroy(cg.gameObject);
        }

        CanvasGroup _captionGroup;

        public void ToggleCaption(string zh, string en)
        {
            if (_captionGroup != null) { Destroy(_captionGroup.gameObject); _captionGroup = null; return; }
            Rect rc = new Rect(LF_Util.RefW * 0.5f - 460f, 1380f, 920f, 190f);
            var img = LF_Util.MakeImage("Caption", _overlayHost, rc, LF_Util.MakeRoundRectSprite(96, 42, 18, new Color(0.02f, 0.05f, 0.15f, 0.9f)), false);
            var t1 = LF_Util.MakeText("zh", img.transform, new Rect(20, 100, 920, 80), zh, 50, Color.white);
            var t2 = LF_Util.MakeText("en", img.transform, new Rect(20, 15, 920, 65), en, 38, new Color(0.85f, 0.9f, 1f));
            _captionGroup = img.gameObject.AddComponent<CanvasGroup>();
            LF_Sfx.Chime();
        }

        // ----------------------------------------------------------------
        // 进度面板
        // ----------------------------------------------------------------
        public void OpenProgress()
        {
            LF_Sfx.Tap();
            BuildDimLayer(out GameObject dim, out RectTransform panel, "Progress");
            float ph = panel.rect.height;
            var title = LF_Util.MakeText("title", panel, new Rect(40, ph * 0.5f + 80, 880, 60),
                "Progress / 冒险进度", 34, new Color(0.5f, 0.3f, 0f), TextAnchor.MiddleLeft);
            var score = LF_Util.MakeText("score", panel, new Rect(40, ph * 0.5f + 10, 880, 54),
                "Total Score: " + TotalScore + "  总得分", 28, new Color(0.35f, 0.22f, 0.05f), TextAnchor.MiddleLeft);
            string[] names = { "Story 1 · 迷路森林", "Story 2 · 过河", "Story 3 · 黑暗洞穴", "Story 4 · 回家之路" };
            float y = ph * 0.5f - 70;
            for (int i = 0; i < 4; i++)
            {
                PageId p = (PageId)(i + 1);
                int stars = GetStars(p);
                string starStr = "";
                for (int s = 0; s < 3; s++) starStr += (s < stars) ? "★" : "☆";
                LF_Util.MakeText("row" + i, panel, new Rect(40, y, 440, 48), names[i], 26,
                    new Color(0.25f, 0.16f, 0.03f), TextAnchor.MiddleLeft);
                LF_Util.MakeText("stars" + i, panel, new Rect(500, y, 360, 48), starStr, 30,
                    new Color(1f, 0.75f, 0.15f));
                y -= 70;
            }
            var hint = LF_Util.MakeText("hint", panel, new Rect(40, -ph * 0.5f + 14, 880, 40),
                "Tap outside to close · 点击空白处关闭", 20, new Color(0.5f, 0.4f, 0.25f));
            AddCloseOnOutside(dim);
        }

        // ----------------------------------------------------------------
        // 设置面板
        // ----------------------------------------------------------------
        public void OpenSettings()
        {
            LF_Sfx.Tap();
            BuildDimLayer(out GameObject dim, out RectTransform panel, "Settings");
            float ph = panel.rect.height;
            var title = LF_Util.MakeText("title", panel, new Rect(40, ph * 0.5f + 80, 880, 60),
                "Settings / 设置", 34, new Color(0.5f, 0.3f, 0f), TextAnchor.MiddleLeft);

            GameObject soundBtnGo = null;
            UnityEngine.Events.UnityAction soundOnClick = () =>
            {
                SfxOn = (SfxOn == 0) ? 1 : 0;
                LF_Sfx.Tap();
                RefreshSoundLabel(soundBtnGo);
            };
            soundBtnGo = MakePanelButton(panel, new Rect(60, ph * 0.5f - 20, 840, 76), soundOnClick);
            RefreshSoundLabel(soundBtnGo);

            GameObject musicBtnGo = null;
            UnityEngine.Events.UnityAction musicOnClick = () =>
            {
                LF_Music.Toggle();
                LF_Sfx.Tap();
                RefreshMusicLabel(musicBtnGo);
            };
            musicBtnGo = MakePanelButton(panel, new Rect(60, ph * 0.5f - 110, 840, 76), musicOnClick);
            RefreshMusicLabel(musicBtnGo);

            GameObject resetBtnGo = MakePanelButton(panel, new Rect(60, ph * 0.5f - 200, 840, 76),
                () =>
                {
                    PlayerPrefs.DeleteKey(K_UNLOCK); PlayerPrefs.DeleteKey(K_SCORE);
                    for (int i = 1; i <= 4; i++) PlayerPrefs.DeleteKey(K_STAR + i);
                    PlayerPrefs.Save();
                    LF_Sfx.Snap();
                    Toast("Progress reset / 进度已重置", "All progress cleared", 1.8f);
                });
            LF_Util.MakeText("label", resetBtnGo.transform, new Rect(0, 0, 840, 76),
                "Reset Progress · 重置全部进度", 28, new Color(0.75f, 0.25f, 0.2f));

            var about = LF_Util.MakeText("about", panel, new Rect(40, -ph * 0.5f + 20, 880, 130),
                "The Little Fox Finds Its Way Home!\nLittle Fox · 小狐狸找家 · 绘本交互小游戏\nIllustrations by zekai Wang",
                22, new Color(0.4f, 0.3f, 0.15f));
            AddCloseOnOutside(dim);
        }

        /// <summary>设置面板里可见的黄色按钮（带圆角背景 + 热区），返回按钮根物体。</summary>
        GameObject MakePanelButton(RectTransform parent, Rect rc, UnityEngine.Events.UnityAction onClick)
        {
            var bg = LF_Util.MakeImage("bg", parent, rc,
                LF_Util.MakeRoundRectSprite(84, 76, 16, new Color(1f, 0.90f, 0.55f, 1f)), true);
            var b = bg.gameObject.AddComponent<Button>();
            b.targetGraphic = bg;
            b.onClick.AddListener(onClick);
            return bg.gameObject;
        }

        void RefreshSoundLabel(GameObject btn)
        {
            var old = btn.transform.Find("label");
            if (old != null) Destroy(old.gameObject);
            LF_Util.MakeText("label", btn.transform, new Rect(0, 0, 840, 76),
                SfxOn == 0 ? "Sound: OFF · 音效关" : "Sound: ON · 音效开", 28,
                new Color(0.3f, 0.2f, 0.05f));
        }

        void RefreshMusicLabel(GameObject btn)
        {
            var old = btn.transform.Find("label");
            if (old != null) Destroy(old.gameObject);
            LF_Util.MakeText("label", btn.transform, new Rect(0, 0, 840, 76),
                LF_Music.IsOn ? "Music: ON · 背景音乐开" : "Music: OFF · 背景音乐关", 28,
                new Color(0.3f, 0.2f, 0.05f));
        }

        void BuildDimLayer(out GameObject dim, out RectTransform panel, string panelName)
        {
            dim = LF_Util.NewUI(panelName + "Dim", _overlayHost);
            var drt = LF_Util.RectOf(dim);
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
            var dimImg = dim.AddComponent<Image>();
            dimImg.color = new Color(0f, 0f, 0f, 0.45f);
            dimImg.raycastTarget = true;

            panel = LF_Util.RectOf(LF_Util.NewUI(panelName, dim.transform));
            float pw = 960f, ph = 620f;
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(pw, ph);
            panel.anchoredPosition = Vector2.zero;
            var pImg = panel.gameObject.AddComponent<Image>();
            pImg.sprite = LF_Util.MakeRoundRectSprite(128, 96, 24, new Color(1f, 0.97f, 0.88f, 1f));
            pImg.raycastTarget = true;
            // 面板自身吃掉点击，避免点面板内部时触发 Dim 关闭
            var pBtn = panel.gameObject.AddComponent<Button>();
            pBtn.targetGraphic = pImg;

            var dimGo = dim;
            var close = LF_Util.MakeButton("close", panel, new Rect(pw * 0.5f - 104f, ph * 0.5f - 74f, 64, 64),
                () => { LF_Sfx.Tap(); Destroy(dimGo); });
            var x = LF_Util.MakeText("x", close.transform, new Rect(0, 0, 64, 64), "✕", 34, new Color(0.4f, 0.25f, 0.05f));
        }

        void AddCloseOnOutside(GameObject dim)
        {
            var b = dim.AddComponent<Button>();
            b.targetGraphic = dim.GetComponent<Image>();
            b.onClick.AddListener(() => { LF_Sfx.Tap(); Destroy(dim); });
        }

        // ----------------------------------------------------------------
        // 通用：清除页面上生成的悬浮物（页面切换时）
        // ----------------------------------------------------------------
        void ClearOverlay()
        {
            HideNextButton();
            if (_toastGroup != null) { Destroy(_toastGroup.gameObject); _toastGroup = null; }
            if (_captionGroup != null) { Destroy(_captionGroup.gameObject); _captionGroup = null; }
            for (int i = _overlayHost.childCount - 1; i >= 0; i--)
                Destroy(_overlayHost.GetChild(i).gameObject);
        }
    }
}
