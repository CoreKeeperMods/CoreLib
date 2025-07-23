using System;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodules.ModEntity
{
    [Serializable]
    public struct CraftingData
    {
        public ObjectID objectID;
        public int amount;

        public CraftingData(ObjectID objectID, int amount)
        {
            this.objectID = objectID;
            this.amount = amount;
        }
    }
}