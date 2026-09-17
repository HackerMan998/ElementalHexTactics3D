using UnityEngine;

namespace ElementalHexTactics3D.Combat
{
    /// <summary>
    /// Self-contained 3D Sound Manager with zero-dependency procedural audio synthesis.
    /// Provides punchy SFX (impacts, spell whooshes, energy siphoning, cataclysm booms, UI clicks)
    /// without requiring any external audio files or dependencies.
    /// </summary>
    public class SoundManager3D : MonoBehaviour
    {
        private static SoundManager3D instance;
        public static SoundManager3D Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Object.FindFirstObjectByType<SoundManager3D>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("SoundManager3D");
                        instance = go.AddComponent<SoundManager3D>();
                    }
                }
                return instance;
            }
        }

        [Header("Audio Settings")]
        [SerializeField] private bool isMuted = false;
        [SerializeField] [Range(0f, 1f)] private float sfxVolume = 0.75f;
        [SerializeField] [Range(0f, 1f)] private float bgmVolume = 0.40f;

        private AudioSource sfxSource;
        private AudioSource bgmSource;

        // Procedural Audio Clips (Generated at runtime with zero external assets)
        private AudioClip fireWhooshClip;
        private AudioClip waterSplashClip;
        private AudioClip impactThudClip;
        private AudioClip siphonChimeClip;
        private AudioClip cataclysmBoomClip;
        private AudioClip buttonClickClip;

        public bool IsMuted
        {
            get => isMuted;
            set
            {
                isMuted = value;
                if (sfxSource != null) sfxSource.mute = isMuted;
                if (bgmSource != null) bgmSource.mute = isMuted;
            }
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                transform.SetParent(null);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }

            SetupAudioSources();
            GenerateProceduralAudioClips();
        }

        private void SetupAudioSources()
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.volume = sfxVolume;
            sfxSource.mute = isMuted;

            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
            bgmSource.volume = bgmVolume;
            bgmSource.mute = isMuted;
        }

        private void GenerateProceduralAudioClips()
        {
            fireWhooshClip = CreateSweepClip("SFX_FireWhoosh", 0.32f, 440f, 160f, isNoise: true);
            waterSplashClip = CreateSweepClip("SFX_WaterSplash", 0.28f, 600f, 300f, isNoise: true);
            impactThudClip = CreateThudClip("SFX_ImpactThud", 0.25f, 120f);
            siphonChimeClip = CreateChimeClip("SFX_SiphonChime", 0.40f);
            cataclysmBoomClip = CreateCataclysmClip("SFX_CataclysmBoom", 0.75f);
            buttonClickClip = CreateClickClip("SFX_ButtonClick", 0.04f, 1200f);
        }

        public void PlaySpellCast(bool isFire)
        {
            if (isMuted || sfxSource == null) return;
            sfxSource.PlayOneShot(isFire ? fireWhooshClip : waterSplashClip, sfxVolume);
        }

        public void PlaySlam(float volumeScale = 1.0f)
        {
            if (isMuted || sfxSource == null) return;
            sfxSource.PlayOneShot(impactThudClip, sfxVolume * volumeScale);
        }

        public void PlayConsumeLand()
        {
            if (isMuted || sfxSource == null) return;
            sfxSource.PlayOneShot(siphonChimeClip, sfxVolume * 1.1f);
        }

        public void PlayCataclysm()
        {
            if (isMuted || sfxSource == null) return;
            sfxSource.PlayOneShot(cataclysmBoomClip, sfxVolume * 1.3f);
        }

        public void PlayButtonClick()
        {
            if (isMuted || sfxSource == null) return;
            sfxSource.PlayOneShot(buttonClickClip, sfxVolume * 0.5f);
        }

        // ================= PROCEDURAL SYNTHESIS ENGINE ================= //

        private AudioClip CreateSweepClip(string name, float duration, float startFreq, float endFreq, bool isNoise)
        {
            int sampleRate = 44100;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            float phase = 0f;
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / totalSamples;
                float currentFreq = Mathf.Lerp(startFreq, endFreq, t);
                phase += 2f * Mathf.PI * currentFreq / sampleRate;

                float envelope = Mathf.Sin(t * Mathf.PI); // Smooth rise and fall
                float tone = Mathf.Sin(phase);
                float noise = (Random.value * 2f - 1f) * 0.35f;

                samples[i] = envelope * (isNoise ? (tone * 0.65f + noise) : tone);
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateThudClip(string name, float duration, float baseFreq)
        {
            int sampleRate = 44100;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            float phase = 0f;
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / totalSamples;
                float currentFreq = Mathf.Lerp(baseFreq, 35f, t);
                phase += 2f * Mathf.PI * currentFreq / sampleRate;

                float decay = Mathf.Exp(-t * 8f); // Exponential punch decay
                float noise = (Random.value * 2f - 1f) * 0.25f * decay;

                samples[i] = (Mathf.Sin(phase) * 0.75f + noise) * decay;
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateChimeClip(string name, float duration)
        {
            int sampleRate = 44100;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            // Harmonic ascending chime: C5 (523Hz), E5 (659Hz), G5 (784Hz), C6 (1046Hz)
            float[] notes = new float[] { 523.25f, 659.25f, 783.99f, 1046.50f };
            int stepSamples = totalSamples / notes.Length;

            for (int n = 0; n < notes.Length; n++)
            {
                float freq = notes[n];
                int start = n * stepSamples;
                int end = (n == notes.Length - 1) ? totalSamples : (n + 1) * stepSamples;
                float phase = 0f;

                for (int i = start; i < end; i++)
                {
                    phase += 2f * Mathf.PI * freq / sampleRate;
                    float localT = (float)(i - start) / (totalSamples - start);
                    float env = Mathf.Exp(-localT * 3.5f);
                    samples[i] = Mathf.Sin(phase) * env * 0.6f;
                }
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateCataclysmClip(string name, float duration)
        {
            int sampleRate = 44100;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            float phase = 0f;
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / totalSamples;
                float freq = Mathf.Lerp(90f, 25f, t);
                phase += 2f * Mathf.PI * freq / sampleRate;

                float decay = Mathf.Exp(-t * 2.8f);
                float noise = (Random.value * 2f - 1f) * 0.45f;

                samples[i] = (Mathf.Sin(phase) * 0.6f + noise) * decay;
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateClickClip(string name, float duration, float freq)
        {
            int sampleRate = 44100;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            float phase = 0f;
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / totalSamples;
                phase += 2f * Mathf.PI * freq / sampleRate;
                float env = 1f - t;
                samples[i] = Mathf.Sin(phase) * env * 0.35f;
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}

