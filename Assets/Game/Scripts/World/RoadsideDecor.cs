using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dresses each pooled road tile with sidewalks, Kenney buildings,
/// trees, bushes, and lamps. Built once per tile.
/// </summary>
public class RoadsideDecor : MonoBehaviour
{
    [Header("Materials")]
    [SerializeField] private Material buildingMaterial;
    [SerializeField] private Material windowMaterial;
    [SerializeField] private Material propMaterial;

    [Header("Layout")]
    [SerializeField] private float roadHalfWidth = 5.5f;
    [SerializeField] private float segmentLength = 30f;
    [SerializeField] private int buildingsPerSide = 3;

    private static bool modelsLoaded;
    private static GameObject[] buildingModels = System.Array.Empty<GameObject>();
    private static GameObject[] treeModels = System.Array.Empty<GameObject>();
    private static GameObject[] bushModels = System.Array.Empty<GameObject>();
    private static GameObject lampModel;
    private static GameObject fenceModel;
    private static Shader urpShader;
    private static readonly Dictionary<EntityId, Material> nightMaterials = new Dictionary<EntityId, Material>();

    private Material sidewalkMaterial;
    private Material shoulderMaterial;
    private Material curbMaterial;
    private bool built;

    private void Awake()
    {
        BuildIfNeeded();
    }

    private void OnEnable()
    {
        BuildIfNeeded();
    }

    private void BuildIfNeeded()
    {
        if (built)
            return;

        built = true;
        EnsureMaterials();
        LoadModels();

        Transform root = new GameObject("CityDecor").transform;
        root.SetParent(transform, false);
        root.localPosition = Vector3.zero;

        BuildSide(root, -1);
        BuildSide(root, 1);
    }

    private void EnsureMaterials()
    {
        if (buildingMaterial == null)
        {
            buildingMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            buildingMaterial.color = new Color(0.08f, 0.1f, 0.18f);
        }

        if (windowMaterial == null)
        {
            windowMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            windowMaterial.EnableKeyword("_EMISSION");
            windowMaterial.color = new Color(0.15f, 0.85f, 1f);
            windowMaterial.SetColor("_EmissionColor", new Color(0.2f, 1.4f, 2.2f));
        }

        if (propMaterial == null)
        {
            propMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            propMaterial.EnableKeyword("_EMISSION");
            propMaterial.color = new Color(0.7f, 0.25f, 1f);
            propMaterial.SetColor("_EmissionColor", new Color(1.6f, 0.5f, 2.8f));
        }

        sidewalkMaterial = CreateLit(new Color(0.16f, 0.17f, 0.2f), 0.04f);
        shoulderMaterial = CreateLit(new Color(0.05f, 0.09f, 0.06f), 0.02f);
        curbMaterial = CreateLit(new Color(0.28f, 0.3f, 0.34f), 0.08f);
    }

    private static Material CreateLit(Color color, float emission)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        Material mat = new Material(shader);
        mat.color = color;
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", 0.22f);
        if (emission > 0f)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * emission);
        }

        return mat;
    }

    private static void LoadModels()
    {
        if (modelsLoaded)
            return;

        modelsLoaded = true;
        buildingModels = Load(
            "house_type01",
            "house_type05",
            "house_type12",
            "house_type15",
            "low_buildingC",
            "low_buildingF",
            "low_buildingN",
            "large_buildingB",
            "skyscraperA",
            "skyscraperB"
        );
        treeModels = Load(
            "tree_oak",
            "tree_default",
            "tree_pineDefaultA",
            "tree_tall",
            "tree_small"
        );
        bushModels = Load("plant_bush", "plant_bushLarge");

        GameObject[] lamps = Load("light_curved");
        lampModel = lamps.Length > 0 ? lamps[0] : null;

        GameObject[] fences = Load("fence_short");
        fenceModel = fences.Length > 0 ? fences[0] : null;
    }

    private static GameObject[] Load(params string[] names)
    {
        var found = new List<GameObject>();
        foreach (string name in names)
        {
            GameObject model = Resources.Load<GameObject>("Roadside/" + name);
            if (model != null)
                found.Add(model);
        }

        return found.ToArray();
    }

    private void BuildSide(Transform root, int side)
    {
        CreateRibbon(root, side, "Shoulder", 8.6f, 14f, 0.03f, 0.06f, shoulderMaterial);
        CreateRibbon(root, side, "Sidewalk", 5.15f, 3.5f, 0.09f, 0.1f, sidewalkMaterial);
        CreateRibbon(root, side, "Curb", 5.02f, 0.22f, 0.16f, 0.16f, curbMaterial);
        CreateRibbon(
            root,
            side,
            "NeonEdge",
            5.28f,
            0.08f,
            0.24f,
            0.05f,
            side > 0 ? windowMaterial : propMaterial
        );

        if (buildingModels.Length == 0)
        {
            BuildProceduralSide(root, side);
            return;
        }

        int count = Mathf.Max(2, buildingsPerSide);
        float slot = segmentLength / count;

        for (int i = 0; i < count; i++)
        {
            float z = 1.1f + i * slot;
            int pick = (i + (side < 0 ? 3 : 0)) % buildingModels.Length;
            GameObject building = buildingModels[pick];
            PlaceModel(
                root,
                building,
                side,
                z,
                HeightFor(building, i),
                slot * 0.82f,
                4.6f,
                true
            );

            if (treeModels.Length > 0)
            {
                GameObject tree = treeModels[(i * 2 + (side > 0 ? 1 : 0)) % treeModels.Length];
                PlaceModel(
                    root,
                    tree,
                    side,
                    z + slot * 0.48f,
                    3.2f + (i % 3) * 0.85f,
                    3.4f,
                    0.55f,
                    true
                );
            }

            if (bushModels.Length > 0)
            {
                GameObject bush = bushModels[i % bushModels.Length];
                PlaceModel(root, bush, side, z + 0.4f, 0.85f, 1.4f, 0.2f, true);
            }
        }

        if (lampModel != null)
        {
            PlaceModel(root, lampModel, side, 7f, 4.8f, 4.8f, 1.15f, false);
            PlaceModel(root, lampModel, side, 22f, 4.8f, 4.8f, 1.15f, false);
        }

        if (fenceModel != null)
        {
            PlaceModel(root, fenceModel, side, 4f, 1.05f, 2.4f, 2.4f, true);
            PlaceModel(root, fenceModel, side, 16f, 1.05f, 2.4f, 2.4f, true);
        }
    }

    private static float HeightFor(GameObject model, int index)
    {
        string name = model.name.ToLower();
        if (name.Contains("skyscraper"))
            return 13f + (index % 2) * 3f;

        if (name.Contains("large_"))
            return 8.5f;

        if (name.Contains("house"))
            return 4.8f + (index % 3) * 0.55f;

        return 5.5f + (index % 2) * 1.2f;
    }

    private void PlaceModel(
        Transform root,
        GameObject prefab,
        int side,
        float z,
        float targetSize,
        float maxDepth,
        float xInset,
        bool fitHeight)
    {
        if (z > segmentLength - 0.8f)
            return;

        GameObject go = Instantiate(prefab, root);
        go.name = prefab.name;
        StripColliders(go);
        ConvertMaterials(go);

        float yaw = side > 0 ? -90f : 90f;
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one;

        Bounds before = LocalBounds(go);
        float basis = fitHeight
            ? before.size.y
            : Mathf.Max(before.size.x, before.size.y, before.size.z);
        float scale = targetSize / Mathf.Max(0.05f, basis);
        go.transform.localScale = Vector3.one * scale;

        Bounds sized = LocalBounds(go);
        if (sized.size.z > maxDepth && sized.size.z > 0.05f)
        {
            scale *= maxDepth / sized.size.z;
            go.transform.localScale = Vector3.one * scale;
            sized = LocalBounds(go);
        }

        float y = 0.12f - sized.min.y;
        float inner = side > 0 ? sized.min.x : sized.max.x;
        float desiredInner = side * (roadHalfWidth + xInset);
        float x = desiredInner - inner;
        float zPos = z - sized.min.z;
        go.transform.localPosition = new Vector3(x, y, zPos);
    }

    private static Bounds LocalBounds(GameObject go)
    {
        Transform space = go.transform.parent;
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0 || space == null)
            return new Bounds(Vector3.zero, Vector3.one);

        bool started = false;
        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);

        foreach (Renderer renderer in renderers)
        {
            Bounds world = renderer.bounds;
            Vector3 center = world.center;
            Vector3 extents = world.extents;

            for (int ix = -1; ix <= 1; ix += 2)
            {
                for (int iy = -1; iy <= 1; iy += 2)
                {
                    for (int iz = -1; iz <= 1; iz += 2)
                    {
                        Vector3 corner = center + Vector3.Scale(
                            extents,
                            new Vector3(ix, iy, iz)
                        );
                        Vector3 local = space.InverseTransformPoint(corner);
                        if (!started)
                        {
                            bounds = new Bounds(local, Vector3.zero);
                            started = true;
                        }
                        else
                        {
                            bounds.Encapsulate(local);
                        }
                    }
                }
            }
        }

        return bounds;
    }

    private static void ConvertMaterials(GameObject go)
    {
        if (urpShader == null)
            urpShader = Shader.Find("Universal Render Pipeline/Lit");

        if (urpShader == null)
            return;

        foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>(true))
        {
            Material[] source = renderer.sharedMaterials;
            var converted = new Material[source.Length];
            for (int i = 0; i < source.Length; i++)
                converted[i] = ToNightMaterial(source[i]);

            renderer.sharedMaterials = converted;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }
    }

    private static Material ToNightMaterial(Material source)
    {
        if (source == null || urpShader == null)
            return source;

        EntityId id = source.GetEntityId();
        if (nightMaterials.TryGetValue(id, out Material cached) && cached != null)
            return cached;

        Color color = Color.white;
        if (source.HasProperty("_Color"))
            color = source.color;
        else if (source.HasProperty("_BaseColor"))
            color = source.GetColor("_BaseColor");

        if (color.r > 0.95f && color.g > 0.95f && color.b > 0.95f)
            color = ColorFromName(source.name);

        float max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
        float min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
        float sat = max <= 0.001f ? 0f : (max - min) / max;
        bool paleWindow = max > 0.7f && sat < 0.28f;
        bool warmLight = color.r > 0.75f && color.g > 0.55f && color.b < 0.4f;
        bool coolLight = color.b > 0.55f &&
                         color.b > color.g + 0.08f &&
                         color.b > color.r;
        bool window = paleWindow || warmLight || coolLight;

        Color baseColor = window
            ? color
            : Color.Lerp(new Color(0.06f, 0.07f, 0.11f), color, 0.82f);

        Material mat = new Material(urpShader);
        mat.name = source.name + "_Night";
        mat.color = baseColor;
        mat.SetColor("_BaseColor", baseColor);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", baseColor * (window ? 1.5f : 0.18f));
        mat.SetFloat("_Smoothness", window ? 0.5f : 0.25f);
        mat.SetFloat("_Metallic", 0.02f);

        nightMaterials[id] = mat;
        return mat;
    }

    private static Color ColorFromName(string materialName)
    {
        string name = materialName.ToLower();
        if (name.Contains("leaf") || name.Contains("green"))
            return new Color(0.18f, 0.55f, 0.28f);
        if (name.Contains("wood") || name.Contains("bark") || name.Contains("trunk"))
            return new Color(0.42f, 0.26f, 0.14f);
        if (name.Contains("window") || name.Contains("glass"))
            return new Color(0.65f, 0.88f, 1f);
        if (name.Contains("roof"))
            return new Color(0.28f, 0.16f, 0.32f);

        return new Color(0.32f, 0.34f, 0.42f);
    }

    private void BuildProceduralSide(Transform root, int side)
    {
        int count = Mathf.Max(2, buildingsPerSide);
        float slot = segmentLength / count;

        for (int i = 0; i < count; i++)
        {
            float z = 2f + i * slot;
            float height = 5f + ((i + (side > 0 ? 1 : 0)) % 4) * 2.4f;
            float width = 3.2f + (i % 2) * 1.1f;
            float depth = 3.4f;

            GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
            building.name = side < 0 ? "Building_L" : "Building_R";
            building.transform.SetParent(root, false);
            building.transform.localPosition = new Vector3(
                side * (roadHalfWidth + 4.2f + width * 0.5f),
                height * 0.5f,
                z
            );
            building.transform.localScale = new Vector3(width, height, depth);
            ApplyMaterial(building, buildingMaterial);
            DisableCollider(building);

            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "TreeTrunk";
            trunk.transform.SetParent(root, false);
            trunk.transform.localPosition = new Vector3(
                side * (roadHalfWidth + 1.3f),
                1.1f,
                z + slot * 0.45f
            );
            trunk.transform.localScale = new Vector3(0.28f, 1.1f, 0.28f);
            ApplyMaterial(trunk, buildingMaterial);
            DisableCollider(trunk);

            GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crown.name = "TreeCrown";
            crown.transform.SetParent(root, false);
            crown.transform.localPosition = trunk.transform.localPosition + new Vector3(0f, 1.8f, 0f);
            crown.transform.localScale = new Vector3(1.8f, 2.1f, 1.8f);
            ApplyMaterial(crown, shoulderMaterial);
            DisableCollider(crown);
        }
    }

    private void CreateRibbon(
        Transform root,
        int side,
        string name,
        float inner,
        float width,
        float y,
        float height,
        Material material)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(root, false);
        box.transform.localPosition = new Vector3(
            side * (inner + width * 0.5f),
            y,
            segmentLength * 0.5f
        );
        box.transform.localScale = new Vector3(width, height, segmentLength);
        ApplyMaterial(box, material);
        DisableCollider(box);
    }

    private static void StripColliders(GameObject go)
    {
        Collider[] colliders = go.GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
            Destroy(collider);
    }

    private static void ApplyMaterial(GameObject obj, Material material)
    {
        if (material == null)
            return;

        MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;
    }

    private static void DisableCollider(GameObject obj)
    {
        Collider col = obj.GetComponent<Collider>();
        if (col != null)
            Destroy(col);
    }
}
