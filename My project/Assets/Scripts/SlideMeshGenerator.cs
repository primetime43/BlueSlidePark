using UnityEngine;

public class SlideMeshGenerator : MonoBehaviour
{
    [SerializeField] private Transform groundTrans;
    [SerializeField] private Material slideMaterial;
    [SerializeField] private float slideWidth = 12f;
    [SerializeField] private float wallHeight = 3f;
    [SerializeField] private float segmentLength = 15f;
    [SerializeField] private int curvePoints = 24;

    private void Awake()
    {
        foreach (Transform segment in groundTrans)
        {
            // Destroy existing flat plane children immediately so they're gone before Start()
            for (int i = segment.childCount - 1; i >= 0; i--)
                DestroyImmediate(segment.GetChild(i).gameObject);

            CreateCurvedSegment(segment);
        }
    }

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
        mr.material = slideMaterial;
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
                float t = (float)x / curvePoints; // 0 to 1
                float xPos = Mathf.Lerp(-slideWidth * 0.5f, slideWidth * 0.5f, t);

                // Smooth U-shape: flat bottom in the middle, curved walls rising at edges
                // Using a power curve for smoother transition
                float n = 2f * t - 1f; // -1 to 1
                float curve = n * n * n * n; // x^4 gives flatter bottom, steeper walls
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
