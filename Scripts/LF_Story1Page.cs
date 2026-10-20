using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LittleFoxHome
{
    /// <summary>
    /// 故事1 · 迷路森林：点击右侧小狐狸，帮它站起来，页面轻跳庆祝。
    /// </summary>
    public class LF_Story1Page : LF_PageBase
    {
        bool _done;

        protected override void OnSetup()
        {
            EnableNav();

            AddZoneButton(LF_Zones.S1Fox, () =>
            {
                if (_done) return;
                _done = true;
                LF_Sfx.Pop();
                StartCoroutine(FoxHop());
            });

            // 可点击小装饰：星星 + 花朵
            AddClickyStar(new Vector2(0.15f, 0.30f));
            AddClickyStar(new Vector2(0.85f, 0.25f));
            AddClickyFlower(new Vector2(0.20f, 0.75f));
            AddClickyFlower(new Vector2(0.80f, 0.80f));

            // 点不同位置的蛀牙虫有不同反应
            AddZoneButton(new Rect(0.20f, 0.40f, 0.15f, 0.12f), () =>
            {
                LF_Sfx.Pop();
                LF_GameManager.I.Toast("Eek! 蛀牙虫被你点到了！", "Sugar bug squashed!", 1.2f);
            });
            AddZoneButton(new Rect(0.70f, 0.45f, 0.15f, 0.12f), () =>
            {
                LF_Sfx.Pop();
                LF_GameManager.I.Toast("Another one! 又赶走一只！", "Keep brushing!", 1.2f);
            });
        }

        IEnumerator FoxHop()
        {
            // 整页轻跳庆祝（狐狸本身已在画稿里，不再叠加程序狐狸）
            yield return StartCoroutine(HopPage(0.55f));
            LF_Sfx.Success();
            LF_GameManager.I.Toast("The fox stands up! 小狐狸站起来啦！", "Now follow the path deeper into the forest.", 1.8f);
            yield return new WaitForSeconds(0.35f);
            LF_GameManager.I.PageComplete(Page, 3, 15, PageId.Story2);
        }

        IEnumerator HopPage(float dur)
        {
            float t = 0f;
            Vector2 basePos = PageImage.anchoredPosition;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.Sin(Mathf.Clamp01(t / dur) * Mathf.PI);
                PageImage.anchoredPosition = basePos + Vector2.up * k * 40f;
                PageImage.localScale = Vector3.one * (1f + k * 0.05f);
                yield return null;
            }
            PageImage.anchoredPosition = basePos;
            PageImage.localScale = Vector3.one;
        }

        protected override KeyValuePair<string, string> StoryText()
        {
            return new KeyValuePair<string, string>(
                "夜晚的森林里，小狐狸迷路了。点一点它，帮它站起来吧！",
                "The little fox is lost in the night forest. Tap the fox to help it stand up!");
        }
    }
}
