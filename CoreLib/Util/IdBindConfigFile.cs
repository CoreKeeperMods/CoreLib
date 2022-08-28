using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using CoreLib.Util.Extensions;

namespace CoreLib;

public class IdBindConfigFile : ConfigFile
{
    private int idRangeStart;
    private int idRangeEnd;
    internal int firstUnusedId;
    
    private HashSet<int> takenIDs = new HashSet<int>();
    internal Dictionary<string, int> modIDs = new Dictionary<string, int>();

    public IdBindConfigFile(string configPath, int idRangeStart, int idRangeEnd) : base(configPath, true)
    {
        this.idRangeStart = idRangeStart;
        this.idRangeEnd = idRangeEnd;
        firstUnusedId = idRangeStart;
    }

    public IdBindConfigFile(string configPath, BepInPlugin ownerMetadata, int idRangeStart, int idRangeEnd) : base(configPath, true, ownerMetadata)
    {
        this.idRangeStart = idRangeStart;
        this.idRangeEnd = idRangeEnd;
        firstUnusedId = idRangeStart; 
    }
    
    public int GetIndex(string itemID)
    {
        if (modIDs.ContainsKey(itemID))
        {
            return modIDs[itemID];
        }

        return 0;
    }
    
    
    private bool IsIdFree(int id)
    {
        if (id < idRangeStart || id >= idRangeEnd)
        {
            return false;
        }

        if (this.GetOrphanedEntries().Any(pair =>
            {
                if (int.TryParse(pair.Value, out int value))
                {
                    return value == id;
                }

                return false;
            }))
        {
            return false;
        }

        if (this.Any(pair => { return (int)pair.Value.BoxedValue == id; }))
        {
            return false;
        }

        return !takenIDs.Contains(id);
    }

    private int NextFreeId()
    {
        if (IsIdFree(firstUnusedId))
        {
            int id = firstUnusedId;
            firstUnusedId++;
            return id;
        }
        else
        {
            while (!IsIdFree(firstUnusedId))
            {
                firstUnusedId++;
                if (firstUnusedId >= idRangeEnd)
                {
                    throw new InvalidOperationException("Reached last mod range id! Report this to CoreLib developers!");
                }
            }

            int id = firstUnusedId;
            firstUnusedId++;
            return id;
        }
    }

    public int GetNextId(string itemId)
    {
        if (modIDs.ContainsKey(itemId))
        {
            throw new ArgumentException($"Failed to bind {itemId} id: such id is already taken!");
        }
        
        int itemIndex = NextFreeId();
        itemIndex = Bind("ID Binds", itemId, itemIndex).Value;
        
        takenIDs.Add(itemIndex);
        modIDs.Add(itemId, itemIndex);
        return itemIndex;
    }
}