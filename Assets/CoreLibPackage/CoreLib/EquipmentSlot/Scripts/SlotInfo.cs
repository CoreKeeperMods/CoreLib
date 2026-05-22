using System;
using CoreLib.Submodule.EquipmentSlot.Interface;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.EquipmentSlot
{
    /// Represents the information associated with an equipment slot.
    public class SlotInfo
    {
        /// Represents prefab associated with equipment slot.
        public GameObject slotPrefab;

        /// A variable that represents the implementation of custom logic for equipment slots.
        public IEquipmentLogic logic;

        /// Represents the type associated with equipment slot.
        public Type slotType;

        /// ObjectType instance assigned to equipment slot
        public ObjectType objectType;

        /// Should prefab be registered for pooling 
        public bool createPool;
    }
}