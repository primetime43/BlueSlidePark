using UnityEngine;

/// <summary>
/// Tree generation matching original SlideController tree spawning from SWF.
/// Decompiled source: SlideController.as
///
/// Original: Trees spawned with 1/6 chance per piece, placed 30-100 units left/right, +10 up.
/// Original tree prefabs: TREE_001, TREE_002, TREE_003 (loaded from Resources).
///
/// This version loads the original extracted tree meshes (TREE_001/002/003.obj)
/// and uses them instead of primitive cylinders/spheres.
/// Falls back to primitives if OBJ meshes aren't available.
/// </summary>
public class TreeGenerator : MonoBehaviour
{
    [SerializeField] private Transform groundTrans;
    [SerializeField] private Material trunkMaterial;
    [SerializeField] private Material canopyMaterial;
    [SerializeField] private Material treeMaterial;

    [Header("Original values from SlideController.as")]
    [SerializeField] private float minOffsetX = 30f;
    [SerializeField] private float maxOffsetX = 100f;
    [SerializeField] private float heightOffset = 10f;
    [SerializeField] private float segmentLength = 15f;

    [Header("Tree Meshes (auto-loaded from Models/ if null)")]
    [SerializeField] private Mesh treeMesh001;
    [SerializeField] private Mesh treeMesh002;
    [SerializeField] private Mesh treeMesh003;

    private Mesh[] treeMeshes;

    private void Start()
    {
        LoadTreeMeshes();
        LoadTreeMaterial();

        if (groundTrans == null) return;

        foreach (Transform segment in groundTrans)
        {
            SpawnTreesForSegment(segment);
        }
    }

    private void LoadTreeMeshes()
    {
        // Try to load original extracted tree meshes
        if (treeMesh001 == null)
        {
            var go = Resources.Load<GameObject>("Models/TREE_001");
            if (go != null) treeMesh001 = go.GetComponentInChildren<MeshFilter>()?.sharedMesh;
        }
        if (treeMesh002 == null)
        {
            var go = Resources.Load<GameObject>("Models/TREE_002");
            if (go != null) treeMesh002 = go.GetComponentInChildren<MeshFilter>()?.sharedMesh;
        }
        if (treeMesh003 == null)
        {
            var go = Resources.Load<GameObject>("Models/TREE_003");
            if (go != null) treeMesh003 = go.GetComponentInChildren<MeshFilter>()?.sharedMesh;
        }

        // Build array of available meshes
        int count = 0;
        if (treeMesh001 != null) count++;
        if (treeMesh002 != null) count++;
        if (treeMesh003 != null) count++;

        if (count > 0)
        {
            treeMeshes = new Mesh[count];
            int i = 0;
            if (treeMesh001 != null) treeMeshes[i++] = treeMesh001;
            if (treeMesh002 != null) treeMeshes[i++] = treeMesh002;
            if (treeMesh003 != null) treeMeshes[i++] = treeMesh003;
        }
    }

    private void LoadTreeMaterial()
    {
        if (treeMaterial != null) return;

        Texture2D tex = Resources.Load<Texture2D>("Textures/MAC_TREE_TEXTURE");
        if (tex == null) return;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) return;

        treeMaterial = new Material(shader);
        treeMaterial.mainTexture = tex;
    }

    private void SpawnTreesForSegment(Transform segment)
    {
        float[] zOffsets = { segmentLength * 0.25f, segmentLength * 0.75f };
        float[] xSides = { -1f, 1f };

        foreach (float z in zOffsets)
        {
            foreach (float side in xSides)
            {
                // Original: Random(30, 100) units left or right, +10 up
                float xOffset = Random.Range(minOffsetX, maxOffsetX) * side;
                CreateTree(segment, new Vector3(xOffset, heightOffset, z));
            }
        }
    }

    private void CreateTree(Transform parent, Vector3 localPos)
    {
        // Use original tree mesh if available
        if (treeMeshes != null && treeMeshes.Length > 0)
        {
            Mesh mesh = treeMeshes[Random.Range(0, treeMeshes.Length)];
            GameObject tree = new GameObject("Tree");
            tree.transform.SetParent(parent, false);
            tree.transform.localPosition = localPos;

            MeshFilter mf = tree.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            MeshRenderer mr = tree.AddComponent<MeshRenderer>();
            if (treeMaterial != null)
                mr.sharedMaterial = treeMaterial;
            else if (canopyMaterial != null)
                mr.sharedMaterial = canopyMaterial;
            return;
        }

        // Fallback: primitive tree (cylinder trunk + sphere canopy)
        GameObject treeObj = new GameObject("Tree");
        treeObj.transform.SetParent(parent, false);
        treeObj.transform.localPosition = localPos;

        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.SetParent(treeObj.transform, false);
        trunk.transform.localPosition = new Vector3(0, 1.5f, 0);
        trunk.transform.localScale = new Vector3(0.5f, 1.5f, 0.5f);
        if (trunkMaterial != null)
            trunk.GetComponent<MeshRenderer>().material = trunkMaterial;
        Object.Destroy(trunk.GetComponent<Collider>());

        GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        canopy.name = "Canopy";
        canopy.transform.SetParent(treeObj.transform, false);
        canopy.transform.localPosition = new Vector3(0, 4f, 0);
        canopy.transform.localScale = new Vector3(2.5f, 2.5f, 2.5f);
        if (canopyMaterial != null)
            canopy.GetComponent<MeshRenderer>().material = canopyMaterial;
        Object.Destroy(canopy.GetComponent<Collider>());
    }
}
