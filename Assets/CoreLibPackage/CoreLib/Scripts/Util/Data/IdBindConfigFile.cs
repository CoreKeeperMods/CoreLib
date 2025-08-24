using System.Linq;
using CoreLib.Data.Configuration;
using PugMod;

namespace  CoreLib.Data
{
    /// <summary>
    /// Represents a configuration-bound implementation of the IdBind system, providing
    /// functionality for managing ID bindings within a specified range and persisting
    /// them to an external configuration file. This class is designed to integrate
    /// with configuration files for persistent storage of ID-related data.
    /// </summary>
    public class IdBindConfigFile : IdBind
    {
        /// <summary>
        /// Represents a configuration file associated with an ID binding.
        /// <see cref="Configuration.ConfigFile"/> is utilized for managing persistent configuration data,
        /// such as ID bindings, loaded from or saved to a file.
        /// </summary>
        public ConfigFile ConfigFile;

        /// Represents a configuration-bound implementation of the IdBind system, which associates and tracks identifiers using a configuration file.
        /// Provides functionality for managing ID bindings within a specified range while persisting them to an external configuration.
        public IdBindConfigFile(LoadedMod mod, string configPath, int idRangeStart, int idRangeEnd) : base(idRangeStart, idRangeEnd)
        {
            ConfigFile = new ConfigFile(configPath, true, mod);
        }

        /// Determines whether a given ID is free to use within the configured range and not already assigned in the configuration file.
        /// <param name="id">The ID to check for availability.</param>
        /// <returns>A boolean value indicating whether the specified ID is free to use (true) or not (false).</returns>
        protected override bool IsIdFree(int id)
        {
            if (ConfigFile.OrphanedEntries.Any(pair =>
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

            if (ConfigFile.Entries.Any(pair => { return (int)pair.Value.BoxedValue == id; }))
            {
                return false;
            }

            return base.IsIdFree(id);
        }

        /// Binds an item ID to a new or existing ID within a specified range and updates the configuration file.
        /// <param name="itemId">The unique identifier of the item to be bound.</param>
        /// <param name="freeId">The proposed free ID to be used for binding.</param>
        /// <returns>The new ID assigned to the item after binding.</>
        protected override int BindId(string itemId, int freeId)
        {
            int newId = ConfigFile.Bind("ID Binds", itemId, freeId).Value;
            return base.BindId(itemId, newId);
        }
    }
}