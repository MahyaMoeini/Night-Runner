using UnityEngine;

public class SpeedFeelEffect : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Camera targetCamera;

    [Header("Speed FOV")]
    [SerializeField] private float extraFovAtMaxSpeed = 26f;
    [SerializeField] private float smoothness = 7f;

    [Header("Speed Lines")]
    [SerializeField] private ParticleSystem speedLines;
    [SerializeField] private float maxSpeedLineRate = 70f;
    [SerializeField] private float linesStartSpeed = 14f;
    [SerializeField] private Color speedLineColor =
        new Color(0.55f, 0.95f, 1f, 0.55f);

    [Header("Crash Camera Shake")]
    [SerializeField] private float shakeDuration = 0.28f;
    [SerializeField] private float shakeStrength = 0.22f;

    private GameManager gm;

    private float baseFov;
    private Vector3 originalLocalPosition;

    private float shakeTimer;
    private bool speedLineColorApplied;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();

        if (targetCamera != null)
        {
            baseFov = targetCamera.fieldOfView;

            originalLocalPosition =
                targetCamera.transform.localPosition;
        }
    }

    private void Start()
    {
        gm = GameManager.Instance;

        if (speedLines == null && targetCamera != null)
            speedLines = CreateSpeedLines(targetCamera.transform);

        if (speedLines != null)
        {
            ApplySpeedLineStyle();

            speedLines.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );

            speedLines.Play();
        }
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
            return;

        if (gm == null)
            gm = GameManager.Instance;

        if (gm == null)
            return;

        UpdateCameraFOV();
        UpdateSpeedLines();
        UpdateCameraShake();
    }

    private static ParticleSystem CreateSpeedLines(Transform parent)
    {
        GameObject linesObject = new GameObject("SpeedLines");
        linesObject.transform.SetParent(parent, false);
        linesObject.transform.localPosition = new Vector3(0f, 0f, 2.5f);
        linesObject.transform.localRotation = Quaternion.identity;

        ParticleSystem lines = linesObject.AddComponent<ParticleSystem>();
        var main = lines.main;
        main.playOnAwake = false;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 80;

        var shape = lines.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 8f;
        shape.radius = 0.4f;

        ParticleSystemRenderer renderer = lines.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 4.5f;
        renderer.velocityScale = 0.05f;

        return lines;
    }

    private void ApplySpeedLineStyle()
    {
        if (speedLineColorApplied || speedLines == null)
            return;

        speedLineColorApplied = true;

        var main = speedLines.main;
        main.startColor = speedLineColor;
        main.startSize = 0.035f;
        main.startSpeed = 28f;
        main.startLifetime = 0.28f;

        var renderer = speedLines.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.lengthScale = 4.5f;
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            if (shader != null &&
                (renderer.sharedMaterial == null || renderer.sharedMaterial.shader != shader))
            {
                Material material = new Material(shader);
                material.color = speedLineColor;
                if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", speedLineColor);
                renderer.material = material;
            }
        }
    }

    private void UpdateCameraFOV()
    {
        float targetFov = baseFov;

        if (gm.IsPlaying)
        {
            float speedRatio = Mathf.InverseLerp(
                gm.MinimumSpeed,
                gm.MaximumSpeed,
                gm.CurrentSpeed
            );

            speedRatio = speedRatio * speedRatio;

            targetFov =
                baseFov + speedRatio * extraFovAtMaxSpeed;
        }

        targetCamera.fieldOfView = Mathf.Lerp(
            targetCamera.fieldOfView,
            targetFov,
            1f - Mathf.Exp(
                -smoothness * Time.unscaledDeltaTime
            )
        );
    }

    private void UpdateSpeedLines()
    {
        if (speedLines == null)
            return;

        float ratio = Mathf.InverseLerp(
            linesStartSpeed,
            gm.MaximumSpeed,
            gm.CurrentSpeed
        );

        var emission = speedLines.emission;

        emission.rateOverTime = gm.IsPlaying
            ? Mathf.Lerp(0f, maxSpeedLineRate, ratio)
            : 0f;
    }

    private void UpdateCameraShake()
    {
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.unscaledDeltaTime;

            float strength = shakeStrength *
                Mathf.Clamp01(
                    shakeTimer / Mathf.Max(shakeDuration, 0.001f)
                );

            Vector3 shakeOffset = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                0f
            ) * strength;

            targetCamera.transform.localPosition =
                originalLocalPosition + shakeOffset;
        }
        else
        {
            targetCamera.transform.localPosition =
                originalLocalPosition;
        }
    }

    public void TriggerCrashShake()
    {
        shakeTimer = shakeDuration;
    }
}
