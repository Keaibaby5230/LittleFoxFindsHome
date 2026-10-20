using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LittleFoxHome
{
    /// <summary>
    /// 所有可交互热区定义。坐标为"原画归一化坐标"：x 向右、y 向下（与图片一致），
    /// 取值范围 0~1，可直接对照原画设计稿/分镜稿上的标注框。
    ///
    /// 调试：运行时按 Z 键显示/隐藏所有热区半透明色块，方便在 Game 视图里核对位置。
    /// 如果某个热区没对准画面元素，只需要修改本文件里对应的 Rect 常量。
    /// </summary>
    public static class LF_Zones
    {
        public const float NavY0 = 0.020f, NavY1 = 0.075f;   // 顶部导航栏公共高度

        // 顶部四个导航按钮：art 坐标左上原点 y 向下，按钮在画面最上方 y≈0.04~0.14
        public static Rect NavReturn   = new Rect(0.000f, 0.040f, 0.250f, 0.100f);
        public static Rect NavRead     = new Rect(0.250f, 0.040f, 0.250f, 0.100f);
        public static Rect NavSound    = new Rect(0.500f, 0.040f, 0.250f, 0.100f);
        public static Rect NavProgress = new Rect(0.750f, 0.040f, 0.250f, 0.100f);

        // ---------------- 主页 ----------------
        public static Rect HomeStart    = new Rect(0.120f, 0.380f, 0.500f, 0.130f);
        public static Rect HomeContinue = new Rect(0.100f, 0.530f, 0.240f, 0.090f);
        public static Rect HomeSettings = new Rect(0.370f, 0.530f, 0.240f, 0.090f);
        public static Vector2 HomeButterflyA = new Vector2(0.58f, 0.41f); // 图上的蓝蝴蝶
        public static float ButterflyR = 0.055f;
        public static Vector2 HomeFox = new Vector2(0.70f, 0.72f);       // 点狐狸反馈
        public static float HomeFoxR = 0.160f;

        // ---------------- 故事1：迷路森林 ----------------
        public static Rect S1Fox = new Rect(0.550f, 0.490f, 0.400f, 0.210f); // 右侧大狐狸

        // ---------------- 故事2：过河 ----------------
        public static Rect S2Fox     = new Rect(0.060f, 0.580f, 0.200f, 0.100f); // 左下角狐狸
        public static Rect S2Wood     = new Rect(0.180f, 0.765f, 0.210f, 0.045f); // 木头（点一下搭桥）
        public static Vector2 S2Stone1 = new Vector2(0.62f, 0.78f);   // 大石头
        public static Vector2 S2Stone2 = new Vector2(0.66f, 0.88f);   // 小石头
        public static float S2StoneR = 0.050f;

        // ---------------- 故事3：黑暗洞穴 ----------------
        public static Vector2[] S3Fireflies = new Vector2[]     // 5 只可收集萤火虫（对准图上发光簇）
        {
            new Vector2(0.22f, 0.35f),
            new Vector2(0.32f, 0.37f),
            new Vector2(0.40f, 0.39f),
            new Vector2(0.50f, 0.35f),
            new Vector2(0.35f, 0.45f),
        };
        public static float S3FireflyR = 0.055f;
        public static Vector2 S3Fox = new Vector2(0.25f, 0.76f);
        public static float S3FoxR = 0.100f;

        // ---------------- 故事4：回家之路 ----------------
        public static Rect S4ArrowLeft  = new Rect(0.080f, 0.295f, 0.120f, 0.060f); // ←
        public static Rect S4ArrowUp     = new Rect(0.220f, 0.295f, 0.100f, 0.060f); // ↑
        public static Rect S4ArrowRight = new Rect(0.320f, 0.295f, 0.110f, 0.060f); // →

        // ---------------- 致谢页 ----------------
        public static Rect CreditsHome    = new Rect(0.360f, 0.900f, 0.270f, 0.050f); // HOME 大字
        public static Vector2 CreditsFox   = new Vector2(0.52f, 0.50f);   // 挥手狐狸
        public static float CreditsFoxR = 0.180f;

        /// <summary>当前页面用到的热区列表（用于调试绘制）。</summary>
        public static List<Rect> ZonesFor(PageId page)
        {
            var list = new List<Rect>();
            switch (page)
            {
                case PageId.Home:
                    list.Add(HomeStart); list.Add(HomeContinue); list.Add(HomeSettings);
                    list.Add(CircleRect(HomeButterflyA, ButterflyR));
                    list.Add(CircleRect(HomeFox, HomeFoxR));
                    break;
                case PageId.Story1:
                    list.Add(NavReturn); list.Add(NavRead); list.Add(NavSound); list.Add(NavProgress);
                    list.Add(S1Fox);
                    break;
                case PageId.Story2:
                    list.Add(NavReturn); list.Add(NavRead); list.Add(NavSound); list.Add(NavProgress);
                    list.Add(S2Fox); list.Add(S2Wood);
                    list.Add(CircleRect(S2Stone1, S2StoneR)); list.Add(CircleRect(S2Stone2, S2StoneR));
                    break;
                case PageId.Story3:
                    list.Add(NavReturn); list.Add(NavRead); list.Add(NavSound); list.Add(NavProgress);
                    foreach (var f in S3Fireflies) list.Add(CircleRect(f, S3FireflyR));
                    list.Add(CircleRect(S3Fox, S3FoxR));
                    break;
                case PageId.Story4:
                    list.Add(NavReturn); list.Add(NavRead); list.Add(NavSound); list.Add(NavProgress);
                    list.Add(S4ArrowLeft); list.Add(S4ArrowUp); list.Add(S4ArrowRight);
                    break;
                case PageId.Credits:
                    list.Add(CreditsHome);
                    list.Add(CircleRect(CreditsFox, CreditsFoxR));
                    break;
            }
            return list;
        }

        static Rect CircleRect(Vector2 c, float r)
        {
            return new Rect(c.x - r, c.y - r, r * 2f, r * 2f);
        }
    }

    /// <summary>
    /// 调试热区绘制器：为指定页面画出半透明色块，按 Z 键开关。
    /// 由 PageBase 调用。
    /// </summary>
    public class LF_DebugZones : MonoBehaviour
    {
        PageId _page;
        float _aspect;
        bool _visible;
        readonly List<Image> _quads = new List<Image>();

        public static LF_DebugZones Create(Transform parent, PageId page, float aspect)
        {
            var go = new GameObject("DebugZones");
            go.transform.SetParent(parent, false);
            var d = go.AddComponent<LF_DebugZones>();
            d._page = page;
            d._aspect = aspect;
            return d;
        }

        void Start()
        {
            _visible = false;   // 默认隐藏，按 Z 键显示/隐藏
            if (_visible) Rebuild();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                _visible = !_visible;
                if (_visible && _quads.Count == 0) Rebuild();
                foreach (var q in _quads) if (q != null) q.enabled = _visible;
            }
        }

        void Rebuild()
        {
            var zones = LF_Zones.ZonesFor(_page);
            Color col = Color.cyan;
            for (int i = 0; i < zones.Count; i++)
            {
                Rect art = zones[i];
                Rect rc = LF_Util.ArtRectToCanvas(art, _aspect);
                float w = Mathf.Max(2f, rc.width);
                float h = Mathf.Max(2f, rc.height);
                rc = new Rect(rc.x - (w - rc.width) * 0.5f, rc.y - (h - rc.height) * 0.5f, w, h);
                var img = LF_Util.MakeImage("zone" + i, transform, rc, new Color(col.r, col.g, col.b, 0.30f), false);
                img.transform.SetSiblingIndex(1);
                _quads.Add(img);
                if (i % 3 == 2) col = Color.yellow; else col = Color.cyan;
            }
        }
    }
}
