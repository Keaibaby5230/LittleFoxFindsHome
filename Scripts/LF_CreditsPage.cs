using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LittleFoxHome
{
    /// <summary>
    /// 致谢页：点图底部 HOME 大字回主页；点挥手狐狸有告别反馈。
    /// </summary>
    public class LF_CreditsPage : LF_PageBase
    {
        protected override void OnSetup()
        {
            EnableNav();   // 顶部 return/read/sound/progress

            AddZoneButton(LF_Zones.CreditsHome, () =>
            {
                LF_Sfx.Tap();
                LF_GameManager.I.ShowPage(PageId.Home);
            });

            AddCircleZone(LF_Zones.CreditsFox, LF_Zones.CreditsFoxR, () =>
            {
                LF_Sfx.Success();
                LF_GameManager.I.Toast("Bye-bye! 下次再见！", "Thank you for playing! ❤", 2.0f);
            });

            AddDecorations();
        }

        // 星空花瓣飘落（程序小物，不挡画面元素）
        void AddDecorations()
        {
            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                const float pw = 30f, ph = 22f;
                float x = 0.08f + idx * 0.26f + Random.value * 0.06f;
                float y0 = -0.06f - Random.value * 0.06f;
                var img = LF_Util.MakeImage("Petal" + idx, Root,
                    new Rect(ArtToCanvas(new Vector2(x, y0)).x - pw * 0.5f,
                             ArtToCanvas(new Vector2(x, y0)).y - ph * 0.5f, pw, ph),
                    LF_Util.MakePetalSprite((int)pw, (int)ph), false);
                img.raycastTarget = false;
                StartCoroutine(PetalLoop(img.rectTransform, idx));
            }
        }

        IEnumerator PetalLoop(RectTransform rt, int idx)
        {
            if (rt == null) yield break;
            float dur = 3.4f + Random.value * 1.4f;
            while (rt != null && this != null && enabled)
            {
                float x = 0.08f + idx * 0.26f + Random.value * 0.06f;
                Vector2 start = ArtToCanvas(new Vector2(x, -0.06f - Random.value * 0.06f));
                Vector2 end = ArtToCanvas(new Vector2(x + (Random.value - 0.5f) * 0.10f, 1.04f + Random.value * 0.06f));
                rt.anchoredPosition = start;
                yield return StartCoroutine(FallPetal(rt, start, end, dur, null));
            }
        }

        protected override KeyValuePair<string, string> StoryText()
        {
            return new KeyValuePair<string, string>(
                "小狐狸终于回到家啦！感谢你的陪伴！",
                "The little fox is home! Thanks for playing!");
        }
    }
}
