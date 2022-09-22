using System.Collections.Generic;

namespace CoreLib.Submodules.DropTables;

public class DropTableInfo
{
    public ObjectID item;
    public bool isGuaranteed;
    public int minAmount;
    public int maxAmount;
    public float weight;

    public DropTableInfo() { }

    public DropTableInfo(ObjectID item, int amount, float weight, bool isGuaranteed = false)
    {
        this.item = item;
        this.isGuaranteed = isGuaranteed;
        this.minAmount = amount;
        this.maxAmount = amount;
        this.weight = weight;
    }
    
    public DropTableInfo(ObjectID item, int minAmount, int maxAmount, float weight, bool isGuaranteed = false)
    {
        this.item = item;
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

        LootInfo info = new LootInfo
        {
            objectID = item,
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