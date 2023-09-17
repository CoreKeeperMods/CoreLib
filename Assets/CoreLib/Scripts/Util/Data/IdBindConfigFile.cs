using System.Linq;

namespace  CoreLib.Data
{
    public class IdBindConfigFile : IdBind
    {
        public JsonConfigFile configFile;

        public IdBindConfigFile(string mod, string configPath, int idRangeStart, int idRangeEnd) : base(idRangeStart, idRangeEnd)
        {
            configFile = new JsonConfigFile(mod, configPath, true);
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
            int newId = configFile.Bind(itemId, freeId).Value;
            return base.BindId(itemId, newId);
        }
    }
}