using System;
using UnityEngine;

namespace CoreLib.Equipment
{
    public class SlotInfo
    {
        public GameObject slot;
        public IEquipmentLogic logic;
        
        public Type slotType;
        public ObjectType objectType;
    }
}