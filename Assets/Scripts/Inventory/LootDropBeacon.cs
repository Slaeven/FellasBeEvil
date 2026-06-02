using UnityEngine;

[ExecuteAlways]
public class LootDropBeacon : MonoBehaviour
{
    [Header("Beam")]
    [SerializeField] private Color goldColour = new Color(1f, 0.68f, 0.16f, 0.85f);
    [SerializeField] private float height = 2.6f;
    [SerializeField] private float radius = 0.18f;
    [SerializeField] private float particleSize = 0.18f;
    [SerializeField] private float particleSpeed = 0.75f;
    [SerializeField] private int particlesPerSecond = 34;
    [SerializeField] private Material particleMaterial;

    [Header("Glow")]
    [SerializeField] private bool addPointLight = true;
    [SerializeField] private float lightRange = 2.4f;
    [SerializeField] private float lightIntensity = 1.2f;

    private ParticleSystem particleSystemInstance;
    private ParticleSystemRenderer particleRenderer;
    private Light glowLight;
    private Material generatedParticleMaterial;


    private void Awake()
    {
        Configure();
    }

    private void OnEnable()
    {
        Configure();
    }

    private void OnValidate()
    {
        Configure();
    }

    private void Configure()
    {
        if (particleSystemInstance == null)
            particleSystemInstance = GetComponent<ParticleSystem>();

        if (particleSystemInstance == null)
            particleSystemInstance = gameObject.AddComponent<ParticleSystem>();

        if (particleRenderer == null)
            particleRenderer = GetComponent<ParticleSystemRenderer>();

        ConfigureParticles();
        ConfigureLight();
    }

    private void ConfigureParticles()
    {
        ParticleSystem.MainModule main = particleSystemInstance.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = height / Mathf.Max(0.01f, particleSpeed);
        main.startSpeed = particleSpeed;
        main.startSize = particleSize;
        main.startColor = goldColour;
        main.gravityModifier = 0f;
        main.maxParticles = Mathf.Max(64, particlesPerSecond * 4);

        ParticleSystem.EmissionModule emission = particleSystemInstance.emission;
        emission.enabled = true;
        emission.rateOverTime = particlesPerSecond;

        ParticleSystem.ShapeModule shape = particleSystemInstance.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;
        shape.arc = 360f;
        shape.rotation = new Vector3(90f, 0f, 0f);

        ParticleSystem.ColorOverLifetimeModule colourOverLifetime = particleSystemInstance.colorOverLifetime;
        colourOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(goldColour, 0f),
                new GradientColorKey(new Color(1f, 0.92f, 0.42f), 0.5f),
                new GradientColorKey(goldColour, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(goldColour.a, 0.18f),
                new GradientAlphaKey(goldColour.a * 0.65f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colourOverLifetime.color = gradient;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particleSystemInstance.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.45f),
            new Keyframe(0.25f, 1f),
            new Keyframe(1f, 0.15f)
        );
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        if (particleRenderer != null)
        {
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.sortingOrder = 10;
            particleRenderer.material = GetParticleMaterial();
        }

        if (Application.isPlaying && !particleSystemInstance.isPlaying)
            particleSystemInstance.Play();
    }

    private Material GetParticleMaterial()
    {
        if (particleMaterial != null)
            return particleMaterial;

        if (generatedParticleMaterial != null)
            return generatedParticleMaterial;

        Shader shader =
            Shader.Find("HDRP/Unlit") ??
            Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
            Shader.Find("Particles/Standard Unlit") ??
            Shader.Find("Sprites/Default");

        if (shader == null)
            return null;

        generatedParticleMaterial = new Material(shader)
        {
            name = "Generated Loot Drop Beacon Particle Material",
            color = goldColour,
            renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent
        };

        ConfigureParticleMaterial(generatedParticleMaterial);
        return generatedParticleMaterial;
    }

    private void ConfigureParticleMaterial(Material material)
    {
        if (material == null)
            return;

        material.color = goldColour;

        SetMaterialColour(material, "_BaseColor", goldColour);
        SetMaterialColour(material, "_UnlitColor", goldColour);
        SetMaterialColour(material, "_Color", goldColour);

        SetMaterialFloat(material, "_SurfaceType", 1f);
        SetMaterialFloat(material, "_BlendMode", 0f);
        SetMaterialFloat(material, "_AlphaCutoffEnable", 0f);
        SetMaterialFloat(material, "_ZWrite", 0f);
        SetMaterialFloat(material, "_CullMode", 0f);

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_BLENDMODE_ALPHA");
        material.DisableKeyword("_ALPHATEST_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private void SetMaterialColour(Material material, string propertyName, Color colour)
    {
        if (material.HasProperty(propertyName))
            material.SetColor(propertyName, colour);
    }

    private void SetMaterialFloat(Material material, string propertyName, float value)
    {
        if (material.HasProperty(propertyName))
            material.SetFloat(propertyName, value);
    }

    private void ConfigureLight()
    {
        if (!addPointLight)
        {
            if (glowLight != null)
                glowLight.enabled = false;

            return;
        }

        if (glowLight == null)
        {
            Transform lightTransform = transform.Find("Glow Light");

            if (lightTransform != null)
                glowLight = lightTransform.GetComponent<Light>();
        }

        if (glowLight == null)
        {
            GameObject lightObject = new GameObject("Glow Light");
            lightObject.transform.SetParent(transform, false);
            glowLight = lightObject.AddComponent<Light>();
        }

        glowLight.enabled = true;
        glowLight.type = LightType.Point;
        glowLight.color = goldColour;
        glowLight.range = lightRange;
        glowLight.intensity = lightIntensity;
        glowLight.transform.localPosition = Vector3.up * (height * 0.45f);
    }
}
