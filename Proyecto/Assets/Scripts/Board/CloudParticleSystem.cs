using UnityEngine;

namespace ReinosDelEter
{
    /// <summary>
    /// Convierte un ParticleSystem genérico en nubes suaves que flotan:
    /// partículas grandes, translúcidas, con textura circular difusa y deriva
    /// horizontal lenta (sin "cuadrados" ni disparo hacia arriba).
    /// Se configura al iniciar el juego.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class CloudParticleSystem : MonoBehaviour
    {
        [Header("Aspecto de las nubes")]
        public Color cloudColor = new Color(1f, 1f, 1f, 0.5f);
        [Tooltip("Tamaño base de cada nube (unidades de mundo)")]
        public float cloudSize = 4f;
        [Tooltip("Deriva horizontal lenta")]
        public float driftSpeed = 0.3f;
        [Tooltip("Área (X,Z) sobre la que aparecen las nubes")]
        public Vector2 spawnArea = new Vector2(10f, 10f);
        public int maxParticles = 16;
        public float spawnRate = 2f;
        public float lifetime = 9f;

        private void Start() => Configure();

        private void Configure()
        {
            var ps = GetComponent<ParticleSystem>();

            var main = ps.main;
            main.startLifetime = lifetime;
            main.startSpeed = 0f;                       // no salen disparadas
            main.gravityModifier = 0f;                  // no suben ni caen
            main.startSize = new ParticleSystem.MinMaxCurve(cloudSize * 0.7f, cloudSize * 1.4f);
            main.startColor = cloudColor;
            main.maxParticles = maxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

            var emission = ps.emission;
            emission.rateOverTime = spawnRate;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(spawnArea.x, 1f, spawnArea.y);

            // Deriva horizontal suave en X/Z, nada en Y.
            // Las 3 curvas deben compartir modo (TwoConstants) o Unity lanza
            // "Particle Velocity curves must all be in the same mode".
            var vel = ps.velocityOverLifetime;
            vel.enabled = driftSpeed > 0f;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(-driftSpeed, driftSpeed);
            vel.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.z = new ParticleSystem.MinMaxCurve(-driftSpeed, driftSpeed);

            // Aparecer y desvanecerse suavemente (sin bordes duros).
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.3f),
                    new GradientAlphaKey(1f, 0.7f),
                    new GradientAlphaKey(0f, 1f)
                });
            col.color = grad;

            // Crecen un poco a lo largo de su vida.
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            var sizeCurve = new AnimationCurve(
                new Keyframe(0f, 0.7f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0.85f));
            sol.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            ConfigureMaterial();

            ps.Clear();
            ps.Play();
        }

        /// <summary>Hace el material translúcido y le pone una textura circular suave.</summary>
        private void ConfigureMaterial()
        {
            var rend = GetComponent<ParticleSystemRenderer>();
            if (rend == null) return;

            rend.renderMode = ParticleSystemRenderMode.Billboard;
            rend.alignment = ParticleSystemRenderSpace.View;

            Material mat = rend.material; // instancia en runtime
            if (mat == null) return;

            // Forzar mezcla alfa (transparente) en el shader URP/Particles.
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);   // 1 = Transparent
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);       // 0 = Alpha
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            Texture2D soft = SoftCircleTexture();
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", soft);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", soft);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", cloudColor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", cloudColor);
        }

        private static Texture2D _softCircle;

        /// <summary>Textura blanca con alfa radial difuso (un disco suave).</summary>
        private static Texture2D SoftCircleTexture()
        {
            if (_softCircle != null) return _softCircle;

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f;

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center) / radius;
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a * (3f - 2f * a); // smoothstep → bordes suaves
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }

            tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            _softCircle = tex;
            return _softCircle;
        }
    }
}
