using UnityEngine;

namespace LittleTrawling.Audio
{
    /// <summary>
    /// Generates clean procedural audio clip fallbacks when Inspector sound clips are unassigned.
    /// </summary>
    public static class ProceduralAudioSynthesizer
    {
        private static AudioClip _castChargeClip;
        private static AudioClip _castReleaseClip;
        private static AudioClip _choiceSelectClip;
        private static AudioClip _choiceHoverClip;
        private static AudioClip _windowOpenClip;
        private static AudioClip _windowCloseClip;
        private static AudioClip _buttonHoverClip;

        public static AudioClip GetCastChargeSound()
        {
            if (_castChargeClip != null) return _castChargeClip;
            int rate = 44100;
            int count = (int)(rate * 0.25f);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / rate;
                float progress = (float)i / count;
                float freq = Mathf.Lerp(220f, 660f, progress);
                float envelope = Mathf.Sin(progress * Mathf.PI);
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.15f;
            }
            _castChargeClip = AudioClip.Create("Synth_CastCharge", count, 1, rate, false);
            _castChargeClip.SetData(samples, 0);
            return _castChargeClip;
        }

        public static AudioClip GetCastReleaseSound()
        {
            if (_castReleaseClip != null) return _castReleaseClip;
            int rate = 44100;
            int count = (int)(rate * 0.15f);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float progress = (float)i / count;
                float envelope = (1f - progress) * (1f - progress);
                float noise = (Random.value * 2f - 1f) * 0.15f;
                float freq = Mathf.Lerp(800f, 200f, progress);
                float t = (float)i / rate;
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.1f;
                samples[i] = (noise + tone) * envelope;
            }
            _castReleaseClip = AudioClip.Create("Synth_CastRelease", count, 1, rate, false);
            _castReleaseClip.SetData(samples, 0);
            return _castReleaseClip;
        }

        public static AudioClip GetChoiceSelectSound()
        {
            if (_choiceSelectClip != null) return _choiceSelectClip;
            int rate = 44100;
            int count = (int)(rate * 0.12f);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / rate;
                float progress = (float)i / count;
                float freq = (progress < 0.5f) ? 523.25f : 659.25f;
                float envelope = 1f - progress;
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.18f;
            }
            _choiceSelectClip = AudioClip.Create("Synth_ChoiceSelect", count, 1, rate, false);
            _choiceSelectClip.SetData(samples, 0);
            return _choiceSelectClip;
        }

        public static AudioClip GetChoiceHoverSound()
        {
            if (_choiceHoverClip != null) return _choiceHoverClip;
            int rate = 44100;
            int count = (int)(rate * 0.03f);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / rate;
                float envelope = 1f - ((float)i / count);
                samples[i] = Mathf.Sin(2f * Mathf.PI * 880f * t) * envelope * 0.08f;
            }
            _choiceHoverClip = AudioClip.Create("Synth_ChoiceHover", count, 1, rate, false);
            _choiceHoverClip.SetData(samples, 0);
            return _choiceHoverClip;
        }

        public static AudioClip GetWindowOpenSound()
        {
            if (_windowOpenClip != null) return _windowOpenClip;
            int rate = 44100;
            int count = (int)(rate * 0.10f);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / rate;
                float progress = (float)i / count;
                float freq = Mathf.Lerp(440f, 880f, progress);
                float envelope = Mathf.Sin(progress * Mathf.PI);
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.15f;
            }
            _windowOpenClip = AudioClip.Create("Synth_WindowOpen", count, 1, rate, false);
            _windowOpenClip.SetData(samples, 0);
            return _windowOpenClip;
        }

        public static AudioClip GetWindowCloseSound()
        {
            if (_windowCloseClip != null) return _windowCloseClip;
            int rate = 44100;
            int count = (int)(rate * 0.08f);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / rate;
                float progress = (float)i / count;
                float freq = Mathf.Lerp(660f, 330f, progress);
                float envelope = (1f - progress);
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.15f;
            }
            _windowCloseClip = AudioClip.Create("Synth_WindowClose", count, 1, rate, false);
            _windowCloseClip.SetData(samples, 0);
            return _windowCloseClip;
        }

        public static AudioClip GetButtonHoverSound()
        {
            if (_buttonHoverClip != null) return _buttonHoverClip;
            int rate = 44100;
            int count = (int)(rate * 0.02f);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / rate;
                float envelope = 1f - ((float)i / count);
                samples[i] = Mathf.Sin(2f * Mathf.PI * 750f * t) * envelope * 0.06f;
            }
            _buttonHoverClip = AudioClip.Create("Synth_ButtonHover", count, 1, rate, false);
            _buttonHoverClip.SetData(samples, 0);
            return _buttonHoverClip;
        }
    }
}
