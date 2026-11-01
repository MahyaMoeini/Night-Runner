using UnityEngine;

/// <summary>
/// Replaces placeholder coin/fuel meshes with clearer stylized visuals.
/// Does not change trigger colliders or Interactable gameplay logic.
/// </summary>
public class PickupVisual : MonoBehaviour
{
    public enum PickupType
    {
        Coin,
        Fuel
    }

    [SerializeField] private PickupType pickupType = PickupType.Coin;
    [SerializeField] private float spinSpeed = 140f;

    private Transform visualRoot;

    private void Awake()
    {
        HideExistingMeshes();
        visualRoot = new GameObject("PickupVisual").transform;
        visualRoot.SetParent(transform, false);
        visualRoot.localPosition = Vector3.zero;

        if (pickupType == PickupType.Coin)
            BuildCoin(visualRoot);
        else
            BuildFuel(visualRoot);
    }

    private void Update()
    {
        if (visualRoot == null)
            return;

        if (pickupType == PickupType.Coin)
        {
            visualRoot.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        }
        else
        {
            visualRoot.Rotate(Vector3.up, spinSpeed * 0.35f * Time.deltaTime, Space.World);
        }
    }

    private void HideExistingMeshes()
    {
        foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>(true))
        {
            renderer.enabled = false;
            Collider col = renderer.GetComponent<Collider>();
            if (col != null)
                col.enabled = false;
        }
    }

    private void BuildCoin(Transform root)
    {
        Material gold = CreateLit(
            new Color(1f, 0.82f, 0.2f),
            new Color(2.5f, 1.8f, 0.4f),
            0.7f,
            0.85f);

        Material rim = CreateLit(
            new Color(1f, 0.95f, 0.55f),
            new Color(1.8f, 1.4f, 0.5f),
            0.5f,
            0.9f);

        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "CoinDisc";
        disc.transform.SetParent(root, false);
        disc.transform.localPosition = new Vector3(0f, 1f, 0f);
        disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        disc.transform.localScale = new Vector3(0.7f, 0.08f, 0.7f);
        Apply(disc, gold);

        GameObject edge = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        edge.name = "CoinRim";
        edge.transform.SetParent(root, false);
        edge.transform.localPosition = new Vector3(0f, 1f, 0f);
        edge.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        edge.transform.localScale = new Vector3(0.78f, 0.05f, 0.78f);
        Apply(edge, rim);

        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        core.name = "CoinCore";
        core.transform.SetParent(root, false);
        core.transform.localPosition = new Vector3(0f, 1f, 0f);
        core.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        core.transform.localScale = new Vector3(0.28f, 0.09f, 0.28f);
        Apply(core, rim);

        CreateGlow(root, new Vector3(0f, 1f, 0f), new Color(1f, 0.85f, 0.3f), 1.8f, 2.5f);
    }

    private void BuildFuel(Transform root)
    {
        Material can = CreateLit(
            new Color(0.15f, 0.75f, 0.25f),
            new Color(0.2f, 1.4f, 0.35f),
            0.2f,
            0.45f);

        Material metal = CreateLit(
            new Color(0.55f, 0.55f, 0.6f),
            Color.black,
            0.8f,
            0.7f);

        Material accent = CreateLit(
            new Color(0.95f, 0.85f, 0.15f),
            new Color(1.5f, 1.2f, 0.2f),
            0.3f,
            0.6f);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "CanBody";
        body.transform.SetParent(root, false);
        body.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        body.transform.localScale = new Vector3(0.7f, 0.95f, 0.45f);
        Apply(body, can);

        GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stripe.name = "Stripe";
        stripe.transform.SetParent(root, false);
        stripe.transform.localPosition = new Vector3(0f, 0.55f, 0.23f);
        stripe.transform.localScale = new Vector3(0.72f, 0.18f, 0.04f);
        Apply(stripe, accent);

        GameObject spout = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        spout.name = "Spout";
        spout.transform.SetParent(root, false);
        spout.transform.localPosition = new Vector3(0.18f, 1.15f, 0f);
        spout.transform.localRotation = Quaternion.Euler(0f, 0f, -25f);
        spout.transform.localScale = new Vector3(0.12f, 0.22f, 0.12f);
        Apply(spout, metal);

        GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cap.name = "Cap";
        cap.transform.SetParent(root, false);
        cap.transform.localPosition = new Vector3(0.28f, 1.32f, 0f);
        cap.transform.localScale = new Vector3(0.14f, 0.06f, 0.14f);
        Apply(cap, metal);

        GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        handle.name = "Handle";
        handle.transform.SetParent(root, false);
        handle.transform.localPosition = new Vector3(-0.22f, 0.95f, 0f);
        handle.transform.localScale = new Vector3(0.08f, 0.35f, 0.08f);
        Apply(handle, metal);

        CreateGlow(root, new Vector3(0f, 0.7f, 0f), new Color(0.3f, 1f, 0.4f), 1.4f, 2.2f);
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

    private static void Apply(GameObject obj, Material material)
    {
        MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;

        Collider col = obj.GetComponent<Collider>();
        if (col != null)
            Destroy(col);
    }

    private static void CreateGlow(
        Transform parent,
        Vector3 localPos,
        Color color,
        float intensity,
        float range)
    {
        GameObject lightObj = new GameObject("PickupGlow");
        lightObj.transform.SetParent(parent, false);
        lightObj.transform.localPosition = localPos;

        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
    }
}
