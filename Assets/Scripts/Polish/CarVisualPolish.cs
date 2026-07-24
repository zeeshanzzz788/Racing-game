using UnityEngine;
using UnityEngine.Rendering;
using VelocityRush.Cars;

namespace VelocityRush.Polish
{
    /// <summary>URP/Lit car-paint polish without material instancing. Assign renderers or allow
    /// discovery; baked probe blending supplies reflections at mobile-safe cost.</summary>
    [RequireComponent(typeof(CarController))]
    public class CarVisualPolish : MonoBehaviour
    {
        [SerializeField] private Renderer[] carRenderers;
        [SerializeField, Range(0f, 1f)] private float metallic = .82f;
        [SerializeField, Range(0f, 1f)] private float smoothness = .86f;
        [SerializeField, Range(0f, 1f)] private float clearCoatMask = .7f;
        [SerializeField, Range(0f, 1f)] private float clearCoatSmoothness = .92f;
        [SerializeField] private Color nitroEmission = new Color(.05f, .55f, 1f);
        [SerializeField, Range(0f, 8f)] private float nitroEmissionIntensity = 2.5f;

        private CarController car;
        private MaterialPropertyBlock block;
        private static readonly int MetallicId = Shader.PropertyToID("_Metallic");
        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        private static readonly int CoatMaskId = Shader.PropertyToID("_CoatMask");
        private static readonly int CoatSmoothnessId = Shader.PropertyToID("_CoatSmoothness");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            car = GetComponent<CarController>();
            if (carRenderers == null || carRenderers.Length == 0) carRenderers = GetComponentsInChildren<Renderer>(true);
            block = new MaterialPropertyBlock();
            for (int i = 0; i < carRenderers.Length; i++)
            {
                Renderer renderer = carRenderers[i];
                if (renderer == null) continue;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
                renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
            }
        }

        private void LateUpdate()
        {
            if (car == null) return;
            Color emission = car.IsNitroActive ? nitroEmission * nitroEmissionIntensity : Color.black;
            for (int i = 0; i < carRenderers.Length; i++)
            {
                Renderer renderer = carRenderers[i];
                if (renderer == null || renderer.sharedMaterial == null) continue;
                renderer.GetPropertyBlock(block);
                block.SetFloat(MetallicId, metallic);
                block.SetFloat(SmoothnessId, smoothness);
                block.SetFloat(CoatMaskId, clearCoatMask);
                block.SetFloat(CoatSmoothnessId, clearCoatSmoothness);
                block.SetColor(EmissionColorId, emission);
                renderer.SetPropertyBlock(block);
            }
        }
    }
}
