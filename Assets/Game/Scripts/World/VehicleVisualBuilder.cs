using System.Collections;
using UnityEngine;

/// <summary>
/// Builds a lightweight futuristic car visual and plays a short crash reaction.
/// Does not change gameplay colliders or pooling ownership.
/// </summary>
public class VehicleVisualBuilder : MonoBehaviour
{
    public enum PaintStyle
    {
        PlayerCyan,
        TrafficPurple,
        TrafficMagenta,
        TrafficDark
    }

    [Header("Style")]
    [SerializeField] private PaintStyle paintStyle = PaintStyle.TrafficPurple;
    [SerializeField] private bool buildOnAwake = true;
    [SerializeField] private bool clearExistingMeshChildren = true;
    [SerializeField] private bool includeRunningLights = true;

    [Header("Crash Reaction")]
    [SerializeField] private float crashDuration = 0.35f;
    [SerializeField] private float crashTiltDegrees = 18f;
    [SerializeField] private float crashShake = 0.18f;

    [Header("Optional Overrides")]
    [SerializeField] private Material bodyMaterial;
    [SerializeField] private Material cabinMaterial;
    [SerializeField] private Material tireMaterial;
    [SerializeField] private Material headlightMaterial;
    [SerializeField] private Material taillightMaterial;
    [SerializeField] private Material accentMaterial;

    private bool built;
    private Transform visualRoot;
    private Quaternion visualBaseRotation;
    private Vector3 visualBasePosition;
    private ParticleSystem crashSparks;
    private Coroutine crashRoutine;
    private Renderer[] cachedRenderers;

    private void Awake()
    {
        if (buildOnAwake)
            Build();
    }

    private void OnEnable()
    {
        ResetVisualPose();
    }

    public void Build()
    {
        if (built)
            return;

        built = true;
        EnsureMaterials();

        if (clearExistingMeshChildren)
            HideExistingVisualMeshes();

        visualRoot = new GameObject("CarVisual").transform;
        visualRoot.SetParent(transform, false);
        visualRoot.localPosition = Vector3.zero;
        visualRoot.localRotation = Quaternion.identity;
        visualRoot.localScale = Vector3.one;
        visualBaseRotation = visualRoot.localRotation;
        visualBasePosition = visualRoot.localPosition;

        switch (paintStyle)
        {
            case PaintStyle.PlayerCyan:
                BuildSportsCar(visualRoot);
                break;
            case PaintStyle.TrafficMagenta:
                BuildWideSedan(visualRoot);
                break;
            case PaintStyle.TrafficDark:
                BuildCompactCar(visualRoot);
                break;
            default:
                BuildCoupe(visualRoot);
                break;
        }

        cachedRenderers = visualRoot.GetComponentsInChildren<Renderer>(true);
        CreateCrashSparks(visualRoot);
    }

    public void PlayCrashReaction()
    {
        if (visualRoot == null)
            Build();

        if (crashRoutine != null)
            StopCoroutine(crashRoutine);

        crashRoutine = StartCoroutine(CrashRoutine());
    }

    public float CrashDuration => crashDuration;

    private IEnumerator CrashRoutine()
    {
        if (crashSparks != null)
        {
            crashSparks.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
            crashSparks.Play();
            crashSparks.Emit(28);
        }

        float timer = 0f;
        Color flashA = new Color(1f, 0.35f, 0.15f);
        Color flashB = new Color(1f, 0.85f, 0.4f);

        while (timer < crashDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / Mathf.Max(crashDuration, 0.01f);
            float shake = (1f - t) * crashShake;

            if (visualRoot != null)
            {
                float tilt = Mathf.Sin(t * Mathf.PI) * crashTiltDegrees;
                float side = Mathf.Sin(t * 40f) * shake;

                visualRoot.localRotation =
                    visualBaseRotation *
                    Quaternion.Euler(tilt * 0.35f, side * 25f, -tilt);

                visualRoot.localPosition =
                    visualBasePosition +
                    new Vector3(side, Mathf.Abs(Mathf.Sin(t * 25f)) * shake, -t * 0.35f);
            }

            ApplyCrashFlash(Color.Lerp(flashA, flashB, Mathf.PingPong(t * 8f, 1f)));

            yield return null;
        }

        ResetVisualPose();
        crashRoutine = null;
    }

    private void ResetVisualPose()
    {
        if (visualRoot == null)
            return;

        visualRoot.localRotation = visualBaseRotation;
        visualRoot.localPosition = visualBasePosition;
        RestoreRendererColors();
    }

    private void ApplyCrashFlash(Color color)
    {
        if (cachedRenderers == null)
            return;

        foreach (Renderer renderer in cachedRenderers)
        {
            if (renderer == null)
                continue;

            foreach (Material mat in renderer.materials)
            {
                if (mat == null)
                    continue;

                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", color * 2.5f);
                }
            }
        }
    }

    private void RestoreRendererColors()
    {
        // Instance materials are recreated cheaply on next build only.
        // For pooled reuse we just clear strong flash by rebuilding emission
        // toward a dim accent based on paint style.
        if (cachedRenderers == null)
            return;

        Color restore = GetBodyColor() * 0.35f;

        foreach (Renderer renderer in cachedRenderers)
        {
            if (renderer == null)
                continue;

            foreach (Material mat in renderer.materials)
            {
                if (mat == null || !mat.HasProperty("_EmissionColor"))
                    continue;

                // Keep light parts bright; dim body flash.
                if (mat.name.Contains("Headlight") ||
                    mat.name.Contains("Tail") ||
                    mat.name.Contains("Accent"))
                    continue;

                mat.SetColor("_EmissionColor", restore);
            }
        }
    }

    private void BuildSportsCar(Transform root)
    {
        // Low futuristic player sports car
        CreatePart(root, "Chassis", PrimitiveType.Cube,
            new Vector3(0f, 0.38f, 0.05f),
            new Vector3(1.65f, 0.28f, 3.2f),
            bodyMaterial);

        CreatePart(root, "Nose", PrimitiveType.Cube,
            new Vector3(0f, 0.42f, 1.35f),
            new Vector3(1.45f, 0.22f, 0.85f),
            bodyMaterial);

        CreatePart(root, "Cabin", PrimitiveType.Cube,
            new Vector3(0f, 0.78f, -0.05f),
            new Vector3(1.25f, 0.42f, 1.35f),
            cabinMaterial);

        CreatePart(root, "RoofSlope", PrimitiveType.Cube,
            new Vector3(0f, 0.92f, 0.55f),
            new Vector3(1.15f, 0.16f, 0.7f),
            cabinMaterial);

        CreatePart(root, "SideSkirt_L", PrimitiveType.Cube,
            new Vector3(-0.88f, 0.22f, 0f),
            new Vector3(0.12f, 0.16f, 2.4f),
            accentMaterial);

        CreatePart(root, "SideSkirt_R", PrimitiveType.Cube,
            new Vector3(0.88f, 0.22f, 0f),
            new Vector3(0.12f, 0.16f, 2.4f),
            accentMaterial);

        CreatePart(root, "Spoiler", PrimitiveType.Cube,
            new Vector3(0f, 0.95f, -1.35f),
            new Vector3(1.5f, 0.08f, 0.28f),
            accentMaterial);

        CreatePart(root, "SpoilerPost_L", PrimitiveType.Cube,
            new Vector3(-0.45f, 0.75f, -1.2f),
            new Vector3(0.08f, 0.35f, 0.08f),
            accentMaterial);

        CreatePart(root, "SpoilerPost_R", PrimitiveType.Cube,
            new Vector3(0.45f, 0.75f, -1.2f),
            new Vector3(0.08f, 0.35f, 0.08f),
            accentMaterial);

        AddCommonDetails(root, 0.85f, 1.05f, 0.48f, 1.55f);
        AddLights(root, 0.5f, 1.58f);
    }

    private void BuildCoupe(Transform root)
    {
        CreatePart(root, "Body", PrimitiveType.Cube,
            new Vector3(0f, 0.52f, 0f),
            new Vector3(1.7f, 0.48f, 3.05f),
            bodyMaterial);

        CreatePart(root, "Hood", PrimitiveType.Cube,
            new Vector3(0f, 0.72f, 0.95f),
            new Vector3(1.5f, 0.18f, 0.95f),
            bodyMaterial);

        CreatePart(root, "Cabin", PrimitiveType.Cube,
            new Vector3(0f, 1.0f, -0.1f),
            new Vector3(1.4f, 0.5f, 1.4f),
            cabinMaterial);

        CreatePart(root, "RoofStripe", PrimitiveType.Cube,
            new Vector3(0f, 1.28f, -0.1f),
            new Vector3(0.25f, 0.06f, 1.2f),
            accentMaterial);

        CreatePart(root, "RearDeck", PrimitiveType.Cube,
            new Vector3(0f, 0.72f, -1.15f),
            new Vector3(1.55f, 0.2f, 0.7f),
            bodyMaterial);

        AddCommonDetails(root, 0.88f, 0.95f, 0.5f, 1.5f);
        AddLights(root, 0.55f, 1.55f);
    }

    private void BuildWideSedan(Transform root)
    {
        CreatePart(root, "Body", PrimitiveType.Cube,
            new Vector3(0f, 0.55f, 0f),
            new Vector3(1.85f, 0.55f, 3.25f),
            bodyMaterial);

        CreatePart(root, "Cabin", PrimitiveType.Cube,
            new Vector3(0f, 1.1f, -0.05f),
            new Vector3(1.55f, 0.58f, 1.7f),
            cabinMaterial);

        CreatePart(root, "FrontLip", PrimitiveType.Cube,
            new Vector3(0f, 0.28f, 1.55f),
            new Vector3(1.9f, 0.18f, 0.28f),
            accentMaterial);

        CreatePart(root, "RoofBar", PrimitiveType.Cube,
            new Vector3(0f, 1.42f, 0f),
            new Vector3(1.2f, 0.08f, 1.4f),
            accentMaterial);

        AddCommonDetails(root, 0.95f, 1.05f, 0.55f, 1.55f);
        AddLights(root, 0.58f, 1.62f);
    }

    private void BuildCompactCar(Transform root)
    {
        CreatePart(root, "Body", PrimitiveType.Cube,
            new Vector3(0f, 0.5f, 0.05f),
            new Vector3(1.55f, 0.5f, 2.7f),
            bodyMaterial);

        CreatePart(root, "Cabin", PrimitiveType.Cube,
            new Vector3(0f, 0.95f, 0.05f),
            new Vector3(1.35f, 0.45f, 1.35f),
            cabinMaterial);

        AddCommonDetails(root, 0.78f, 0.85f, 0.48f, 1.35f);
        AddLights(root, 0.5f, 1.4f);
    }

    private void AddCommonDetails(
        Transform root,
        float wheelX,
        float wheelZ,
        float wheelY,
        float bumperZ)
    {
        CreatePart(root, "BumperF", PrimitiveType.Cube,
            new Vector3(0f, 0.3f, bumperZ),
            new Vector3(1.7f, 0.2f, 0.2f),
            bodyMaterial);

        CreatePart(root, "BumperR", PrimitiveType.Cube,
            new Vector3(0f, 0.3f, -bumperZ),
            new Vector3(1.7f, 0.2f, 0.2f),
            bodyMaterial);

        CreateWheel(root, "Wheel_FL", new Vector3(-wheelX, wheelY, wheelZ));
        CreateWheel(root, "Wheel_FR", new Vector3(wheelX, wheelY, wheelZ));
        CreateWheel(root, "Wheel_RL", new Vector3(-wheelX, wheelY, -wheelZ));
        CreateWheel(root, "Wheel_RR", new Vector3(wheelX, wheelY, -wheelZ));

        CreatePart(root, "NeonStrip", PrimitiveType.Cube,
            new Vector3(0f, 0.12f, 0f),
            new Vector3(1.2f, 0.05f, 2.2f),
            accentMaterial);
    }

    private void AddLights(Transform root, float lightY, float lightZ)
    {
        CreatePart(root, "Headlight_L", PrimitiveType.Cube,
            new Vector3(-0.5f, lightY, lightZ),
            new Vector3(0.38f, 0.14f, 0.08f),
            headlightMaterial);

        CreatePart(root, "Headlight_R", PrimitiveType.Cube,
            new Vector3(0.5f, lightY, lightZ),
            new Vector3(0.38f, 0.14f, 0.08f),
            headlightMaterial);

        CreatePart(root, "TailLight_L", PrimitiveType.Cube,
            new Vector3(-0.5f, lightY, -lightZ),
            new Vector3(0.42f, 0.14f, 0.08f),
            taillightMaterial);

        CreatePart(root, "TailLight_R", PrimitiveType.Cube,
            new Vector3(0.5f, lightY, -lightZ),
            new Vector3(0.42f, 0.14f, 0.08f),
            taillightMaterial);

        CreatePointLight(root, "TailGlow",
            new Vector3(0f, lightY, -lightZ - 0.12f),
            new Color(1f, 0.08f, 0.12f),
            4.5f,
            5.5f);

        if (includeRunningLights)
        {
            CreatePointLight(root, "HeadGlow",
                new Vector3(0f, lightY, lightZ + 0.15f),
                new Color(0.85f, 0.95f, 1f),
                2.4f,
                6f);
        }
    }

    private void CreateCrashSparks(Transform root)
    {
        GameObject sparkObj = new GameObject("CrashSparks");
        sparkObj.transform.SetParent(root, false);
        sparkObj.transform.localPosition = new Vector3(0f, 0.6f, 0.8f);

        crashSparks = sparkObj.AddComponent<ParticleSystem>();
        var main = crashSparks.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = 0.35f;
        main.startSpeed = 4.5f;
        main.startSize = 0.08f;
        main.startColor = new Color(1f, 0.7f, 0.25f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 1.2f;
        main.maxParticles = 40;

        var emission = crashSparks.emission;
        emission.rateOverTime = 0f;

        var shape = crashSparks.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.35f;

        var renderer = sparkObj.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = PlayerEffects.SoftCircleMaterial();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.mesh = null;
            renderer.maxParticleSize = 0.05f;
        }

        crashSparks.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );
    }

    private void HideExistingVisualMeshes()
    {
        foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>(true))
        {
            if (renderer == null)
                continue;

            string n = renderer.gameObject.name;
            if (n.StartsWith("CarVisual") ||
                n.Contains("Underglow") ||
                n.Contains("Exhaust") ||
                n.Contains("Particle") ||
                n.Contains("SpeedLine") ||
                n.Contains("CrashSparks"))
                continue;

            renderer.enabled = false;

            Collider col = renderer.GetComponent<Collider>();
            if (col != null)
                col.enabled = false;
        }
    }

    private void EnsureMaterials()
    {
        if (bodyMaterial == null)
        {
            bodyMaterial = CreateLit(GetBodyColor(), GetBodyColor() * 0.35f, 0.7f, 0.65f);
            bodyMaterial.name = "BodyMat";
        }

        if (cabinMaterial == null)
        {
            cabinMaterial = CreateLit(
                new Color(0.04f, 0.1f, 0.2f),
                new Color(0.15f, 0.7f, 1.2f),
                0.2f,
                0.8f);
            cabinMaterial.name = "CabinMat";
        }

        if (tireMaterial == null)
        {
            tireMaterial = CreateLit(
                new Color(0.04f, 0.04f, 0.04f),
                Color.black,
                0.1f,
                0.2f);
            tireMaterial.name = "TireMat";
        }

        if (headlightMaterial == null)
        {
            headlightMaterial = CreateLit(
                new Color(0.9f, 0.95f, 1f),
                new Color(4.5f, 5f, 6f),
                0.1f,
                0.9f);
            headlightMaterial.name = "HeadlightMat";
        }

        if (taillightMaterial == null)
        {
            taillightMaterial = CreateLit(
                new Color(0.85f, 0.05f, 0.1f),
                new Color(4.2f, 0.2f, 0.25f),
                0.1f,
                0.85f);
            taillightMaterial.name = "TailMat";
        }

        if (accentMaterial == null)
        {
            Color accent = paintStyle == PaintStyle.PlayerCyan
                ? new Color(0.1f, 1f, 1f)
                : new Color(0.85f, 0.25f, 1f);

            accentMaterial = CreateLit(accent, accent * 2.2f, 0.2f, 0.7f);
            accentMaterial.name = "AccentMat";
        }
    }

    private static Material CreateLit(
        Color color,
        Color emission,
        float metallic,
        float smoothness)
    {
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Smoothness", smoothness);

        if (emission.maxColorComponent > 0.01f)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emission);
        }

        return mat;
    }

    private Color GetBodyColor()
    {
        switch (paintStyle)
        {
            case PaintStyle.PlayerCyan:
                return new Color(0.05f, 0.55f, 0.98f);
            case PaintStyle.TrafficMagenta:
                return new Color(0.9f, 0.12f, 0.55f);
            case PaintStyle.TrafficDark:
                return new Color(0.1f, 0.12f, 0.18f);
            default:
                return new Color(0.58f, 0.15f, 0.95f);
        }
    }

    private void CreateWheel(Transform parent, string name, Vector3 localPos)
    {
        GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        wheel.name = name;
        wheel.transform.SetParent(parent, false);
        wheel.transform.localPosition = localPos;
        wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        wheel.transform.localScale = new Vector3(0.58f, 0.16f, 0.58f);
        ApplyMaterial(wheel, tireMaterial);
        DisableCollider(wheel);

        GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rim.name = name + "_Rim";
        rim.transform.SetParent(wheel.transform, false);
        rim.transform.localPosition = Vector3.zero;
        rim.transform.localRotation = Quaternion.identity;
        rim.transform.localScale = new Vector3(0.55f, 1.05f, 0.55f);
        ApplyMaterial(rim, accentMaterial);
        DisableCollider(rim);
    }

    private static void CreatePart(
        Transform parent,
        string name,
        PrimitiveType type,
        Vector3 localPos,
        Vector3 localScale,
        Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPos;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = localScale;
        ApplyMaterial(part, material);
        DisableCollider(part);
    }

    private static void CreatePointLight(
        Transform parent,
        string name,
        Vector3 localPos,
        Color color,
        float intensity,
        float range)
    {
        GameObject lightObj = new GameObject(name);
        lightObj.transform.SetParent(parent, false);
        lightObj.transform.localPosition = localPos;

        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
    }

    private static void ApplyMaterial(GameObject obj, Material material)
    {
        MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
        if (renderer != null && material != null)
            renderer.sharedMaterial = material;
    }

    private static void DisableCollider(GameObject obj)
    {
        Collider col = obj.GetComponent<Collider>();
        if (col != null)
            Destroy(col);
    }
}
