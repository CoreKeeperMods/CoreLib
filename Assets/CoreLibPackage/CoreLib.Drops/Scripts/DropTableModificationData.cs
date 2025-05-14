using System.Collections.Generic;
using Pug.UnityExtensions;
using PugMod;

namespace CoreLib.Drops
{
    public class DropTableInfo
    {
        public string itemName;
        public bool isGuaranteed;
        public int minAmount;
        public int maxAmount;
        public float weight;

        public int amount
        {
            get => minAmount;
            set
            {
                minAmount = value;
                maxAmount = value;
            }
        }

        public DropTableInfo() { }

        public DropTableInfo(string itemName, int amount, float weight, bool isGuaranteed = false)
        {
            this.itemName = itemName;
            this.isGuaranteed = isGuaranteed;
            minAmount = amount;
            maxAmount = amount;
            this.weight = weight;
        }
    
        public DropTableInfo(string itemName, int minAmount, int maxAmount, float weight, bool isGuaranteed = false)
        {
            this.itemName = itemName;
            this.isGuaranteed = isGuaranteed;
            this.minAmount = minAmount;
            this.maxAmount = maxAmount;
            this.weight = weight;
        }
        
        public DropTableInfo(ObjectID item, int amount, float weight, bool isGuaranteed = false)
        {
            itemName = item.ToString();
            this.isGuaranteed = isGuaranteed;
            minAmount = amount;
            maxAmount = amount;
            this.weight = weight;
        }
    
        public DropTableInfo(ObjectID item, int minAmount, int maxAmount, float weight, bool isGuaranteed = false)
        {
            itemName = item.ToString();
            this.isGuaranteed = isGuaranteed;
            this.minAmount = minAmount;
            this.maxAmount = maxAmount;
            this.weight = weight;
        }

        public LootInfo GetLootInfo()
        {
            RangeInt dropAmount = new RangeInt
            {
                max = maxAmount,
                min = minAmount
            };

            ObjectID objectID = API.Authoring.GetObjectID(itemName);
            CoreLibMod.Log.LogInfo($"{itemName} is {objectID}");
            LootInfo info = new LootInfo
            {
                objectID = objectID,
                amount = dropAmount,
                isPartOfGuaranteedDrop = isGuaranteed,
                weight = weight
            };
            return info;
        }

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

    public class DropTableModificationData
    {
        public List<ObjectID> removeDrops = new List<ObjectID>();
        public List<DropTableInfo> editDrops = new List<DropTableInfo>();
        public List<DropTableInfo> addDrops = new List<DropTableInfo>();
    }
}