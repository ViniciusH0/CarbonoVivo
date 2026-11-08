using System;
using UnityEngine;

public class BioacousticAudioManager : MonoBehaviour
{
    public static BioacousticAudioManager Instance { get; private set; }

    private AudioSource degradedSource;
    private AudioSource regeneratedSource;
    private AudioSource rainSource;
    private AudioSource fireSource;
    private AudioSource sfxSource;
    private float targetRainVolume = 0f;
    private float targetFireVolume = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SetupAudioSources();
    }

    private void SetupAudioSources()
    {
        // Fonte para ambiente degradado (vento seco, fumaça)
        degradedSource = gameObject.AddComponent<AudioSource>();
        degradedSource.loop = true;
        degradedSource.volume = 0.5f;
        degradedSource.clip = GenerateDegradedLoop();
        degradedSource.Play();

        // Fonte para ambiente regenerado (nascentes d'água, cantos de aves da Mata Atlântica)
        regeneratedSource = gameObject.AddComponent<AudioSource>();
        regeneratedSource.loop = true;
        regeneratedSource.volume = 0f;
        regeneratedSource.clip = GenerateRegeneratedLoop();
        regeneratedSource.Play();

        // Fonte para chuva tropical bioacústica
        rainSource = gameObject.AddComponent<AudioSource>();
        rainSource.loop = true;
        rainSource.volume = 0f;
        rainSource.clip = GenerateRainLoop();
        rainSource.Play();

        // Fonte para incêndio florestal procedural (chamas rugindo + estalos de madeira)
        fireSource = gameObject.AddComponent<AudioSource>();
        fireSource.loop = true;
        fireSource.volume = 0f;
        fireSource.clip = GenerateFireLoop();
        fireSource.Play();

        // Fonte para efeitos sonoros pontuais
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.volume = 0.7f;
    }

    private void Update()
    {
        float healthRatio = 0.15f;
        if (IndicatorManager.Instance != null)
        {
            healthRatio = Mathf.Clamp01(IndicatorManager.Instance.forestHealth / 10f);
        }

        // Transição suave de bioacústica reativa
        float targetDegradedVol = Mathf.Lerp(0.45f, 0.02f, healthRatio);
        float targetRegenVol = Mathf.Lerp(0.02f, 0.45f, healthRatio);

        if (degradedSource != null)
            degradedSource.volume = Mathf.Lerp(degradedSource.volume, targetDegradedVol, Time.deltaTime * 1.5f);

        if (regeneratedSource != null)
            regeneratedSource.volume = Mathf.Lerp(regeneratedSource.volume, targetRegenVol, Time.deltaTime * 1.5f);

        if (rainSource != null)
            rainSource.volume = Mathf.MoveTowards(rainSource.volume, targetRainVolume, Time.deltaTime * 0.6f);

        if (fireSource != null)
            fireSource.volume = Mathf.MoveTowards(fireSource.volume, targetFireVolume, Time.deltaTime * 1.8f);
    }

    public void StartRainSound()
    {
        targetRainVolume = 0.55f;
    }

    public void StopRainSound()
    {
        targetRainVolume = 0f;
    }

    public void SetFireAudioIntensity(float normalizedIntensity)
    {
        targetFireVolume = Mathf.Clamp01(normalizedIntensity) * 0.75f;
    }

    public void PlayExtinguishSound()
    {
        if (sfxSource == null) return;
        AudioClip clip = GenerateWaterSizzleSound(0.28f);
        sfxSource.PlayOneShot(clip, 0.75f);
    }

    public void PlayDroneLaunchSound()
    {
        if (sfxSource == null) return;
        AudioClip clip = GenerateShortTone(340f, 520f, 0.18f);
        sfxSource.PlayOneShot(clip, 0.45f);
    }

    public void PlayPlantSound()
    {
        AudioClip clip = GenerateShortTone(523.25f, 659.25f, 0.2f); // C5 -> E5
        sfxSource.PlayOneShot(clip, 0.6f);
    }

    public void PlayPositiveChime()
    {
        AudioClip clip = GenerateShortTone(587.33f, 880f, 0.22f); // D5 -> A5 (brilho ascendente suave)
        if (sfxSource != null) sfxSource.PlayOneShot(clip, 0.65f);
    }

    public void PlayNegativeWarning()
    {
        AudioClip clip = GenerateShortTone(220f, 150f, 0.28f); // tom grave descendente suave
        if (sfxSource != null) sfxSource.PlayOneShot(clip, 0.65f);
    }

    public void PlayTreeClickSound()
    {
        AudioClip clip = GenerateShortTone(440f, 440f, 0.08f, true); // toque suave
        sfxSource.PlayOneShot(clip, 0.4f);
    }

    public void PlayAlertSound()
    {
        AudioClip clip = GenerateShortTone(300f, 200f, 0.35f); // alerta descendente
        sfxSource.PlayOneShot(clip, 0.7f);
    }

    public void PlayFleeSound()
    {
        AudioClip clip = GenerateShortTone(600f, 880f, 0.15f); // fuga ascendente
        sfxSource.PlayOneShot(clip, 0.5f);
    }

    public void PlayHitSound()
    {
        AudioClip clip = GenerateShortTone(240f, 90f, 0.12f, true); // impacto/soco percussivo
        sfxSource.PlayOneShot(clip, 0.85f);
    }

    public void PlayChopSound()
    {
        AudioClip clip = GenerateShortTone(180f, 75f, 0.08f, true); // machadada percussiva na madeira
        sfxSource.PlayOneShot(clip, 0.55f);
    }

    // Geração procedural de áudio para ambiente seco/degradado (ruído de vento filtrado)
    private AudioClip GenerateDegradedLoop()
    {
        int sampleRate = 22050;
        int length = sampleRate * 4; // 4 segundos de loop
        float[] data = new float[length];
        System.Random rnd = new System.Random(42);

        float lastSample = 0f;
        for (int i = 0; i < length; i++)
        {
            float white = (float)(rnd.NextDouble() * 2.0 - 1.0);
            // Filtro passa-baixa para criar sensação de vento seco e distante
            lastSample = Mathf.Lerp(lastSample, white, 0.04f);
            float lowRumble = Mathf.Sin(2f * Mathf.PI * 45f * i / sampleRate) * 0.15f;
            data[i] = (lastSample * 0.5f + lowRumble) * 0.4f;
        }

        AudioClip clip = AudioClip.Create("DegradedLoop", length, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // Geração procedural de bioacústica regenerada (nascente d'água + cantos de Gralha-azul)
    private AudioClip GenerateRegeneratedLoop()
    {
        int sampleRate = 22050;
        int length = sampleRate * 6; // 6 segundos de loop
        float[] data = new float[length];
        System.Random rnd = new System.Random(1337);

        float waterSample = 0f;
        for (int i = 0; i < length; i++)
        {
            float t = (float)i / sampleRate;

            // Água corrente (filtro passa-faixa suave)
            float white = (float)(rnd.NextDouble() * 2.0 - 1.0);
            waterSample = Mathf.Lerp(waterSample, white, 0.15f);

            // Canto rítmico simulando Gralha-azul / pássaros nativos da Mata Atlântica em momentos específicos
            float birdChirp = 0f;
            if ((t > 1.2f && t < 1.45f) || (t > 3.5f && t < 3.8f) || (t > 4.1f && t < 4.35f))
            {
                float chirpFreq = 1800f + Mathf.Sin(t * 40f) * 400f;
                birdChirp = Mathf.Sin(2f * Mathf.PI * chirpFreq * t) * 0.25f;
            }

            data[i] = (waterSample * 0.2f + birdChirp) * 0.5f;
        }

        AudioClip clip = AudioClip.Create("RegeneratedLoop", length, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip GenerateShortTone(float startFreq, float endFreq, float duration, bool isPercussive = false)
    {
        int sampleRate = 22050;
        int length = Mathf.RoundToInt(sampleRate * duration);
        float[] data = new float[length];

        for (int i = 0; i < length; i++)
        {
            float progress = (float)i / length;
            float freq = Mathf.Lerp(startFreq, endFreq, progress);
            float envelope = isPercussive ? Mathf.Exp(-progress * 10f) : (1f - progress);
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * i / sampleRate) * envelope;
        }

        AudioClip clip = AudioClip.Create("Tone", length, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip GenerateRainLoop()
    {
        int sampleRate = 22050;
        int length = sampleRate * 5; // 5 segundos de loop contínuo
        float[] data = new float[length];
        System.Random rnd = new System.Random(777);

        float b0 = 0f, b1 = 0f, b2 = 0f;

        for (int i = 0; i < length; i++)
        {
            float white = (float)(rnd.NextDouble() * 2.0 - 1.0);

            // Filtro Pink Noise para som encorpado e suave de chuva contínua
            b0 = 0.99886f * b0 + white * 0.0555179f;
            b1 = 0.99332f * b1 + white * 0.0750759f;
            b2 = 0.96900f * b2 + white * 0.1538520f;
            float pink = (b0 + b1 + b2 + white * 0.5362f) * 0.12f;

            // Gotas isoladas estalando na vegetação
            float drops = 0f;
            if (rnd.NextDouble() < 0.0035)
            {
                drops = (float)(rnd.NextDouble() * 0.35 - 0.175);
            }

            data[i] = Mathf.Clamp(pink * 0.75f + drops, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("RainLoop", length, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // Síntese procedural de incêndio florestal: rugido grave das chamas + chiado + estalos secos de madeira queimando
    private AudioClip GenerateFireLoop()
    {
        int sampleRate = 22050;
        int length = sampleRate * 4; // 4 segundos de loop rico
        float[] data = new float[length];
        System.Random rnd = new System.Random(909);

        float lowRoar = 0f;
        float midHiss = 0f;
        float crackleEnv = 0f;
        float crackleFreq = 1800f;

        for (int i = 0; i < length; i++)
        {
            float t = (float)i / sampleRate;
            float white = (float)(rnd.NextDouble() * 2.0 - 1.0);

            // 1. Rugido grave das labaredas (filtro passa-baixa + modulação de vento/chama)
            lowRoar = Mathf.Lerp(lowRoar, white, 0.035f);
            float flameFlutter = 0.65f + 0.35f * Mathf.Sin(2f * Mathf.PI * 3.2f * t) * Mathf.Cos(2f * Mathf.PI * 5.7f * t);
            float roarSignal = lowRoar * flameFlutter * 0.9f;

            // 2. Chiado médio de calor e folhas queimando
            midHiss = Mathf.Lerp(midHiss, white - lowRoar, 0.22f);
            float hissSignal = midHiss * 0.22f;

            // 3. Estalos agudos de galhos e fagulhas (crackling pops)
            if (rnd.NextDouble() < 0.0018)
            {
                crackleEnv = (float)(0.55 + rnd.NextDouble() * 0.45);
                crackleFreq = (float)(1400.0 + rnd.NextDouble() * 2200.0);
            }
            crackleEnv *= 0.965f;
            float crackleSignal = Mathf.Sin(2f * Mathf.PI * crackleFreq * t) * white * crackleEnv * 0.85f;

            data[i] = Mathf.Clamp(roarSignal + hissSignal + crackleSignal, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("ForestFireLoop", length, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip GenerateWaterSizzleSound(float duration)
    {
        int sampleRate = 22050;
        int length = Mathf.RoundToInt(sampleRate * duration);
        float[] data = new float[length];
        System.Random rnd = new System.Random();

        float filtered = 0f;
        for (int i = 0; i < length; i++)
        {
            float progress = (float)i / length;
            float white = (float)(rnd.NextDouble() * 2.0 - 1.0);
            filtered = Mathf.Lerp(filtered, white, Mathf.Lerp(0.45f, 0.12f, progress));
            float env = Mathf.Sin(progress * Mathf.PI) * (1f - progress * 0.5f);
            float dropTone = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(420f, 180f, progress) * i / sampleRate) * 0.25f;
            data[i] = Mathf.Clamp((filtered * 0.65f + dropTone) * env, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("WaterExtinguish", length, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}

