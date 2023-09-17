using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace  CoreLib.Data
{
    public class IdBind
    {
        protected readonly int idRangeStart;
        protected readonly int idRangeEnd;
        protected int firstUnusedId;

        protected HashSet<int> takenIDs = new HashSet<int>();
        protected Dictionary<string, int> modIDs = new Dictionary<string, int>();

        public IReadOnlyDictionary<string, int> ModIDs => modIDs;

        public IdBind(int idRangeStart, int idRangeEnd)
        {
            this.idRangeStart = idRangeStart;
            this.idRangeEnd = idRangeEnd;
            firstUnusedId = idRangeStart;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasIndex(string itemID)
        {
            return modIDs.ContainsKey(itemID);
        }
        
        public int GetIndex(string itemID)
        {
            if (HasIndex(itemID))
            {
                return modIDs[itemID];
            }

            CoreLibMod.Log.LogWarning($"Requesting ID for {itemID}, which is not registered!");
            return 0;
        }

        public string GetStringID(int index)
        {
            if (modIDs.ContainsValue(index))
            {
                return modIDs.First(pair => pair.Value == index).Key;
            }
            
            CoreLibMod.Log.LogWarning($"Requesting string ID for index {index}, which does not exist!");
            return "missing:missing";
        }


        protected virtual bool IsIdFree(int id)
        {
            if (id < idRangeStart || id >= idRangeEnd)
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

            int itemIndex = BindId(itemId, NextFreeId());
            return itemIndex;
        }

        protected virtual int BindId(string itemId, int freeId)
        {
            takenIDs.Add(freeId);
            modIDs.Add(itemId, freeId);
            return freeId;
        }
    }
}