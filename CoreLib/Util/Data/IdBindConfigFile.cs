using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using CoreLib.Util.Extensions;

namespace CoreLib;

public class IdBindConfigFile : IdBind
{
    public ConfigFile configFile;

    public IdBindConfigFile(string configPath, int idRangeStart, int idRangeEnd) : base(idRangeStart, idRangeEnd)
    {
        configFile = new ConfigFile(configPath, true);
    }

    public IdBindConfigFile(string configPath, BepInPlugin ownerMetadata, int idRangeStart, int idRangeEnd) : base(idRangeStart, idRangeEnd)
    {
        configFile = new ConfigFile(configPath, true, ownerMetadata);
    }
    
    
    protected override bool IsIdFree(int id)
    {
        if (configFile.GetOrphanedEntries().Any(pair =>
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

        if (configFile.Any(pair => { return (int)pair.Value.BoxedValue == id; }))
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