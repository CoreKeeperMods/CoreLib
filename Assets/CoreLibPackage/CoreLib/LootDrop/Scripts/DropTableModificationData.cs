using System.Collections.Generic;
using Pug.UnityExtensions;
using PugMod;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.LootDrop
{
    /// Represents detailed information about an item's drop configuration from a drop table.
    /// <remarks>
    /// This class encapsulates the properties and methods necessary for defining and managing
    /// an item's drop characteristics within a drop table, including its name, drop chances,
    /// quantity range, and whether it is guaranteed.
    /// </remarks>
    public class DropTableInfo
    {
        /// Represents the name of the item associated with the drop table entry.
        /// <remarks>
        /// The <c>itemName</c> field specifies the identifier or name used to reference the item in the drop table.
        /// It may interact with other systems to retrieve the corresponding object or metadata.
        /// </remarks>
        public string itemName;

        /// Indicates whether the item is guaranteed to drop from the drop table.
        /// <remarks>
        /// The <c>isGuaranteed</c> field determines if the drop table entry should always yield this item
        /// regardless of randomness or weighting. When set to <c>true</c>, the item will be included
        /// as part of a guaranteed drop list.
        /// </remarks>
        public bool isGuaranteed;

        /// Represents the minimum amount of an item that can be dropped in a loot table configuration.
        /// <remarks>
        /// The <c>minAmount</c> value defines the lower bound of the drop quantity. It is used in conjunction with the <c>maxAmount</c> property
        /// to determine the range of possible drop quantities. If the drop has a fixed amount specified, <c>minAmount</c> will be equal to <c>maxAmount</c>.
        /// </remarks>
        public int minAmount;

        /// Represents the maximum amount of an item that can be dropped in the loot table.
        /// Defines the upper bound of the range for possible drop amounts in a DropTableInfo object.
        public int maxAmount;

        /// Represents the weight assigned to an item in the drop table, determining its likelihood of being selected during loot generation.
        /// <remarks>
        /// A higher value for the weight increases the chances of the corresponding item being chosen.
        /// This is typically used in conjunction with other properties like <c>isGuaranteed</c> and <c>amount</c>
        /// to form the overall drop logic.
        /// </remarks>
        public float weight;

        /// Defines the amount of an item to be dropped in a loot table configuration.
        /// <remarks>
        /// The <c>amount</c> property sets both the <c>minAmount</c> and <c>maxAmount</c> values to the same number, effectively ensuring a fixed drop quantity.
        /// This property is useful when the drop is not intended to have a variable range and must guarantee a specific amount.
        /// </remarks>
        public int amount
        {
            get => minAmount;
            set
            {
                minAmount = value;
                maxAmount = value;
            }
        }

        /// Represents a structured model that encapsulates information about a drop table entry,
        /// including item details, quantity range, weight, and guarantee status.
        /// DropTableInfo instances are typically used to define the properties and behavior of
        /// randomized item drops in a system, specifying the item name or identifier, drop likelihood,
        /// and potential quantity range.
        public DropTableInfo() { }

        /// Represents an entry in a drop table structure, defining the properties of an item
        /// that can be dropped, including its name, drop quantity range, drop probability weight,
        /// and whether the drop is guaranteed. This class provides various constructors for initialization
        /// and methods to interact with or modify the stored drop information.
        public DropTableInfo(string itemName, int amount, float weight, bool isGuaranteed = false)
        {
            this.itemName = itemName;
            this.isGuaranteed = isGuaranteed;
            minAmount = amount;
            maxAmount = amount;
            this.weight = weight;
        }

        /// Defines a model for managing drop table entries, encapsulating attributes such as item
        /// identification, drop likelihood, quantity range, and guarantee state. This class enables
        /// structured handling of item drops and their associated properties in a drop system.
        public DropTableInfo(string itemName, int minAmount, int maxAmount, float weight, bool isGuaranteed = false)
        {
            this.itemName = itemName;
            this.isGuaranteed = isGuaranteed;
            this.minAmount = minAmount;
            this.maxAmount = maxAmount;
            this.weight = weight;
        }

        /// Encapsulates data about an item in a drop table, including properties such as the item's name,
        /// the quantity range, the drop weight, and whether the drop is guaranteed.
        /// This class is often used to define the specific configuration and behavior of item drops within a system,
        /// outlining deterministic or probabilistic entries for random generation.
        public DropTableInfo(ObjectID item, int amount, float weight, bool isGuaranteed = false)
        {
            itemName = item.ToString();
            this.isGuaranteed = isGuaranteed;
            minAmount = amount;
            maxAmount = amount;
            this.weight = weight;
        }

        /// Represents detailed information about an entry in a drop table, including the associated item's
        /// name or identifier, drop quantity parameters, weight for determining likelihood of drops,
        /// and a flag indicating whether the drop is guaranteed or not.
        /// This class is used to configure and manage the behavior of item drops in a system.
        public DropTableInfo(ObjectID item, int minAmount, int maxAmount, float weight, bool isGuaranteed = false)
        {
            itemName = item.ToString();
            this.isGuaranteed = isGuaranteed;
            this.minAmount = minAmount;
            this.maxAmount = maxAmount;
            this.weight = weight;
        }

        /// Retrieves the loot information for the current drop table entry.
        /// This method generates a LootInfo object encapsulating details such as
        /// the associated object ID, drop quantity range, guaranteed drop status,
        /// and item weight, using the properties of the drop table entry.
        /// <returns>
        /// A LootInfo object containing the compiled loot details for the drop entry.
        /// </returns>
        public LootInfo GetLootInfo()
        {
            RangeInt dropAmount = new RangeInt
            {
                max = maxAmount,
                min = minAmount
            };

            ObjectID objectID = API.Authoring.GetObjectID(itemName);
            LootDropModule.Log.LogInfo($"{itemName} is {objectID}");
            LootInfo info = new LootInfo
            {
                objectID = objectID,
                amount = dropAmount,
                isPartOfGuaranteedDrop = isGuaranteed,
                weight = weight
            };
            return info;
        }

        /// Sets the properties of the specified LootInfo instance based on the
        /// current DropTableInfo values.
        /// <param name="info">The LootInfo object to be updated.</param>
        public void SetLootInfo(LootInfo info)
        {
            RangeInt dropAmount = new RangeInt
            {
                max = maxAmount,
                min = minAmount
            };

            info.amount = dropAmount;
            info.weight = weight;
        }
    }

    /// Represents data used for modifying drop tables within the system.
    /// <remarks>
    /// This class allows the management of drop table modifications such as adding, editing, or removing items
    /// in a drop table. It is primarily utilized by the drop table modification processes.
    /// </remarks>
    public class DropTableModificationData
    {
        /// Represents a list of ObjectID instances that specify the items to be removed
        /// from a drop table's loot during a modification process.
        /// <remarks>
        /// This variable is used to track items that need to be excluded from the drops
        /// in a drop table. The removal is handled within the drop table system logic
        /// by modules such as DropTablesModule. When an item is added to this list, it
        /// indicates that the item must be removed from the associated loot tables during
        /// modifications.
        /// </remarks>
        public List<ObjectID> removeDrops = new List<ObjectID>();

        /// A collection of <see cref="DropTableInfo"/> objects representing modifications to existing drop table entries.
        /// These modifications include editing the drop settings such as item name, drop amounts, weights, and guarantee status.
        public List<DropTableInfo> editDrops = new List<DropTableInfo>();

        /// A list of DropTableInfo objects representing new drops to be added
        /// to the drop table. Each DropTableInfo contains details of the drop,
        /// such as the item name, guaranteed flag, minimum and maximum amount,
        /// weight, and more.
        public List<DropTableInfo> addDrops = new List<DropTableInfo>();
    }
}