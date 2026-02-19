using UnityEngine;

public class TreeGenerator : MonoBehaviour
{
    [SerializeField] private Transform groundTrans;
    [SerializeField] private Material trunkMaterial;
    [SerializeField] private Material canopyMaterial;
    [SerializeField] private float treeOffsetX = 10f;
    [SerializeField] private float segmentLength = 15f;

    private void Start()
    {
        if (groundTrans == null) return;

        foreach (Transform segment in groundTrans)
        {
            SpawnTreesForSegment(segment);
        }
    }

    private void SpawnTreesForSegment(Transform segment)
    {
        float[] zOffsets = { segmentLength * 0.25f, segmentLength * 0.75f };
        float[] xSides = { -treeOffsetX, treeOffsetX };

        foreach (float z in zOffsets)
        {
            foreach (float x in xSides)
            {
                CreateTree(segment, new Vector3(x, 0, z));
            }
        }
    }

    private void CreateTree(Transform parent, Vector3 localPos)
    {
        GameObject tree = new GameObject("Tree");
        tree.transform.SetParent(parent, false);
        tree.transform.localPosition = localPos;

        // Trunk (cylinder)
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.SetParent(tree.transform, false);
        trunk.transform.localPosition = new Vector3(0, 1.5f, 0);
        trunk.transform.localScale = new Vector3(0.5f, 1.5f, 0.5f);
        if (trunkMaterial != null)
            trunk.GetComponent<MeshRenderer>().material = trunkMaterial;
        Object.Destroy(trunk.GetComponent<Collider>());

        // Canopy (sphere)
        GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        canopy.name = "Canopy";
        canopy.transform.SetParent(tree.transform, false);
        canopy.transform.localPosition = new Vector3(0, 4f, 0);
        canopy.transform.localScale = new Vector3(2.5f, 2.5f, 2.5f);
        if (canopyMaterial != null)
            canopy.GetComponent<MeshRenderer>().material = canopyMaterial;
        Object.Destroy(canopy.GetComponent<Collider>());
    }
}
