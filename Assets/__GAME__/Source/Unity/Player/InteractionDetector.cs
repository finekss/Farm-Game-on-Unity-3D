using UnityEngine;
using __GAME__.Source.Unity.Interaction;
using __GAME__.Source.Unity.Inventory;
using __GAME__.Source.Features;

namespace __GAME__.Source.Unity.Player
{
    // Обнаруживает интерактивные объекты через SphereCast от камеры,
    // подсвечивает их Outline-шейдером и позволяет взаимодействовать.
    public class InteractionDetector : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField] private float interactionDistance = 3f;
        [SerializeField] private float sphereRadius = 0.5f;
        [SerializeField] private LayerMask interactableLayer;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        private Camera _camera;
        private IInteractable _currentHover;

        private void Start()
        {
            _camera = Camera.main;
            if (_camera == null)
                Debug.LogError("[InteractionDetector] MainCamera not found!");
        }

        private void Update()
        {
            DetectHover();
        }

        // SphereCast от камеры вперёд — ищем IInteractable в радиусе
        private void DetectHover()
        {
            if (_camera == null) return;

            Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);

            if (Physics.SphereCast(ray, sphereRadius, out RaycastHit hit, interactionDistance, interactableLayer))
            {
                float realDistance = Vector3.Distance(_camera.transform.position, hit.point);
                if (realDistance > interactionDistance)
                {
                    ClearHover();
                    return;
                }

                var interactable = hit.collider.GetComponent<IInteractable>();
                if (interactable == null)
                    interactable = hit.collider.GetComponentInParent<IInteractable>();

                if (interactable != null && interactable.CanInteract)
                {
                    if (_currentHover != interactable)
                    {
                        ClearHover();
                        _currentHover = interactable;
                        SetOutline(_currentHover, true);

                        if (showDebugLogs)
                            LogHoverInfo(interactable, realDistance);
                    }
                }
                else
                {
                    ClearHover();
                }
            }
            else
            {
                ClearHover();
            }
        }

        private void ClearHover()
        {
            if (_currentHover != null)
            {
                SetOutline(_currentHover, false);
                _currentHover = null;
            }
        }

        // Включает / выключает OutlineEffect на объекте
        private void SetOutline(IInteractable interactable, bool enable)
        {
            if (interactable is MonoBehaviour mb)
            {
                var outline = mb.GetComponent<OutlineEffect>();
                if (outline != null)
                {
                    outline.SetEnabled(enable);
                }
                else if (enable)
                {
                    outline = mb.gameObject.AddComponent<OutlineEffect>();
                    outline.SetEnabled(true);
                }
            }
        }

        // Debug-лог при наведении на объект
        private void LogHoverInfo(IInteractable interactable, float distance)
        {
            string name = interactable.DisplayName;

            if (interactable is ResourceNode resource)
            {
                var inventoryFeature = Main.Instance?.Get<InventoryFeature>();
                int inInventory = inventoryFeature?.GetAmount(resource.ItemId) ?? 0;

                Debug.Log($"[Interact] Hover: {name} | " +
                          $"Node amount: {resource.Amount} | " +
                          $"In inventory: {inInventory} | " +
                          $"Distance: {distance:F2}m");
            }
            else
            {
                Debug.Log($"[Interact] Hover: {name} | Distance: {distance:F2}m");
            }
        }

        // Попытка взаимодействия с текущим объектом
        public void TryInteract()
        {
            if (_currentHover == null) return;

            if (!_currentHover.CanInteract)
            {
                if (showDebugLogs)
                    Debug.Log($"[Interact] {_currentHover.DisplayName} — нельзя взаимодействовать");
                return;
            }

            if (showDebugLogs)
                Debug.Log($"[Interact] Взаимодействие с: {_currentHover.DisplayName}");

            _currentHover.Interact();
        }

        // Текущий объект под прицелом (для UI)
        public IInteractable CurrentTarget => _currentHover;
    }
}

