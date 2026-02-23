using UnityEngine;
using __GAME__.Source.Features;
using __GAME__.Source.Unity.Data;
using __GAME__.Source.Unity.Interaction;

namespace __GAME__.Source.Unity.Inventory
{
    // Ресурсный узел в мире. При взаимодействии отправляет событие сбора
    // и уничтожается (или уменьшает количество).
    public class ResourceNode : MonoBehaviour, IInteractable
    {
        [Header("Resource")]
        [SerializeField] private ItemData item;
        [SerializeField] private int amount = 1;
        [SerializeField] private bool destroyOnCollect = true;

        [Header("Visual")]
        [SerializeField] private GameObject collectVFX;

        private bool _collected;

        // ── IInteractable ──

        public string DisplayName => item != null ? item.itemName : name;
        public bool CanInteract => !_collected && amount > 0;

        // ID предмета (для запроса из инвентаря)
        public string ItemId => item != null ? item.id : string.Empty;

        // Сколько ресурса в этом узле
        public int Amount => amount;

        public void Interact()
        {
            if (!CanInteract) return;

            _collected = true;

            // Публикуем событие сбора
            var bus = Main.Instance.Get<EventBus>();
            bus.Publish(new ResourceCollectedEvent(item.id, amount));

            Debug.Log($"[ResourceNode] Collected: {item.itemName} x{amount}");

            if (collectVFX)
                Instantiate(collectVFX, transform.position, Quaternion.identity);

            if (destroyOnCollect)
                Destroy(gameObject);
        }
    }
}