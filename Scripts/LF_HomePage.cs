using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LittleFoxHome
{
    /// <summary>
    /// 主页：START / Continue / Settings 三个按钮；点图上的蝴蝶和小狐狸有可爱反馈。
    /// </summary>
    public class LF_HomePage : LF_PageBase
    {
        protected override void OnSetup()
        {
            EnableNav();   // 顶部 read / sound / progress 可用；return 在主页无操作（fade 回主页）

            // 先放装饰性小热区（蝴蝶、狐狸），最后放 START 主按钮，保证 START 在最上层不被遮挡
            AddZoneButton(LF_Zones.HomeSettings, () => LF_GameManager.I.OpenSettings());

            AddZoneButton(LF_Zones.HomeContinue, () =>
            {
                LF_Sfx.Tap();
                PageId next = PageId.Home;
                bool found = false;
                for (int i = 1; i <= 4; i++)
                {
                    if (LF_GameManager.I.GetStars((PageId)i) == 0)
                    {
                        next = (PageId)i;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    LF_GameManager.I.Toast("All done! 全部完成啦！", "Every story is starred — enjoy again!", 2f);
                    return;
                }
                LF_GameManager.I.ShowPage(next);
            });

            // 图上蓝蝴蝶（小热区，不挡按钮）
            AddZoneButton(new Rect(0.52f, 0.36f, 0.13f, 0.10f), () =>
            {
                LF_Sfx.FlutterSoft();
                LF_GameManager.I.Toast("A friendly butterfly! 一只友善的蝴蝶！",
                    "It will guide you through the forest.", 1.6f);
            });

            // 小狐狸身体范围（右下，远离 START）
            AddZoneButton(new Rect(0.55f, 0.58f, 0.40f, 0.30f), () =>
            {
                LF_Sfx.Badge();
                StartCoroutine(HopPage(0.45f));
            });

            // START 放最后 = 最上层
            AddZoneButton(LF_Zones.HomeStart, () =>
            {
                LF_Sfx.Tap();
                LF_GameManager.I.ShowPage(PageId.Story1);
            });

            // 额外互动点
            AddClickyStar(new Vector2(0.15f, 0.15f));
            AddClickyStar(new Vector2(0.85f, 0.15f));
            AddClickyFlower(new Vector2(0.10f, 0.85f));
            AddClickyFlower(new Vector2(0.90f, 0.85f));
        }

        IEnumerator HopPage(float dur)
        {
            float t = 0f;
            Vector2 basePos = PageImage.anchoredPosition;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.Sin(Mathf.Clamp01(t / dur) * Mathf.PI);
                PageImage.anchoredPosition = basePos + Vector2.up * k * 36f;
                yield return null;
            }
            PageImage.anchoredPosition = basePos;
        }
    }
}
