using System.Collections;
using UnityEngine;

/// <summary>
/// Visible but modest fireworks high in the night sky.
/// A rocket rises, then a small colored burst.
/// </summary>
public class FireworksController : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera targetCamera;

    [Header("Upper Sky (viewport)")]
    [SerializeField] private float minViewportX = 0.3f;
    [SerializeField] private float maxViewportX = 0.7f;
    [SerializeField] private float minViewportY = 0.9f;
    [SerializeField] private float maxViewportY = 0.96f;
    [SerializeField] private float minDistance = 22f;
    [SerializeField] private float maxDistance = 34f;
    [SerializeField] private float minWorldY = 5f;

    [Header("Timing")]
    [SerializeField] private float firstDelay = 1.5f;
    [SerializeField] private float minInterval = 6f;
    [SerializeField] private float maxInterval = 12f;
    [SerializeField] private float rocketTravelTime = 0.65f;

    [Header("Size")]
    [SerializeField] private int explosionParticles = 80;
    [SerializeField] private float explosionSize = 0.22f;
    [SerializeField] private float rocketSize = 0.12f;
    [SerializeField] private float burstSpeed = 3.4f;

    private ParticleSystem rocket;
    private ParticleSystem burst;
    private Material particleMaterial;
    private Texture2D softTexture;
    private Vector3 pendingBurstPosition;

    private readonly Color[] colors =
    {
        new Color(1f, 0.82f, 0.28f),
        new Color(1f, 0.25f, 0.18f),
        new Color(0.3f, 0.95f, 1f),
        new Color(0.75f, 0.35f, 1f),
        new Color(0.3f, 1f, 0.45f)
    };

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        PrepareSystems();
    }

    private IEnumerator Start()
    {
        while (GameManager.Instance == null ||
               !GameManager.Instance.IsPlaying)
        {
            yield return null;
        }

        yield return new WaitForSecondsRealtime(firstDelay);

        while (true)
        {
            if (GameManager.Instance != null &&
                GameManager.Instance.IsPlaying &&
                EnsureCamera())
            {
                LaunchFirework();
                yield return new WaitForSecondsRealtime(rocketTravelTime);

                if (GameManager.Instance != null &&
                    GameManager.Instance.IsPlaying)
                {
                    ExplodeFirework();
                }
            }

            yield return new WaitForSecondsRealtime(
                Random.Range(minInterval, maxInterval)
            );
        }
    }

    private bool EnsureCamera()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        return targetCamera != null;
    }

    private void PrepareSystems()
    {
        softTexture = CreateSoftGlowTexture(64);
        particleMaterial = CreateParticleMaterial(softTexture);

        rocket = GetOrCreate("FireworkRocket", 16);
        burst = GetOrCreate("FireworkBurst", 160);

        var rocketMain = rocket.main;
        rocketMain.startSize = rocketSize;
        rocketMain.startSpeed = 0f;
        rocketMain.gravityModifier = 0f;

        var burstMain = burst.main;
        burstMain.startSize = explosionSize;
        burstMain.startSpeed = burstSpeed;
        burstMain.startLifetime = 1.25f;
        burstMain.gravityModifier = 0.45f;

        var shape = burst.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.08f;

        var sizeOverLifetime = burst.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            AnimationCurve.EaseInOut(0f, 1f, 1f, 0.15f)
        );

        var colorOverLifetime = burst.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.7f, 0.4f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = fade;
    }

    private ParticleSystem GetOrCreate(string name, int maxParticles)
    {
        Transform existing = transform.Find(name);
        ParticleSystem system = existing != null
            ? existing.GetComponent<ParticleSystem>()
            : null;

        if (system == null)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(transform, false);
            system = obj.AddComponent<ParticleSystem>();
        }

        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = system.main;
        main.playOnAwake = false;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = maxParticles;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;

        var emission = system.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;

        ParticleSystemRenderer renderer =
            system.GetComponent<ParticleSystemRenderer>();

        if (renderer != null)
        {
            renderer.sharedMaterial = particleMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 2.8f;
            renderer.velocityScale = 0.05f;
            renderer.minParticleSize = 0.001f;
            renderer.maxParticleSize = 0.045f;
            renderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        return system;
    }

    private static Texture2D CreateSoftGlowTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float center = (size - 1) * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float t = 1f - Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                t = t * t;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, t));
            }
        }

        tex.Apply();
        return tex;
    }

    private static Material CreateParticleMaterial(Texture2D texture)
    {
        Shader shader =
            Shader.Find("Universal Render Pipeline/Particles/Unlit");

        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");

        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        Material mat = new Material(shader);
        mat.mainTexture = texture;

        if (mat.HasProperty("_BaseMap"))
            mat.SetTexture("_BaseMap", texture);
        if (mat.HasProperty("_MainTex"))
            mat.SetTexture("_MainTex", texture);
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", Color.white);

        if (mat.HasProperty("_Surface"))
            mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend"))
            mat.SetFloat("_Blend", 2f);
        if (mat.HasProperty("_SrcBlend"))
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend"))
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        if (mat.HasProperty("_ZWrite"))
            mat.SetInt("_ZWrite", 0);

        mat.renderQueue = 3000;
        return mat;
    }

    private void LaunchFirework()
    {
        float x = Random.Range(minViewportX, maxViewportX);
        float y = Random.Range(minViewportY, maxViewportY);
        float distance = Random.Range(minDistance, maxDistance);

        pendingBurstPosition = targetCamera.ViewportToWorldPoint(
            new Vector3(x, y, distance)
        );

        Vector3 rocketPosition = targetCamera.ViewportToWorldPoint(
            new Vector3(x, y - 0.045f, distance)
        );

        pendingBurstPosition.y = Mathf.Max(pendingBurstPosition.y, minWorldY);
        rocketPosition.y = Mathf.Min(
            rocketPosition.y,
            pendingBurstPosition.y - 2.2f
        );

        rocket.transform.position = rocketPosition;
        burst.transform.position = pendingBurstPosition;

        Vector3 direction =
            (pendingBurstPosition - rocketPosition).normalized;
        float speed =
            Vector3.Distance(rocketPosition, pendingBurstPosition) /
            Mathf.Max(rocketTravelTime, 0.05f);

        rocket.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        rocket.Play();

        ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams();
        emit.position = rocketPosition;
        emit.velocity = direction * speed;
        emit.startSize = rocketSize;
        emit.startColor = new Color(1f, 0.95f, 0.75f, 1f);
        emit.startLifetime = rocketTravelTime;
        rocket.Emit(emit, 1);
    }

    private void ExplodeFirework()
    {
        Color color = colors[Random.Range(0, colors.Length)];
        burst.transform.position = pendingBurstPosition;

        var main = burst.main;
        main.startColor = color;
        main.startSize = explosionSize;
        main.startSpeed = burstSpeed;
        main.startLifetime = 1.25f;

        burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        burst.Play();
        burst.Emit(explosionParticles);
    }

    private void OnDestroy()
    {
        if (particleMaterial != null)
            Destroy(particleMaterial);

        if (softTexture != null)
            Destroy(softTexture);
    }
}
