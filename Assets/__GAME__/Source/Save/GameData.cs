using System.Collections.Generic;
using UnityEngine;

namespace __GAME__.Source.Save
{
    public class GameData
    {
        public bool HasSave;

        public SerializableVector3 PlayerPosition;
        public Dictionary<string, int> Inventory { get; set; }
    }
}