using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

// ReSharper disable once CheckNamespace
namespace CoreLib.Data
{
    /// <summary>
    /// Represents a binding system for managing unique ID allocations within a specified range.
    /// Provides functionality to track and allocate IDs to specific items, ensuring that
    /// IDs remain unique and avoid conflicts. It supports binding IDs to string identifiers,
    /// checking for existing bindings, and retrieving corresponding IDs or string identifiers.
    /// </summary>
    public class IdBind
    {
        /// <summary>
        /// The starting value for the allowed range of IDs.
        /// This field specifies the lower boundary of the ID range
        /// that this instance can allocate or use. IDs less than this
        /// value are considered out of range and invalid for binding.
        /// </summary>
        protected readonly int IDRangeStart;

        /// Specifies the upper bound of the permissible ID range.
        /// This value defines the exclusive maximum for ID assignments within this range.
        /// Any IDs equal to or exceeding this value are deemed invalid and cannot be assigned.
        /// The range defined by `idRangeStart` and `idRangeEnd` ensures that IDs are
        /// allocated within a specific segment, providing better organization and avoiding conflicts.
        /// Typically used in conjunction with ID management operations, such as determining
        /// if there's still room for new IDs or validating that an ID falls within the allowable range.
        protected readonly int IDRangeEnd;

        /// Represents the first available unused ID within the defined ID range for the current instance of the class.
        /// This variable is initialized to the start of the ID range and is incremented as new IDs are assigned.
        /// It is used to track the next free ID and ensures proper allocation without overlaps.
        /// The value of this variable is updated dynamically as IDs are assigned or checked.
        protected int FirstUnusedId;

        /// <summary>
        /// A set of integers representing IDs that have been allocated and are currently in use.
        /// This set is used for tracking and preventing reuse of IDs within a defined range.
        /// </summary>
        protected HashSet<int> TakenIDs = new HashSet<int>();

        /// <summary>
        /// A dictionary used to map unique string identifiers (keys) to their corresponding
        /// integer IDs (values) within the specified range of IDs.
        /// </summary>
        /// <remarks>
        /// This variable serves as the internal storage for the mapping of item IDs to integer
        /// indices. It ensures quick lookup, addition, and verification of associations between
        /// string identifiers and their respective numeric representations.
        /// </remarks>
        protected Dictionary<string, int> ModIDs = new Dictionary<string, int>();
        
        /// Represents a structure for managing unique identifiers within a specified range.
        /// Provides functionality to associate and track custom string identifiers with their respective integer indices.
        /// Ensures IDs within the specified range are unique and non-conflicting.
        public IdBind(int idRangeStart, int idRangeEnd)
        {
            IDRangeStart = idRangeStart;
            IDRangeEnd = idRangeEnd;
            FirstUnusedId = idRangeStart;
        }

        /// Checks if the specified item ID has an associated index in the internal dictionary.
        /// Returns true if the item ID is present; otherwise, returns false.
        /// <param name="itemID">The string identifier of the item to check for association with an index.</param>
        /// <returns>True if the specified item ID has an associated index; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasIndex(string itemID)
        {
            return ModIDs.ContainsKey(itemID);
        }

        /// Retrieves the index associated with the specified item ID.
        /// If the given item ID has been registered, its associated index is returned.
        /// If the item ID is not registered, a warning is logged and the method returns 0.
        /// <param name="itemID">The string identifier of the item for which the index is requested.</param>
        /// <returns>The index associated with the specified item ID if registered; otherwise, 0.</returns>
        public int GetIndex(string itemID)
        {
            if (HasIndex(itemID))
            {
                return ModIDs[itemID];
            }

            CoreLibMod.Log.LogWarning($"Requesting ID for {itemID}, which is not registered!");
            return 0;
        }

        /// Retrieves the string ID associated with the specified index.
        /// This method looks up the dictionary of stored mod IDs to find the corresponding key
        /// based on the provided integer `index`. If no associated string ID exists for the specified index,
        /// a warning is logged, and a default value is returned.
        /// <param name="index">The integer index whose associated string ID needs to be retrieved.</param>
        /// <returns>The string ID corresponding to the specified index. If the index does not exist, returns "missing:missing".</returns>
        public string GetStringID(int index)
        {
            if (ModIDs.ContainsValue(index))
            {
                return ModIDs.First(pair => pair.Value == index).Key;
            }
            
            CoreLibMod.Log.LogWarning($"Requesting string ID for index {index}, which does not exist!");
            return "missing:missing";
        }


        /// Determines whether the given ID is available for use within the specified ID range.
        /// The method checks if the ID falls within the predefined range (`idRangeStart` to `idRangeEnd`)
        /// and ensures it has not already been taken.
        /// <param name="id">The ID to check for availability.</param>
        /// <returns>True if the ID is free and available for use; otherwise, false.</returns>
        protected virtual bool IsIdFree(int id)
        {
            if (id < IDRangeStart || id >= IDRangeEnd)
            {
                return false;
            }

            return !TakenIDs.Contains(id);
        }

        /// Determines the next available free ID within the specified ID range.
        /// The method increments the `firstUnusedId` field to locate a free ID that has not been taken
        /// and ensures that the ID is within the valid range as defined by `idRangeStart` and `idRangeEnd`.
        /// If no IDs are available, an exception is thrown to indicate that the ID range has been exhausted.
        /// <returns>The next free ID within the specified range.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if no free IDs remain within the specified ID range.
        /// </exception>
        private int NextFreeId()
        {
            if (IsIdFree(FirstUnusedId))
            {
                int id = FirstUnusedId;
                FirstUnusedId++;
                return id;
            }
            else
            {
                while (!IsIdFree(FirstUnusedId))
                {
                    FirstUnusedId++;
                    if (FirstUnusedId >= IDRangeEnd)
                    {
                        throw new InvalidOperationException("Reached last mod range id! Report this to CoreLib developers!");
                    }
                }

                int id = FirstUnusedId;
                FirstUnusedId++;
                return id;
            }
        }

        /// <summary>
        /// Retrieves the next available unique identifier for the specified item ID and binds it.
        /// </summary>
        /// <param name="itemId">The unique identifier of the item to bind to the next free ID.</param>
        /// <returns>The newly assigned ID as an integer.</returns>
        /// <exception cref="ArgumentException">Thrown if the specified item ID is already in use.</exception>
        public int GetNextId(string itemId)
        {
            if (ModIDs.ContainsKey(itemId))
            {
                throw new ArgumentException($"Failed to bind {itemId} id: such id is already taken!");
            }

            int itemIndex = BindId(itemId, NextFreeId());
            return itemIndex;
        }

        /// Binds a given item ID to a specific free ID within the defined range and returns the assigned ID.
        /// <param name="itemId">The unique identifier of the item to be bound.</param>
        /// <param name="freeId">An available ID that will be associated with the specified item ID.</param>
        /// <return>The ID that was successfully bound to the specified item ID.</return>
        protected virtual int BindId(string itemId, int freeId)
        {
            TakenIDs.Add(freeId);
            ModIDs.Add(itemId, freeId);
            return freeId;
        }
    }
}