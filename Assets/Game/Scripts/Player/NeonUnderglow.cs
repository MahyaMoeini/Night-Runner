
using UnityEngine;

public class NeonUnderglow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Light underglowLight;
    [SerializeField] private Renderer underglowRenderer;

    [Header("Neon Settings")]
    [SerializeField]
    private Color neonColor =
        new Color(0.15f, 0.95f, 1f);

    [SerializeField]
    private Color highSpeedColor =
        new Color(0.62f, 0.22f, 1f);

    [SerializeField] private float minLightIntensity = 3f;
    [SerializeField] private float maxLightIntensity = 28f;

    [SerializeField] private float minEmission = 1.2f;
    [SerializeField] private float maxEmission = 14f;

    private Material neonMaterial;
    private Light purpleLight;
    private float smoothedRatio;
    private float ratioVelocity;

    private void Awake()
    {
        if (underglowRenderer != null)
        {
            neonMaterial = underglowRenderer.material;
            neonMaterial.EnableKeyword("_EMISSION");
            neonMaterial.color = neonColor;
            if (neonMaterial.HasProperty("_BaseColor"))
                neonMaterial.SetColor("_BaseColor", neonColor);
        }

        if (underglowLight != null)
        {
            underglowLight.color = neonColor;
            underglowLight.shadows = LightShadows.None;

            GameObject purpleObject = new GameObject("UnderglowPurple");
            purpleObject.transform.SetParent(underglowLight.transform, false);
            purpleObject.transform.localPosition = new Vector3(0f, -0.02f, -0.35f);

            purpleLight = purpleObject.AddComponent<Light>();
            purpleLight.type = LightType.Point;
            purpleLight.shadows = LightShadows.None;
            purpleLight.color = highSpeedColor;
            purpleLight.range = underglowLight.range;
        }
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;

        if (gm == null)
            return;

        float speedRatio = Mathf.InverseLerp(
            gm.MinimumSpeed,
            gm.MaximumSpeed,
            gm.CurrentSpeed
        );

        float delta = gm.IsPlaying ? Time.deltaTime : Time.unscaledDeltaTime;
        smoothedRatio = Mathf.SmoothDamp(
            smoothedRatio,
            speedRatio,
            ref ratioVelocity,
            0.45f,
            Mathf.Infinity,
            delta
        );

        // Squared curve makes high speed much brighter than a cruise.
        float punch = Mathf.Lerp(0.12f, 1f, smoothedRatio * smoothedRatio);
        Color glow = Color.Lerp(neonColor, highSpeedColor, 0.25f + smoothedRatio * 0.75f);

        if (underglowLight != null)
        {
            underglowLight.color = neonColor;
            underglowLight.intensity = Mathf.Lerp(
                minLightIntensity,
                maxLightIntensity,
                punch
            );
        }

        if (purpleLight != null)
        {
            purpleLight.intensity = Mathf.Lerp(
                minLightIntensity * 0.35f,
                maxLightIntensity * 0.85f,
                punch
            );
        }

        if (neonMaterial != null &&
            neonMaterial.HasProperty("_EmissionColor"))
        {
            float emission = Mathf.Lerp(minEmission, maxEmission, punch);
            neonMaterial.SetColor("_EmissionColor", glow * emission);
        }
    }

    private void OnDestroy()
    {
        if (neonMaterial != null)
            Destroy(neonMaterial);
    }
}
