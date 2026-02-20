using UnityEngine;

/// <summary>
/// Slide mesh generation for each segment.
///
/// Loads the original HALF_PIPE, HALF_PIPE_LEFT, HALF_PIPE_RIGHT meshes from Resources/.
/// These three meshes together form each slide segment.
/// Falls back to procedurally generated curved U-shape if models not found.
/// </summary>
public class SlideMeshGenerator : MonoBehaviour
{
    [SerializeField] private Transform groundTrans;
    [SerializeField] private Material slideMaterial;

    [Header("Fallback Procedural Mesh (used if OBJ not found)")]
    [SerializeField] private float slideWidth = 12f;
    [SerializeField] private float wallHeight = 3f;
    [SerializeField] private float segmentLength = 15f;
    [SerializeField] private int curvePoints = 24;

    [Header("Original Meshes (auto-loaded from Resources)")]
    [SerializeField] private Mesh halfPipeMesh;
    [SerializeField] private Mesh halfPipeLeftMesh;
    [SerializeField] private Mesh halfPipeRightMesh;

    [Header("Mesh Scale (original meshes are 24x12x24)")]
    [SerializeField] private float meshScale = 0.5f;

    private Material slideBlueMat;

    private Mesh TryLoadMesh(string path)
    {
        // Try loading as GameObject first (standard .obj import)
        GameObject obj = Resources.Load<GameObject>(path);
        if (obj != null)
        {
            MeshFilter mf = obj.GetComponentInChildren<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
                return mf.sharedMesh;
        }
        // Fallback: try loading mesh directly
        Mesh mesh = Resources.Load<Mesh>(path);
        if (mesh != null)
            return mesh;
        return null;
    }

    private void LoadMeshesFromResources()
    {
        if (halfPipeMesh == null)
            halfPipeMesh = TryLoadMesh("Models/HALF_PIPE");
        if (halfPipeLeftMesh == null)
            halfPipeLeftMesh = TryLoadMesh("Models/HALF_PIPE_LEFT");
        if (halfPipeRightMesh == null)
            halfPipeRightMesh = TryLoadMesh("Models/HALF_PIPE_RIGHT");

        if (halfPipeMesh != null)
            Debug.Log("[SlideMeshGenerator] Using original HALF_PIPE meshes");
        else
            Debug.LogWarning("[SlideMeshGenerator] HALF_PIPE not found, using procedural fallback");
    }

    private void Awake()
    {
        LoadMeshesFromResources();
        CreateSlideMaterial();

        foreach (Transform segment in groundTrans)
        {
            // Destroy existing flat plane children immediately so they're gone before Start()
            for (int i = segment.childCount - 1; i >= 0; i--)
                DestroyImmediate(segment.GetChild(i).gameObject);

            if (halfPipeMesh != null)
                CreateOriginalSegment(segment);
            else
                CreateCurvedSegment(segment);
        }
    }

    private void CreateSlideMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) return;

        Texture2D slideTex = Resources.Load<Texture2D>("Textures/SLIDE_TEXTURE_001");
        if (slideTex != null)
        {
            slideBlueMat = new Material(shader);
            slideBlueMat.mainTexture = slideTex;
        }
        else
        {
            slideBlueMat = new Material(shader);
            slideBlueMat.color = new Color(0.33f, 0.53f, 0.93f);
        }
    }

    private void CreateOriginalSegment(Transform parent)
    {
        // Center piece: curved surface the player rides on (has collider)
        CreateMeshPart(parent, "HalfPipe_Center", halfPipeMesh, true);
        // Left/Right pieces: visual side detail (no collider so player can ride over the edge)
        if (halfPipeLeftMesh != null)
            CreateMeshPart(parent, "HalfPipe_Left", halfPipeLeftMesh, false);
        if (halfPipeRightMesh != null)
            CreateMeshPart(parent, "HalfPipe_Right", halfPipeRightMesh, false);
    }

    private void CreateMeshPart(Transform parent, string name, Mesh mesh, bool addCollider)
    {
        GameObject meshObj = new GameObject(name);
        meshObj.transform.SetParent(parent, false);
        meshObj.transform.localScale = Vector3.one * meshScale;
        meshObj.tag = "Floor";

        MeshFilter mf = meshObj.AddComponent<MeshFilter>();
        MeshRenderer mr = meshObj.AddComponent<MeshRenderer>();
        mf.sharedMesh = mesh;
        mr.sharedMaterial = slideBlueMat != null ? slideBlueMat : slideMaterial;

        if (addCollider)
        {
            MeshCollider mc = meshObj.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
        }
    }

    /// <summary>
    /// Fallback: procedurally generate a curved U-shape slide segment.
    /// </summary>
    private void CreateCurvedSegment(Transform parent)
    {
        GameObject meshObj = new GameObject("CurvedSlide");
        meshObj.transform.SetParent(parent, false);
        meshObj.tag = "Floor";

        MeshFilter mf = meshObj.AddComponent<MeshFilter>();
        MeshRenderer mr = meshObj.AddComponent<MeshRenderer>();
        MeshCollider mc = meshObj.AddComponent<MeshCollider>();

        Mesh mesh = BuildCurvedMesh();
        mf.mesh = mesh;
        mc.sharedMesh = mesh;
        mr.sharedMaterial = slideBlueMat != null ? slideBlueMat : slideMaterial;
    }

    private Mesh BuildCurvedMesh()
    {
        Mesh mesh = new Mesh();

        int xCount = curvePoints + 1;
        int zCount = 5;
        int vertCount = xCount * zCount;

        Vector3[] verts = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];

        for (int z = 0; z < zCount; z++)
        {
            float zPos = (float)z / (zCount - 1) * segmentLength;

            for (int x = 0; x < xCount; x++)
            {
                float t = (float)x / curvePoints;
                float xPos = Mathf.Lerp(-slideWidth * 0.5f, slideWidth * 0.5f, t);

                float n = 2f * t - 1f;
                float curve = n * n * n * n;
                float yPos = wallHeight * curve;

                int idx = z * xCount + x;
                verts[idx] = new Vector3(xPos, yPos, zPos);
                uvs[idx] = new Vector2(t, zPos / segmentLength);
            }
        }

        int triCount = (xCount - 1) * (zCount - 1) * 6;
        int[] tris = new int[triCount];
        int ti = 0;

        for (int z = 0; z < zCount - 1; z++)
        {
            for (int x = 0; x < xCount - 1; x++)
            {
                int bl = z * xCount + x;
                int br = bl + 1;
                int tl = bl + xCount;
                int tr = tl + 1;

                tris[ti++] = bl;
                tris[ti++] = tl;
                tris[ti++] = br;

                tris[ti++] = br;
                tris[ti++] = tl;
                tris[ti++] = tr;
            }
        }

        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
}
