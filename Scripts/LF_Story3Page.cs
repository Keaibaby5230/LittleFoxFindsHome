using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LittleFoxHome
{
    /// <summary>
    /// 故事3 · 黑暗洞穴：点图上发光的萤火虫，收集满 5 只照亮前路。
    /// </summary>
    public class LF_Story3Page : LF_PageBase
    {
        bool _done;
        int _collected;

        protected override void OnSetup()
        {
            EnableNav();

            for (int i = 0; i < LF_Zones.S3Fireflies.Length; i++)
            {
                int idx = i;
                AddCircleZone(LF_Zones.S3Fireflies[i], LF_Zones.S3FireflyR, () => OnFireflyTapped(idx));
            }

            // 可点击小装饰：洞顶星星
            AddClickyStar(new Vector2(0.15f, 0.20f));
            AddClickyStar(new Vector2(0.50f, 0.15f));
            AddClickyStar(new Vector2(0.85f, 0.20f));
        }

        void OnFireflyTapped(int idx)
        {
            if (_done) return;
            LF_Sfx.Pop();
            _collected++;
            SpawnBurst(LF_Zones.S3Fireflies[idx]);

            if (_collected >= LF_Zones.S3Fireflies.Length)
            {
                StartCoroutine(Finish());
            }
            else
            {
                LF_GameManager.I.Toast(_collected + " / 5 fireflies! 收集了 " + _collected + " 只！",
                    "Keep tapping the glowing lights!", 1.2f);
            }
        }

        void SpawnBurst(Vector2 art)
        {
            Vector2 c = ArtToCanvas(art);
            const float size = 90f;
            var img = LF_Util.MakeImage("burst", Root,
                new Rect(c.x - size * 0.5f, c.y - size * 0.5f, size, size),
                LF_Util.CircleSprite(), false);
            img.color = new Color(1f, 0.95f, 0.5f, 0.85f);
            StartCoroutine(FadeBurst(img));
        }

        IEnumerator FadeBurst(Image img)
        {
            float t = 0f;
            while (t < 0.8f && img != null)
            {
                t += Time.deltaTime;
                var c = img.color; c.a = 0.85f * (1f - t / 0.8f); img.color = c;
                img.rectTransform.localScale = Vector3.one * (1f + t * 0.8f);
                yield return null;
            }
            if (img != null) Destroy(img.gameObject);
        }

        IEnumerator Finish()
        {
            _done = true;
            LF_Sfx.Chime();
            LF_GameManager.I.Toast("All 5 fireflies collected! 5 只萤火虫都收集到啦！",
                "The cave is bright now — off you go!", 1.8f);
            yield return new WaitForSeconds(0.6f);
            LF_Sfx.Success();
            LF_GameManager.I.PageComplete(Page, 3, 25, PageId.Story4);
        }

        protected override KeyValuePair<string, string> StoryText()
        {
            return new KeyValuePair<string, string>(
                "洞穴里一片漆黑。点一点发光的萤火虫，收集 5 只照亮出路！",
                "The cave is dark. Tap the glowing fireflies to collect all 5 and light the way out!");
        }
    }
}
