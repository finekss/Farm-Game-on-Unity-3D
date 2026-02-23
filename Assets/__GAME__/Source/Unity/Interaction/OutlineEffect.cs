using UnityEngine;

namespace __GAME__.Source.Unity.Interaction
{
    // Добавляет/убирает outline-материал на Renderer объекта.
    // Вешается на каждый интерактивный объект, который нужно подсвечивать.
    [DisallowMultipleComponent]
    public class OutlineEffect : MonoBehaviour
    {
        [Header("Outline Settings")]
        [Tooltip("Шейдер Outline (Game/Outline). Если не задан — ищется автоматически.")]
        [SerializeField] private Shader outlineShader;

        [SerializeField] private Color outlineColor = new Color(1f, 0.84f, 0f, 1f);
        [SerializeField][Range(0.001f, 0.08f)] private float outlineWidth = 0.025f;

        private Renderer _renderer;
        private Material _outlineMaterial;
        private bool _isActive;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            if (_renderer == null)
                _renderer = GetComponentInChildren<Renderer>();

            CreateOutlineMaterial();
        }

        private void CreateOutlineMaterial()
        {
            if (outlineShader == null)
                outlineShader = Shader.Find("Game/Outline");

            if (outlineShader == null)
            {
                Debug.LogWarning($"[OutlineEffect] Outline shader not found on {name}");
                return;
            }

            _outlineMaterial = new Material(outlineShader)
            {
                name = "Outline (Instance)",
                hideFlags = HideFlags.HideAndDontSave
            };

            _outlineMaterial.SetColor("_OutlineColor", outlineColor);
            _outlineMaterial.SetFloat("_OutlineWidth", outlineWidth);
        }

        // Включить/выключить подсветку
        public void SetEnabled(bool enable)
        {
            if (_renderer == null || _outlineMaterial == null) return;
            if (enable == _isActive) return;

            _isActive = enable;

            var mats = _renderer.sharedMaterials;

            if (enable)
            {
                var newMats = new Material[mats.Length + 1];
                mats.CopyTo(newMats, 0);
                newMats[mats.Length] = _outlineMaterial;
                _renderer.materials = newMats;
            }
            else
            {
                var list = new System.Collections.Generic.List<Material>(mats);
                list.RemoveAll(m => m != null && m.shader == outlineShader);
                _renderer.materials = list.ToArray();
            }
        }

        // Обновить цвет outline в рантайме
        public void SetColor(Color color)
        {
            outlineColor = color;
            if (_outlineMaterial != null)
                _outlineMaterial.SetColor("_OutlineColor", color);
        }

        // Обновить толщину outline в рантайме
        public void SetWidth(float width)
        {
            outlineWidth = width;
            if (_outlineMaterial != null)
                _outlineMaterial.SetFloat("_OutlineWidth", width);
        }

        private void OnDestroy()
        {
            if (_outlineMaterial != null)
                Destroy(_outlineMaterial);
        }
    }
}
