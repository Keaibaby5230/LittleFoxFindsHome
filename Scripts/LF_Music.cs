using UnityEngine;

namespace LittleFoxHome
{
    /// <summary>
    /// 程序化背景音乐：温馨摇篮曲风格循环，无需外部音频文件。
    /// </summary>
    public static class LF_Music
    {
        static AudioSource _src;
        static bool _on = true;

        public static bool IsOn => _on;

        public static void Ensure(GameObject host)
        {
            if (_src != null) return;
            _src = host.AddComponent<AudioSource>();
            _src.playOnAwake = false;
            _src.loop = true;
            _src.volume = 0.5f;
            _src.clip = BuildLullaby();
            _src.Play();
        }

        public static void Toggle()
        {
            _on = !_on;
            if (_src == null) return;
            if (_on) { _src.Play(); } else { _src.Pause(); }
        }

        public static void Duck()
        {
            if (_src == null || !_on) return;
            _src.volume = 0.15f;
            LF_GameManager.I.StartCoroutine(RestoreVolume());
        }

        static System.Collections.IEnumerator RestoreVolume()
        {
            float t = 0f;
            while (t < 1.5f && _src != null)
            {
                t += Time.deltaTime;
                _src.volume = Mathf.Lerp(0.15f, 0.5f, Mathf.Clamp01(t / 1.5f));
                yield return null;
            }
            if (_src != null) _src.volume = 0.5f;
        }

        // ----------------------------------------------------------------
        // 温馨摇篮曲：D 大调舒缓琶音，10 秒循环
        // ----------------------------------------------------------------
        static AudioClip BuildLullaby()
        {
            const int rate = 22050;
            const float dur = 10f;
            int total = Mathf.RoundToInt(rate * dur);
            var data = new float[total];

            // D 大调舒缓分解和弦
            float[] notes =
            {
                293.66f, 369.99f, 440.00f, 587.33f, 440.00f, 369.99f,
                392.00f, 493.88f, 587.33f, 739.99f, 587.33f, 493.88f,
                329.63f, 440.00f, 493.88f, 587.33f, 493.88f, 440.00f,
                293.66f, 369.99f, 440.00f, 587.33f, 440.00f, 369.99f,
            };
            float noteLen = dur / notes.Length;

            for (int i = 0; i < notes.Length; i++)
            {
                int start = Mathf.RoundToInt(i * noteLen * rate);
                int len = Mathf.RoundToInt(noteLen * rate);
                AddWarmPluck(data, start, len, notes[i], rate);
            }

            AddDrone(data, 0f, dur, 146.83f, rate, 0.12f);
            AddDrone(data, 0f, dur, 73.42f, rate, 0.08f);

            var clip = AudioClip.Create("LF_Lullaby", total, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static void AddWarmPluck(float[] data, int start, int len, float freq, int rate)
        {
            for (int i = 0; i < len && start + i < data.Length; i++)
            {
                float t = (float)i / rate;
                float env = Mathf.Exp(-t * 3.0f);
                float s = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.5f
                        + Mathf.Sin(2f * Mathf.PI * freq * 2f * t) * 0.15f
                        + Mathf.Sin(2f * Mathf.PI * freq * 0.5f * t) * 0.1f;
                data[start + i] += s * env * 0.28f;
            }
        }

        static void AddDrone(float[] data, float t0, float dur, float freq, int rate, float vol)
        {
            int start = Mathf.RoundToInt(t0 * rate);
            int len = Mathf.RoundToInt(dur * rate);
            for (int i = 0; i < len && start + i < data.Length; i++)
            {
                float t = (float)i / rate;
                float fade = Mathf.Min(1f, t * 2f) * Mathf.Min(1f, (dur - t) * 2f);
                data[start + i] += Mathf.Sin(2f * Mathf.PI * freq * t) * vol * fade * 0.25f;
            }
        }
    }
}
