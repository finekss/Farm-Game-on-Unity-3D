using System.Text;
using __GAME__.Source.Save;
using UnityEngine;

namespace __GAME__.Source.Features
{
    public class InventoryFeature : FeatureBase
    {
        private InventoryModel _model;
        private EventBus _bus;

        public override void Start()
        {
            _model = new InventoryModel();
            _bus = Main.Get<EventBus>();

            _bus.Subscribe<ResourceCollectedEvent>(OnResourceCollected);

            Load();
        }

        private void OnResourceCollected(ResourceCollectedEvent evt)
        {
            _model.Add(evt.Id, evt.Amount);

            int total = GetAmount(evt.Id);
            Debug.Log($"[Inventory] +{evt.Amount} {evt.Id}  →  total: {total}");
        }

        public override void OnSave()
        {
            Main.data.Inventory = _model.Save();
        }

        private void Load()
        {
            if (Main.data.Inventory != null)
                _model.Load(Main.data.Inventory);
        }

        // Количество предмета в инвентаре по id
        public int GetAmount(string id)
        {
            return _model.Items.TryGetValue(id, out var amount) ? amount : 0;
        }

        // Проверить наличие предмета
        public bool HasItem(string id, int amount = 1)
        {
            return _model.Has(id, amount);
        }

        // Удалить предмет из инвентаря
        public bool RemoveItem(string id, int amount = 1)
        {
            bool ok = _model.Remove(id, amount);
            if (ok)
                Debug.Log($"[Inventory] -{amount} {id}  →  total: {GetAmount(id)}");
            return ok;
        }

        // Добавить предмет напрямую (без события)
        public void AddItem(string id, int amount)
        {
            _model.Add(id, amount);
            Debug.Log($"[Inventory] +{amount} {id}  →  total: {GetAmount(id)}");
        }

        // Вывести всё содержимое инвентаря в консоль
        public void PrintToConsole()
        {
            if (_model.Items.Count == 0)
            {
                Debug.Log("[Inventory] Инвентарь пуст");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("[Inventory] ═══════════════════");

            int idx = 1;
            foreach (var kvp in _model.Items)
            {
                sb.AppendLine($"  {idx}. {kvp.Key}  x{kvp.Value}");
                idx++;
            }

            sb.Append("[Inventory] ═══════════════════");
            Debug.Log(sb.ToString());
        }

        // Вся модель (read-only)
        public InventoryModel Model => _model;
    }
}