using System.Collections.Generic;
namespace __GAME__.Source.Features
{
    public class InventoryModel
    {
        private Dictionary<string, int> _items = new ();
        
        public IReadOnlyDictionary<string, int> Items => _items;
        
        public void Add(string id, int amount)
        {
            _items.TryAdd(id, 0);
            _items[id] += amount;
        }

        public bool Has(string id, int amount)
        {
            return _items.TryGetValue(id, out var current) && current >= amount;
        }

        public bool Remove(string id, int amount)
        {
            if (!Has(id, amount)) return false;
            
            _items[id] -= amount;
            
            if (_items[id] <= 0) _items.Remove(id);
            return true;
        }

        public void Clear()
        {
            _items.Clear();
        }
        
        public void Load(Dictionary<string, int> data)
        {
            _items = new Dictionary<string, int>(data);
        }

        public Dictionary<string, int> Save()
        {
            return new Dictionary<string, int>(_items);
        }

    }
}