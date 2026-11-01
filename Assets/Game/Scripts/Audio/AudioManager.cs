using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Music")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.35f;

    [Header("Engine / Motor")]
    [SerializeField] private AudioClip engineLoop;
    [SerializeField] [Range(0f, 1f)] private float engineVolume = 0.55f;
    [SerializeField] private float minEnginePitch = 0.7f;
    [SerializeField] private float maxEnginePitch = 1.7f;

    [Header("SFX")]
    [SerializeField] private AudioClip collisionClip;
    [SerializeField] private AudioClip brakeClip;
    [SerializeField] private AudioClip coinClip;
    [SerializeField] private AudioClip fuelClip;
    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField] private AudioClip gameOverClip;

    [SerializeField] [Range(0f, 1f)] private float sfxVolume = 0.7f;

    [Header("Anti-Spam")]
    [SerializeField] private float coinCooldown = 0.05f;
    [SerializeField] private float collisionCooldown = 0.15f;
    [SerializeField] private float brakeCooldown = 0.2f;
    [SerializeField] private float clickCooldown = 0.08f;

    private AudioSource musicSource;
    private AudioSource engineSource;
    private AudioSource sfxSource;

    private float lastCoinTime = -10f;
    private float lastCollisionTime = -10f;
    private float lastBrakeTime = -10f;
    private float lastClickTime = -10f;
    private float engineDuckUntil = -10f;
    private bool gameOverPlayed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        musicSource = CreateSource("MusicSource", true);
        engineSource = CreateSource("EngineSource", true);
        sfxSource = CreateSource("SfxSource", false);

        musicSource.volume = musicVolume;
        engineSource.volume = engineVolume;
        sfxSource.volume = 1f;
        sfxSource.priority = 32;

        TryLoadClipsFromResources();
    }

    private AudioSource CreateSource(string name, bool loop)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(transform);

        AudioSource source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        return source;
    }

    private void TryLoadClipsFromResources()
    {
        if (backgroundMusic == null)
            backgroundMusic = Resources.Load<AudioClip>("Audio/racing_bgm");

        if (engineLoop == null)
            engineLoop = Resources.Load<AudioClip>("Audio/engine_hum");

        if (collisionClip == null)
            collisionClip = Resources.Load<AudioClip>("Audio/collision");

        if (brakeClip == null)
            brakeClip = Resources.Load<AudioClip>("Audio/brake");

        if (coinClip == null)
            coinClip = Resources.Load<AudioClip>("Audio/coin");

        if (fuelClip == null)
            fuelClip = Resources.Load<AudioClip>("Audio/fuel");

        if (buttonClickClip == null)
            buttonClickClip = Resources.Load<AudioClip>("Audio/click");

        if (gameOverClip == null)
            gameOverClip = Resources.Load<AudioClip>("Audio/game_over");
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
            return;

        UpdateEngine(gm);
        UpdateMusic(gm);
    }

    private void UpdateMusic(GameManager gm)
    {
        bool shouldPlay = gm.IsPlaying ||
            gm.State == GameManager.GameState.Paused;

        if (shouldPlay)
        {
            if (backgroundMusic != null && !musicSource.isPlaying)
            {
                musicSource.clip = backgroundMusic;
                musicSource.Play();
            }

            musicSource.volume = musicVolume;
        }
        else if (musicSource.isPlaying &&
                 gm.State == GameManager.GameState.Menu)
        {
            musicSource.Stop();
        }

        musicSource.pitch = gm.State == GameManager.GameState.Paused
            ? 0.85f
            : 1f;
    }

    private void UpdateEngine(GameManager gm)
    {
        if (engineLoop == null)
        {
            engineLoop = Resources.Load<AudioClip>("Audio/engine_hum");
            if (engineLoop == null)
                return;
        }

        if (gm.IsPlaying)
        {
            if (engineSource.clip != engineLoop)
                engineSource.clip = engineLoop;

            if (!engineSource.isPlaying)
            {
                engineSource.loop = true;
                engineSource.volume = engineVolume;
                engineSource.Play();
            }

            float ratio = Mathf.InverseLerp(
                gm.MinimumSpeed,
                gm.MaximumSpeed,
                gm.CurrentSpeed
            );

            // Motor pitch rises with speed; volume also rises a bit.
            engineSource.pitch = Mathf.Lerp(
                minEnginePitch,
                maxEnginePitch,
                ratio
            );

            float volume = Mathf.Lerp(
                engineVolume * 0.75f,
                engineVolume,
                ratio
            );

            if (Time.unscaledTime < engineDuckUntil)
                volume *= 0.2f;

            engineSource.volume = volume;
        }
        else if (engineSource.isPlaying)
        {
            engineSource.Stop();
        }
    }

    public void PlayCollision()
    {
        if (Time.unscaledTime - lastCollisionTime < collisionCooldown)
            return;

        lastCollisionTime = Time.unscaledTime;
        engineDuckUntil = Time.unscaledTime + 0.45f;
        PlayOneShot(collisionClip, 1f);
        PlayBrake();
    }

    public void PlayBrake()
    {
        if (Time.unscaledTime - lastBrakeTime < brakeCooldown)
            return;

        lastBrakeTime = Time.unscaledTime;
        PlayOneShot(brakeClip, 0.7f);
    }

    public void PlayCoin()
    {
        if (Time.unscaledTime - lastCoinTime < coinCooldown)
            return;

        lastCoinTime = Time.unscaledTime;
        engineDuckUntil = Time.unscaledTime + 0.2f;
        PlayOneShot(coinClip, 1f);
    }

    public void PlayFuel()
    {
        engineDuckUntil = Time.unscaledTime + 0.35f;
        PlayOneShot(fuelClip, 1f);
    }

    public void PlayButtonClick()
    {
        if (Time.unscaledTime - lastClickTime < clickCooldown)
            return;

        lastClickTime = Time.unscaledTime;
        PlayOneShot(buttonClickClip, 0.55f);
    }

    public void PlayGameOver()
    {
        if (gameOverPlayed)
            return;

        gameOverPlayed = true;

        if (engineSource.isPlaying)
            engineSource.Stop();

        if (musicSource.isPlaying)
            musicSource.Stop();

        PlayOneShot(gameOverClip, 1f);
    }

    public void ResetGameOverFlag()
    {
        gameOverPlayed = false;
    }

    public float MusicVolume => musicVolume;
    public float EngineVolume => engineVolume;
    public float SfxVolume => sfxVolume;

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        if (musicSource != null)
            musicSource.volume = musicVolume;
    }

    public void SetEngineVolume(float value)
    {
        engineVolume = Mathf.Clamp01(value);
    }

    public void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
    }

    private void PlayOneShot(AudioClip clip, float volumeScale)
    {
        if (clip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(clip, sfxVolume * volumeScale);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
