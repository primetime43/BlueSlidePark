using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Runtime loader that replaces placeholder primitive meshes with the original
/// extracted game meshes and textures from Resources/.
///
/// Attach this to the GameController. It runs in Awake() before other scripts.
/// Falls back gracefully if assets aren't found.
/// </summary>
public class OriginalAssetLoader : MonoBehaviour
{
    [Header("Player Model")]
    [SerializeField] private float playerScale = 0.1f;
    [SerializeField] private float playerYOffset = -0.4f;

    [Header("Pickup/Obstacle Scale")]
    [SerializeField] private float pickupScale = 3f;
    [SerializeField] private float obstacleScale = 1.5f;

    private void Awake()
    {
        SwapPlayerModel();
        SwapPickupMeshes();
        SwapObstacleMeshes();
    }

    /// <summary>
    /// Creates a URP Lit material with the given texture.
    /// Falls back to URP/Lit shader, then Standard.
    /// </summary>
    private Material CreateTexturedMaterial(Texture2D texture)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        if (shader == null)
            return null;

        Material mat = new Material(shader);
        mat.mainTexture = texture;
        return mat;
    }

    private void SwapPlayerModel()
    {
        GameObject macModel = Resources.Load<GameObject>("Models/MAC_MILLER_BOY");
        if (macModel == null) return;

        MeshFilter sourceMF = macModel.GetComponentInChildren<MeshFilter>();
        if (sourceMF == null || sourceMF.sharedMesh == null) return;

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        // Hide the capsule primitive
        MeshRenderer parentRenderer = player.GetComponent<MeshRenderer>();
        if (parentRenderer != null)
            parentRenderer.enabled = false;

        // Create child object for the visual model
        GameObject visual = new GameObject("MacModel");
        visual.transform.SetParent(player.transform, false);
        visual.transform.localScale = Vector3.one * playerScale;
        visual.transform.localPosition = new Vector3(0f, playerYOffset, 0f);

        MeshFilter mf = visual.AddComponent<MeshFilter>();
        mf.sharedMesh = sourceMF.sharedMesh;

        MeshRenderer mr = visual.AddComponent<MeshRenderer>();

        // Load original character texture
        Texture2D macTex = Resources.Load<Texture2D>("Textures/MAC_MILLER_TEXTURE_001");
        if (macTex != null)
        {
            mr.sharedMaterial = CreateTexturedMaterial(macTex);
        }
        else if (parentRenderer != null && parentRenderer.sharedMaterial != null)
        {
            mr.sharedMaterial = parentRenderer.sharedMaterial;
        }
    }

    private void SwapPickupMeshes()
    {
        GameObject icModel = Resources.Load<GameObject>("Models/ICE_CREAM");
        if (icModel == null) return;

        MeshFilter sourceMF = icModel.GetComponentInChildren<MeshFilter>();
        if (sourceMF == null || sourceMF.sharedMesh == null) return;

        // Load ice cream texture
        Texture2D icTex = Resources.Load<Texture2D>("Textures/ICE_CREAM");
        Material icMat = icTex != null ? CreateTexturedMaterial(icTex) : null;

        GameObject[] pickups = GameObject.FindGameObjectsWithTag("Pickup");
        foreach (var pickup in pickups)
        {
            MeshFilter mf = pickup.GetComponent<MeshFilter>();
            if (mf != null)
            {
                mf.sharedMesh = sourceMF.sharedMesh;
                pickup.transform.localScale = Vector3.one * pickupScale;

                if (icMat != null)
                {
                    MeshRenderer mr = pickup.GetComponent<MeshRenderer>();
                    if (mr != null)
                        mr.sharedMaterial = icMat;
                }
            }
        }
    }

    private void SwapObstacleMeshes()
    {
        GameObject poopModel = Resources.Load<GameObject>("Models/POOP");
        if (poopModel == null) return;

        MeshFilter sourceMF = poopModel.GetComponentInChildren<MeshFilter>();
        if (sourceMF == null || sourceMF.sharedMesh == null) return;

        // Load poop texture
        Texture2D poopTex = Resources.Load<Texture2D>("Textures/MM_POO_TEXTURE_001");
        Material poopMat = poopTex != null ? CreateTexturedMaterial(poopTex) : null;

        GameObject[] obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        foreach (var obs in obstacles)
        {
            MeshFilter mf = obs.GetComponent<MeshFilter>();
            if (mf != null)
            {
                mf.sharedMesh = sourceMF.sharedMesh;
                obs.transform.localScale = Vector3.one * obstacleScale;

                if (poopMat != null)
                {
                    MeshRenderer mr = obs.GetComponent<MeshRenderer>();
                    if (mr != null)
                        mr.sharedMaterial = poopMat;
                }
            }
        }
    }
}
