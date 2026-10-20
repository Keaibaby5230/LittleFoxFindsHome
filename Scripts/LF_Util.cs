using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LittleFoxHome
{
    /// <summary>
    /// 通用工具：画布坐标换算、字体回退、运行时生成贴图/精灵、简易动画辅助。
    /// 所有“画布单位”均基于参考分辨率 1080x1920（竖屏）。
    /// </summary>
    public static class LF_Util
    {
        public const float RefW = 1080f;
        public const float RefH = 1920f;

        // ------------------------------------------------------------------
        // 页面显示：原画按“高度撑满”的方式放入画布，两侧留白用页面底色填充，
        // 不做任何裁切。pageAspect = 原图宽 / 原图高（必须写死真实值，
        // 不能读纹理尺寸——Unity 会把非 2 的幂纹理缩放，读到的宽高不可靠）。
        // ------------------------------------------------------------------
        public static Rect PageFitRect(float pageAspect)
        {
            float imgW = RefH * pageAspect;
            float x0 = (RefW - imgW) * 0.5f;
            return new Rect(x0, 0f, imgW, RefH);
        }

        /// <summary>原画归一化坐标（左上原点，y 向下）→ 画布坐标（左下原点，y 向上）。</summary>
        public static Vector2 ArtToCanvas(Vector2 art, float pageAspect)
        {
            Rect r = PageFitRect(pageAspect);
            float x = r.x + art.x * r.width;
            float y = RefH * (1f - art.y);
            return new Vector2(x, y);
        }

        /// <summary>原画归一化矩形 → 画布矩形（Rect: x,y 左下，w,h）。</summary>
        public static Rect ArtRectToCanvas(Rect art, float pageAspect)
        {
            Vector2 bl = ArtToCanvas(new Vector2(art.xMin, art.yMax), pageAspect);
            Vector2 tr = ArtToCanvas(new Vector2(art.xMax, art.yMin), pageAspect);
            return Rect.MinMaxRect(bl.x, bl.y, tr.x, tr.y);
        }

        // ------------------------------------------------------------------
        // UI 工厂
        // ------------------------------------------------------------------
        public static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        public static RectTransform RectOf(GameObject go)
        {
            return go.GetComponent<RectTransform>();
        }

        /// <summary>放置一个以左下角为锚点的矩形。</summary>
        public static void Place(RectTransform rt, Rect rect)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(rect.x, rect.y);
            rt.sizeDelta = new Vector2(rect.width, rect.height);
        }

        public static Image MakeImage(string name, Transform parent, Rect rect, Color color, bool raycast)
        {
            GameObject go = NewUI(name, parent);
            Image img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            Place(RectOf(go), rect);
            return img;
        }

        public static Image MakeImage(string name, Transform parent, Rect rect, Sprite sprite, bool raycast)
        {
            GameObject go = NewUI(name, parent);
            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.raycastTarget = raycast;
            Place(RectOf(go), rect);
            return img;
        }

        public static Text MakeText(string name, Transform parent, Rect rect, string content,
            int size, Color color, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            GameObject go = NewUI(name, parent);
            Text t = go.AddComponent<Text>();
            t.font = FontForUI();
            t.text = content;
            t.fontSize = size;
            t.fontStyle = FontStyle.Bold;
            t.color = color;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            Place(RectOf(go), rect);
            return t;
        }

        /// <summary>按钮：透明底 + 点击回调（UI Button 事件）。</summary>
        public static Button MakeButton(string name, Transform parent, Rect rect, UnityEngine.Events.UnityAction onClick)
        {
            GameObject go = NewUI(name, parent);
            Image img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.01f);
            img.raycastTarget = true;
            Button b = go.AddComponent<Button>();
            b.targetGraphic = img;
            if (onClick != null) b.onClick.AddListener(onClick);
            Place(RectOf(go), rect);
            return b;
        }

        // ------------------------------------------------------------------
        // 字体回退：优先内置 LegacyRuntime，其次 Arial，最后尝试系统中文黑体。
        // ------------------------------------------------------------------
        static Font _font;
        public static Font FontForUI()
        {
            if (_font != null) return _font;

            var candidates = new List<string> { "LegacyRuntime.ttf", "Arial.ttf" };
            foreach (string name in candidates)
            {
                Font f = Resources.GetBuiltinResource<Font>(name);
                if (f != null) { _font = f; return f; }
            }
            foreach (string os in new[] { "Microsoft YaHei", "PingFang SC", "SimHei", "Arial" })
            {
                try { Font f = Font.CreateDynamicFontFromOSFont(os, 24); if (f != null) { _font = f; return f; } } catch { }
            }
            _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return _font;
        }

        // ------------------------------------------------------------------
        // 运行时生成圆形 / 圆环 / 圆角矩形贴图（用于进度圆点、徽章、蝙蝠等）
        // ------------------------------------------------------------------
        static Sprite _circleSprite;
        public static Sprite CircleSprite()
        {
            if (_circleSprite != null) return _circleSprite;
            _circleSprite = MakeCircleSprite(64, Color.white);
            return _circleSprite;
        }

        /// <summary>加载 Resources/Sprites 下的装饰图案（原画裁剪素材）。</summary>
        public static Sprite LoadDecoSprite(string name)
        {
            var sp = Resources.Load<Sprite>("Sprites/" + name);
            if (sp != null) return sp;
            var tex = Resources.Load<Texture2D>("Sprites/" + name);
            if (tex != null)
            {
                try
                {
                    tex.wrapMode = TextureWrapMode.Clamp;
                    tex.filterMode = FilterMode.Bilinear;
                    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
                catch (System.Exception e)
                {
                    // 纹理不可读（Read/Write 未开启）时 Sprite.Create 会抛异常，跳过该装饰而不是让页面构建失败。
                    Debug.LogWarning("[LF] 装饰图案不可读，跳过: " + name + "（请运行 LittleFox → Configure Textures 开启 Read/Write）\n" + e.Message);
                    return null;
                }
            }
            return null;
        }

        public static Sprite MakeCircleSprite(int size, Color color)
        {
            var tex = NewClearTexture(size, size);
            float c = (size - 1) * 0.5f;
            float r = c - 1f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - c, dy = y - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(r - d + 1f);
                tex.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * a));
            }
            BleedEdges(tex);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        static Sprite _ringSprite;
        /// <summary>圆环描边精灵（空心高亮圈，不遮挡原画内容）。</summary>
        public static Sprite RingSprite()
        {
            if (_ringSprite != null) return _ringSprite;
            _ringSprite = MakeRingSprite(64, 0.20f, Color.white);
            return _ringSprite;
        }

        /// <summary>生成圆环贴图：中间透明、只有一圈描边（高亮提示用）。</summary>
        public static Sprite MakeRingSprite(int size, float thickness, Color color)
        {
            var tex = NewClearTexture(size, size);
            float c = (size - 1) * 0.5f;
            float rOuter = c - 1f;
            float rInner = Mathf.Max(1f, rOuter * (1f - thickness));
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - c, dy = y - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(rOuter - d + 1f) * Mathf.Clamp01(d - rInner + 1f);
                tex.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * a));
            }
            BleedEdges(tex);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        /// <summary>生成蝙蝠剪影贴图：身体 + 展开双翅 + 小耳朵，带一圈亮色描边（夜空中更醒目）。</summary>
        public static Sprite MakeBatSprite(int w, int h, Color body, Color edge)
        {
            var tex = NewClearTexture(w, h);
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.56f;
            float rx = w * 0.16f, ry = h * 0.34f;

            Vector2[] lWing = {
                new Vector2(cx - rx * 1.00f, cy - ry * 0.40f),
                new Vector2(cx * 0.55f,       cy - ry * 1.00f),
                new Vector2(w * 0.02f,        cy - ry * 0.60f),
                new Vector2(w * 0.02f,        cy + ry * 0.60f),
                new Vector2(cx * 0.26f,       cy + ry * 1.10f),
                new Vector2(cx * 0.44f,       cy + ry * 0.50f),
                new Vector2(cx * 0.64f,       cy + ry * 1.00f),
                new Vector2(cx - rx * 0.85f,  cy + ry * 0.30f),
            };
            Vector2[] rWing = Mirror(lWing, cx);
            Vector2[] lEar = {
                new Vector2(cx - rx * 0.90f, cy - ry * 0.70f),
                new Vector2(cx - rx * 0.42f, cy - ry * 1.45f),
                new Vector2(cx - rx * 0.02f, cy - ry * 0.75f),
            };
            Vector2[] rEar = Mirror(lEar, cx);

            Vector2[] Scaled(Vector2[] p, float s)
            {
                var r = new Vector2[p.Length];
                for (int i = 0; i < p.Length; i++)
                    r[i] = new Vector2((p[i].x - cx) * s + cx, (p[i].y - cy) * s + cy);
                return r;
            }
            bool InBat(float px, float py, Vector2[] lw, Vector2[] rw, Vector2[] le, Vector2[] re, float rxs, float rys)
            {
                float dx = px - cx, dy = py - cy;
                if (dx * dx / (rxs * rxs) + dy * dy / (rys * rys) <= 1f) return true;
                return InPoly(px, py, lw) || InPoly(px, py, rw) || InPoly(px, py, le) || InPoly(px, py, re);
            }
            var lw1 = Scaled(lWing, 1f);    var rw1 = Scaled(rWing, 1f);
            var le1 = Scaled(lEar, 1f);     var re1 = Scaled(rEar, 1f);
            var lw2 = Scaled(lWing, 1.12f); var rw2 = Scaled(rWing, 1.12f);
            var le2 = Scaled(lEar, 1.12f);  var re2 = Scaled(rEar, 1.12f);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (InBat(x, y, lw1, rw1, le1, re1, rx, ry))
                    tex.SetPixel(x, y, body);
                else if (InBat(x, y, lw2, rw2, le2, re2, rx * 1.12f, ry * 1.12f))
                    tex.SetPixel(x, y, edge);
            }
            BleedEdges(tex);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        /// <summary>水平镜像多边形。</summary>
        static Vector2[] Mirror(Vector2[] poly, float cx)
        {
            var r = new Vector2[poly.Length];
            for (int i = 0; i < poly.Length; i++)
                r[i] = new Vector2(2f * cx - poly[i].x, poly[i].y);
            return r;
        }

        /// <summary>射线法判断点是否在多边形内。</summary>
        static bool InPoly(float px, float py, Vector2[] poly)
        {
            bool inside = false;
            int n = poly.Length;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float xi = poly[i].x, yi = poly[i].y;
                float xj = poly[j].x, yj = poly[j].y;
                if (((yi > py) != (yj > py)) && (px < (xj - xi) * (py - yi) / (yj - yi) + xi))
                    inside = !inside;
            }
            return inside;
        }

        public static Sprite MakeRoundRectSprite(int w, int h, int radius, Color color)
        {
            var tex = NewClearTexture(w, h);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float a = 1f;
                float dx = 0f, dy = 0f;
                if (x < radius) dx = radius - x; else if (x > w - radius - 1) dx = x - (w - radius - 1);
                if (y < radius) dy = radius - y; else if (y > h - radius - 1) dy = y - (h - radius - 1);
                if (dx > 0f || dy > 0f)
                {
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    a = Mathf.Clamp01(radius - d + 1f);
                }
                tex.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * a));
            }
            BleedEdges(tex);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        // ------------------------------------------------------------------
        // 程序绘制：弹跳狐狸 + 页面装饰（草 / 蝴蝶 / 小虫）
        // ------------------------------------------------------------------
        /// <summary>
        /// 创建全透明纹理：显式把所有像素清零为 (0,0,0,0)。
        /// 部分平台/构建下 new Texture2D 未初始化像素是不透明白色，会导致图案外出现白方块。
        /// </summary>
        static Texture2D NewClearTexture(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var clear = new Color[w * h];   // Color 默认 (0,0,0,0)
            tex.SetPixels(clear);
            return tex;
        }

        static void FillPoly(Texture2D tex, Vector2[] poly, Color c)
        {
            int w = tex.width, h = tex.height;
            float minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < poly.Length; i++)
            {
                minY = Mathf.Min(minY, poly[i].y);
                maxY = Mathf.Max(maxY, poly[i].y);
            }
            int y0 = Mathf.Max(0, Mathf.FloorToInt(minY)), y1 = Mathf.Min(h - 1, Mathf.CeilToInt(maxY));
            for (int y = y0; y <= y1; y++)
                for (int x = 0; x < w; x++)
                    if (InPoly(x + 0.5f, y + 0.5f, poly)) tex.SetPixel(x, y, c);
        }

        static void FillEllipse(Texture2D tex, float cx, float cy, float rx, float ry, Color c)
        {
            int w = tex.width, h = tex.height;
            int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - ry)), y1 = Mathf.Min(h - 1, Mathf.CeilToInt(cy + ry));
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - rx)), x1 = Mathf.Min(w - 1, Mathf.CeilToInt(cx + rx));
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                    if (dx * dx + dy * dy <= 1f) tex.SetPixel(x, y, c);
                }
        }

        static Vector2[] Grow(Vector2[] poly, float s)
        {
            float cx = 0f, cy = 0f;
            for (int i = 0; i < poly.Length; i++) { cx += poly[i].x; cy += poly[i].y; }
            cx /= poly.Length; cy /= poly.Length;
            var r = new Vector2[poly.Length];
            for (int i = 0; i < poly.Length; i++)
                r[i] = new Vector2((poly[i].x - cx) * s + cx, (poly[i].y - cy) * s + cy);
            return r;
        }

        /// <summary>
        /// 边缘去色晕：把透明像素的 RGB 填成相邻图案的颜色（alpha 保持 0）。
        /// 这样 Bilinear 过滤时边缘只产生 alpha 渐变，不会和透明背景的黑色/白色混合出“白边、黑边”。
        /// 所有程序绘制的贴图在 Apply 前都应调用。
        /// </summary>
        static void BleedEdges(Texture2D tex)
        {
            int w = tex.width, h = tex.height;
            var cols = tex.GetPixels();
            for (int pass = 0; pass < 2; pass++)
            {
                var next = (Color[])cols.Clone();
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    if (cols[i].a > 0.01f) continue;
                    Color c = Color.clear;
                    if (x > 0 && cols[i - 1].a > 0.01f) c = cols[i - 1];
                    else if (x < w - 1 && cols[i + 1].a > 0.01f) c = cols[i + 1];
                    else if (y > 0 && cols[i - w].a > 0.01f) c = cols[i - w];
                    else if (y < h - 1 && cols[i + w].a > 0.01f) c = cols[i + w];
                    if (c.a > 0.01f) next[i] = new Color(c.r, c.g, c.b, 0f);
                }
                cols = next;
            }
            tex.SetPixels(cols);
        }

        /// <summary>程序绘制：侧立红狐狸（面向右，儿童绘本风），带深棕描边。</summary>
        public static Sprite MakeFoxSprite(int w, int h)
        {
            var tex = NewClearTexture(w, h);
            Color red = new Color(0.84f, 0.30f, 0.20f);
            Color orange = new Color(0.95f, 0.58f, 0.20f);
            Color dark = new Color(0.45f, 0.22f, 0.12f);
            Color white = new Color(0.99f, 0.98f, 0.94f);
            Color black = new Color(0.16f, 0.10f, 0.08f);

            Vector2[] lEar = { new Vector2(0.70f * w, 0.92f * h), new Vector2(0.57f * w, 0.60f * h), new Vector2(0.86f * w, 0.78f * h) };
            Vector2[] rEar = { new Vector2(0.94f * w, 0.92f * h), new Vector2(1.02f * w, 0.62f * h), new Vector2(0.86f * w, 0.78f * h) };
            Vector2[] lEarIn = { new Vector2(0.71f * w, 0.87f * h), new Vector2(0.63f * w, 0.68f * h), new Vector2(0.82f * w, 0.79f * h) };
            Vector2[] rEarIn = { new Vector2(0.92f * w, 0.87f * h), new Vector2(0.98f * w, 0.69f * h), new Vector2(0.86f * w, 0.79f * h) };

            // 描边 pass
            FillPoly(tex, Grow(lEar, 1.10f), dark);
            FillPoly(tex, Grow(rEar, 1.10f), dark);
            FillEllipse(tex, 0.20f * w, 0.60f * h, 0.24f * w * 1.08f, 0.13f * h * 1.08f, dark);   // 尾
            FillEllipse(tex, 0.52f * w, 0.46f * h, 0.28f * w * 1.08f, 0.30f * h * 1.08f, dark);   // 身
            FillEllipse(tex, 0.82f * w, 0.76f * h, 0.17f * w * 1.08f, 0.17f * w * 1.08f, dark);   // 头
            FillEllipse(tex, 0.72f * w, 0.22f * h, 0.09f * w, 0.13f * h, dark);                   // 前腿
            FillEllipse(tex, 0.33f * w, 0.22f * h, 0.10f * w, 0.14f * h, dark);                   // 后腿

            // 本体 pass
            FillPoly(tex, lEar, red);
            FillPoly(tex, rEar, red);
            FillEllipse(tex, 0.20f * w, 0.60f * h, 0.24f * w, 0.13f * h, orange);
            FillEllipse(tex, 0.52f * w, 0.46f * h, 0.28f * w, 0.30f * h, red);
            FillEllipse(tex, 0.82f * w, 0.76f * h, 0.17f * w, 0.17f * w, red);
            FillEllipse(tex, 0.72f * w, 0.22f * h, 0.09f * w, 0.13f * h, red);
            FillEllipse(tex, 0.33f * w, 0.22f * h, 0.10f * w, 0.14f * h, red);
            FillPoly(tex, lEarIn, orange);
            FillPoly(tex, rEarIn, orange);
            FillEllipse(tex, 0.55f * w, 0.42f * h, 0.14f * w, 0.18f * h, white);                  // 白肚皮
            FillEllipse(tex, 0.95f * w, 0.72f * h, 0.065f * w, 0.055f * h, white);                // 吻部
            FillEllipse(tex, 0.985f * w, 0.72f * h, 0.03f * w, 0.03f * w, black);                 // 鼻
            FillEllipse(tex, 0.87f * w, 0.80f * h, 0.035f * w, 0.035f * w, black);                // 眼

            BleedEdges(tex);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        /// <summary>程序绘制：五瓣小花（柔和蜡笔风，无描边融入背景）。</summary>
        public static Sprite MakeFlowerSprite(int w, int h)
        {
            var tex = NewClearTexture(w, h);
            Color petal = new Color(0.95f, 0.55f, 0.62f);
            Color heart = new Color(0.98f, 0.82f, 0.32f);
            float cx = 0.5f * w, cy = 0.55f * h, dist = 0.17f * w;
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72f * Mathf.Deg2Rad;
                FillEllipse(tex, cx + dist * Mathf.Cos(a), cy + dist * Mathf.Sin(a),
                    0.17f * w, 0.21f * h, petal);
            }
            FillEllipse(tex, cx, cy, 0.10f * w, 0.10f * w, heart);
            BleedEdges(tex);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        /// <summary>程序绘制：小蘑菇（红顶白点，无描边融入背景）。</summary>
        public static Sprite MakeMushroomSprite(int w, int h)
        {
            var tex = NewClearTexture(w, h);
            Color red = new Color(0.86f, 0.30f, 0.26f);
            Color cream = new Color(0.97f, 0.93f, 0.84f);
            Color white = new Color(1f, 1f, 1f, 0.96f);
            // 柄（先画，顶盖住上部）
            FillEllipse(tex, 0.50f * w, 0.74f * h, 0.17f * w, 0.24f * h, cream);
            // 顶盖
            FillEllipse(tex, 0.50f * w, 0.36f * h, 0.36f * w, 0.27f * h, red);
            // 白点
            FillEllipse(tex, 0.36f * w, 0.30f * h, 0.055f * w, 0.075f * h, white);
            FillEllipse(tex, 0.58f * w, 0.24f * h, 0.045f * w, 0.06f * h, white);
            FillEllipse(tex, 0.64f * w, 0.42f * h, 0.05f * w, 0.068f * h, white);
            BleedEdges(tex);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        /// <summary>程序绘制：小圆石（暖灰，无描边融入背景）。</summary>
        public static Sprite MakeStoneSprite(int w, int h)
        {
            var tex = NewClearTexture(w, h);
            Color stone = new Color(0.78f, 0.76f, 0.70f);
            Color lite = new Color(0.93f, 0.92f, 0.88f);
            FillEllipse(tex, 0.5f * w, 0.56f * h, 0.42f * w, 0.30f * h, stone);
            FillEllipse(tex, 0.38f * w, 0.44f * h, 0.13f * w, 0.08f * h, lite);
            BleedEdges(tex);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        /// <summary>程序绘制：粉色小花瓣（飘落装饰用，无描边）。</summary>
        public static Sprite MakePetalSprite(int w, int h)
        {
            var tex = NewClearTexture(w, h);
            Color petal = new Color(0.97f, 0.62f, 0.70f);
            FillEllipse(tex, 0.5f * w, 0.5f * h, 0.44f * w, 0.30f * h, petal);
            BleedEdges(tex);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }
    }
}
