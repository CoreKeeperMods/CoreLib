using System.Linq;
using CoreLib.Data.Configuration;
using PugMod;

namespace  CoreLib.Data
{
    public class IdBindConfigFile : IdBind
    {
        public ConfigFile configFile;

        public IdBindConfigFile(LoadedMod mod, string configPath, int idRangeStart, int idRangeEnd) : base(idRangeStart, idRangeEnd)
        {
            configFile = new ConfigFile(configPath, true, mod);
        }

        protected override bool IsIdFree(int id)
        {
            if (configFile.OrphanedEntries.Any(pair =>
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

            if (configFile.Entries.Any(pair => { return (int)pair.Value.BoxedValue == id; }))
            {
                return false;
            }

            return base.IsIdFree(id);
        }

        protected override int BindId(string itemId, int freeId)
        {
            int newId = configFile.Bind("ID Binds", itemId, freeId).Value;
            return base.BindId(itemId, newId);
        }
    }
}