using UnityEngine;

public class PickupEffect : MonoBehaviour
{
    private ParticleSystem ps;
    private Color effectColor;

    private void Awake()
    {
        // Get color from material
        var renderer = GetComponent<MeshRenderer>();
        if (renderer != null && renderer.material != null)
            effectColor = renderer.material.color;
        else
            effectColor = Color.white;

        // Create particle system
        GameObject psObj = new GameObject("PickupParticles");
        psObj.transform.SetParent(transform, false);
        ps = psObj.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = false;
        main.startLifetime = 0.5f;
        main.startSpeed = 5f;
        main.startSize = 0.15f;
        main.startColor = effectColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 30;
        main.playOnAwake = false;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, 20)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;

        // Disable the default renderer and add a simple one
        var psRenderer = psObj.GetComponent<ParticleSystemRenderer>();
        psRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        psRenderer.material.color = effectColor;

        ps.Stop();
    }

    public void PlayEffect()
    {
        if (ps == null) return;

        // Detach particles so they persist after object is deactivated
        ps.transform.SetParent(null);
        ps.transform.position = transform.position;
        ps.Play();

        // Self-destruct after particles finish
        Destroy(ps.gameObject, 2f);
    }
}
