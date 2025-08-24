using System;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Entity
{
    /// <summary>
    /// Represents data associated with crafting operations, encapsulating the object identifier
    /// and the quantity involved in the crafting process.
    /// </summary>
    [Serializable]
    public struct CraftingData
    {
        /// <summary>
        /// Represents the unique identifier of an object used in crafting operations.
        /// </summary>
        public ObjectID objectID;

        /// <summary>
        /// The amount involved in the crafting process, representing the quantity of a specific object
        /// required or produced during crafting operations.
        /// </summary>
        public int amount;

        /// <summary>
        /// Represents data associated with crafting operations, encapsulating the identifier
        /// of the object being crafted and the quantity involved.
        /// </summary>
        public CraftingData(ObjectID objectID, int amount)
        {
            this.objectID = objectID;
            this.amount = amount;
        }
    }
}