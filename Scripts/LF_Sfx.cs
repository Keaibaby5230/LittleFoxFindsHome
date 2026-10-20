using UnityEngine;

namespace LittleFoxHome
{
    /// <summary>
    /// 程序化合成音效（无外部音频素材）。
    /// 所有音效由正弦/方波/噪声实时合成，尊重 GameManager 的音效开关。
    /// </summary>
    public static class LF_Sfx
    {
        static AudioSource _src;
        static AudioSource Src
        {
            get
            {
                if (_src == null)
                {
                    GameObject go = new GameObject("LF_SfxPlayer");
                    Object.DontDestroyOnLoad(go);
                    _src = go.AddComponent<AudioSource>();
                    _src.playOnAwake = false;
                    _src.spatialBlend = 0f;
                }
                return _src;
            }
        }

        static bool Enabled
        {
            get
            {
                var gm = LF_GameManager.I;
                return gm == null || gm.SfxOn != 0;
            }
        }

        static void Play(AudioClip clip, float volume = 1f, float pitch = 1f, float pan = 0f)
        {
            if (!Enabled || clip == null) return;
            Src.pitch = pitch;
            Src.panStereo = Mathf.Clamp(pan, -1f, 1f);
            Src.PlayOneShot(clip, volume);
        }

        /// <summary>强制播放提示音（不受音效开关控制），用于切换音效开关本身的确认反馈。</summary>
        public static void ForceTap()
        {
            var clip = Tone(660f, 0.08f, 0.5f, 0.004f, 18f, 0f);
            if (clip == null) return;
            Src.pitch = 1f;
            Src.panStereo = 0f;
            Src.PlayOneShot(clip, 0.7f);
        }

        // ----------------------------------------------------------------
        // 合成原语
        // ----------------------------------------------------------------
        static AudioClip Tone(float freq, float dur, float vol, float attack, float decay, float wave)
        {
            int n = Mathf.Max(1, Mathf.CeilToInt(dur * 44100f));
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / 44100f;
                float env = Mathf.Min(1f, t / Mathf.Max(0.001f, attack)) * Mathf.Exp(-t * decay);
                float s;
                if (wave < 0.5f) s = Mathf.Sin(2f * Mathf.PI * freq * t);
                else if (wave < 0.8f) s = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * freq * t));
                else s = Mathf.PerlinNoise(freq * t, 0f) * 2f - 1f;
                data[i] = s * env * vol;
            }
            var clip = AudioClip.Create("tone" + freq + "_" + dur, n, 1, 44100, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Noise(float dur, float vol, float cutoff)
        {
            int n = Mathf.Max(1, Mathf.CeilToInt(dur * 44100f));
            var data = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / 44100f;
                float env = Mathf.Exp(-t * 6f);
                float white = Random.Range(-1f, 1f);
                lp += (white - lp) * Mathf.Clamp01(cutoff / 44100f * 8f);
                data[i] = lp * env * vol;
            }
            var clip = AudioClip.Create("noise" + dur, n, 1, 44100, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ----------------------------------------------------------------
        // 对外音效接口
        // ----------------------------------------------------------------
        /// <summary>轻点 / 通用 UI 点击。</summary>
        public static void Tap() { Play(Tone(660f, 0.08f, 0.5f, 0.004f, 18f, 0f), 0.7f); }

        /// <summary>弹起 / 小跳。</summary>
        public static void Pop() { Play(Tone(520f, 0.12f, 0.55f, 0.005f, 14f, 0f), 0.8f, 1.15f); }

        /// <summary>收集成功（高频闪烁上行）。</summary>
        public static void Collect()
        {
            Play(Tone(880f, 0.12f, 0.4f, 0.004f, 12f, 0f), 0.7f);
            Play(Tone(1320f, 0.18f, 0.35f, 0.008f, 8f, 0f), 0.7f, 1.05f);
        }

        /// <summary>成功和弦（上行琶音）。</summary>
        public static void Success()
        {
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            for (int i = 0; i < notes.Length; i++)
                Play(Tone(notes[i], 0.22f, 0.3f, 0.01f, 5f, 0f), 0.8f, 1f + i * 0.02f);
        }

        /// <summary>徽章到账（大三度）。</summary>
        public static void Badge()
        {
            Play(Tone(659.25f, 0.16f, 0.4f, 0.006f, 8f, 0f), 0.8f);
            Play(Tone(987.77f, 0.24f, 0.3f, 0.01f, 5f, 0f), 0.8f, 1.03f);
        }

        /// <summary>错误 / 提示音。</summary>
        public static void Error() { Play(Tone(180f, 0.18f, 0.4f, 0.006f, 10f, 0.65f), 0.6f); }

        /// <summary>呼啸（狐狸跑动 / 拖拽）。</summary>
        public static void Whoosh() { Play(Noise(0.35f, 0.5f, 2400f), 0.55f, 1.1f); }

        /// <summary>咔哒（桥搭好）。</summary>
        public static void Snap() { Play(Tone(220f, 0.09f, 0.55f, 0.003f, 20f, 0.5f), 0.9f); Play(Tone(440f, 0.14f, 0.3f, 0.005f, 12f, 0f), 0.7f, 0.98f); }

        /// <summary>妈妈的呼唤（双音短促，带左右声像）。</summary>
        public static void MomCall(float pan)
        {
            Play(Tone(523.25f, 0.14f, 0.32f, 0.008f, 9f, 0f), 0.75f, 1f, pan);
            Play(Tone(659.25f, 0.18f, 0.28f, 0.008f, 8f, 0f), 0.75f, 1.02f, pan);
        }

        /// <summary>朗读提示音（柔和双音）。</summary>
        public static void Chime()
        {
            Play(Tone(783.99f, 0.20f, 0.22f, 0.01f, 5f, 0f), 0.6f);
            Play(Tone(1046.5f, 0.28f, 0.18f, 0.012f, 4f, 0f), 0.6f, 1.02f);
        }

        /// <summary>翻页/过渡。</summary>
        public static void Flip() { Play(Noise(0.18f, 0.3f, 5200f), 0.5f, 1.35f); }

        /// <summary>蝴蝶振翅（轻高频）。</summary>
        public static void FlutterSoft() { Play(Tone(1400f, 0.05f, 0.12f, 0.003f, 30f, 0f), 0.4f, Random.Range(0.95f, 1.1f)); }

        /// <summary>完成页面。</summary>
        public static void PageDone() { Success(); }
    }
}
