using UnityEngine;

namespace __GAME__.Source.Unity.Data
{
    [CreateAssetMenu(fileName = "New Resource", menuName = "Game/Resource")]
    public class ResourceData : ScriptableObject
    {
        [Header("Base")]
        public string id;
        public string resourceName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;

        [Header("Settings")]
        public bool stackable = true;
        public int maxStack = 99;

        [Header("Gathering")]
        [Tooltip("Предмет, получаемый при сборе (ItemData). Если null — используется id ресурса.")]
        public ItemData harvestItem;
        [Tooltip("Сколько ударов нужно для полного сбора (0 = мгновенный)")]
        public int hitsToHarvest = 0;
    }
}