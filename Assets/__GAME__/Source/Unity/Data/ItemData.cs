using UnityEngine;

namespace __GAME__.Source.Unity.Data
{
    public enum ItemType
    {
        Resource,
        Tool,
        Consumable,
        Material,
        Quest
    }

    [CreateAssetMenu(fileName = "New Item", menuName = "Game/Item")]
    public class ItemData : ScriptableObject
    {
        [Header("Base")]
        public string id;
        public string itemName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;

        [Header("Settings")]
        public ItemType itemType = ItemType.Resource;
        public bool stackable = true;
        public int maxStack = 99;

        [Header("Gathering")]
        [Tooltip("Сколько ударов нужно для полного сбора (0 = мгновенный)")]
        public int hitsToHarvest = 0;

        [Header("World")]
        [Tooltip("Префаб для спауна в мире (drop / placement)")]
        public GameObject worldPrefab;
    }
}