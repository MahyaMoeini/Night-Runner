using UnityEngine;

public class PlayerEffects : MonoBehaviour
{
    public static PlayerEffects Instance { get; private set; }

    [Header("Particle Effects")]
    [SerializeField] private ParticleSystem crashParticles;
    [SerializeField] private ParticleSystem coinParticles;
    [SerializeField] private ParticleSystem fuelParticles;
    [SerializeField] private ParticleSystem exhaustSmoke;

    [Header("Camera")]
    [SerializeField] private SpeedFeelEffect speedFeelEffect;

    [Header("Exhaust Tuning")]
    [SerializeField] private float minExhaustRate = 8f;
    [SerializeField] private float maxExhaustRate = 36f;
    [SerializeField] private Color exhaustColor =
        new Color(0.55f, 0.58f, 0.62f, 0.35f);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        PrepareEffect(crashParticles, new Color(1f, 0.55f, 0.15f, 1f), 0.055f, 5.5f, 0.3f, 1.0f);
        PrepareEffect(coinParticles, new Color(1f, 0.82f, 0.25f, 1f), 0.07f, 2.6f, 0.45f, 0.12f);
        PrepareEffect(fuelParticles, new Color(0.3f, 1f, 0.45f, 1f), 0.075f, 2.2f, 0.5f, 0.05f);
        PrepareEffect(exhaustSmoke, exhaustColor, 0.1f, 1.2f, 0.65f, -0.04f);

        if (exhaustSmoke != null)
        {
            var main = exhaustSmoke.main;
            main.loop = true;
            exhaustSmoke.Play();
        }
    }

    private static void PrepareEffect(
        ParticleSystem effect,
        Color color,
        float size,
        float speed,
        float lifetime,
        float gravity)
    {
        if (effect == null)
            return;

        var main = effect.main;
        main.playOnAwake = false;
        main.loop = false;
        main.startColor = color;
        main.startSize = size;
        main.startSpeed = speed;
        main.startLifetime = lifetime;
        main.gravityModifier = gravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 80;

        var emission = effect.emission;
        emission.rateOverTime = 0f;

        var shape = effect.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.15f;

        ParticleSystemRenderer renderer = effect.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = SoftCircleMaterial();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.mesh = null;
            renderer.minParticleSize = 0.001f;
            renderer.maxParticleSize = 0.06f;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    private static Material softCircleMaterial;

    public static Material SoftCircleMaterial()
    {
        if (softCircleMaterial != null)
            return softCircleMaterial;

        // Sprites/Default reliably uses the alpha of a soft circle texture.
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");

        softCircleMaterial = new Material(shader);
        Texture2D circle = CreateSoftCircle(64);
        softCircleMaterial.mainTexture = circle;
        softCircleMaterial.color = Color.white;

        if (softCircleMaterial.HasProperty("_MainTex"))
            softCircleMaterial.SetTexture("_MainTex", circle);
        if (softCircleMaterial.HasProperty("_BaseMap"))
            softCircleMaterial.SetTexture("_BaseMap", circle);
        if (softCircleMaterial.HasProperty("_BaseColor"))
            softCircleMaterial.SetColor("_BaseColor", Color.white);
        if (softCircleMaterial.HasProperty("_Color"))
            softCircleMaterial.SetColor("_Color", Color.white);
        if (softCircleMaterial.HasProperty("_Surface"))
            softCircleMaterial.SetFloat("_Surface", 1f);
        if (softCircleMaterial.HasProperty("_Blend"))
            softCircleMaterial.SetFloat("_Blend", 0f);
        if (softCircleMaterial.HasProperty("_SrcBlend"))
            softCircleMaterial.SetInt(
                "_SrcBlend",
                (int)UnityEngine.Rendering.BlendMode.SrcAlpha
            );
        if (softCircleMaterial.HasProperty("_DstBlend"))
            softCircleMaterial.SetInt(
                "_DstBlend",
                (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha
            );
        if (softCircleMaterial.HasProperty("_ZWrite"))
            softCircleMaterial.SetInt("_ZWrite", 0);

        softCircleMaterial.renderQueue = 3000;
        return softCircleMaterial;
    }

    private static Texture2D CreateSoftCircle(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        float center = (size - 1) * 0.5f;
        float outer = center;
        float inner = center * 0.55f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha;

                if (dist <= inner)
                    alpha = 1f;
                else if (dist >= outer)
                    alpha = 0f;
                else
                    alpha = 1f - ((dist - inner) / (outer - inner));

                alpha = alpha * alpha;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply(false, false);
        return tex;
    }

    private void Update()
    {
        if (exhaustSmoke == null ||
            GameManager.Instance == null)
            return;

        GameManager gm = GameManager.Instance;

        float ratio = Mathf.InverseLerp(
            gm.MinimumSpeed,
            gm.MaximumSpeed,
            gm.CurrentSpeed
        );

        var emission = exhaustSmoke.emission;

        emission.rateOverTime = gm.IsPlaying
            ? Mathf.Lerp(minExhaustRate, maxExhaustRate, ratio)
            : 0f;

        var main = exhaustSmoke.main;
        main.startSize = Mathf.Lerp(0.08f, 0.16f, ratio);
        main.startSpeed = Mathf.Lerp(0.9f, 2.4f, ratio);
    }

    public void PlayCrash()
    {
        EmitEffect(crashParticles, 56);

        if (speedFeelEffect != null)
            speedFeelEffect.TriggerCrashShake();

        PlayerController player =
            FindFirstObjectByType<PlayerController>();

        if (player != null)
        {
            VehicleVisualBuilder playerVisual =
                player.GetComponent<VehicleVisualBuilder>();

            if (playerVisual != null)
                playerVisual.PlayCrashReaction();
        }
    }

    public void PlayCoin()
    {
        EmitEffect(coinParticles, 28);
    }

    public void PlayFuel()
    {
        EmitEffect(fuelParticles, 36);
    }

    private void EmitEffect(ParticleSystem effect, int count)
    {
        if (effect == null)
            return;

        effect.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        effect.Play();
        effect.Emit(count);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
