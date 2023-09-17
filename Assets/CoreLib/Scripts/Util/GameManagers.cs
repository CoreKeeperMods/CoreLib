using System;

namespace CoreLib.Util
{
    public static class GameManagers {

        public static Manager GetMainManager() {
            if (Manager.main == null) {
                CoreLibMod.Log.LogError("Could not retrieve Manager instance, has it been instantiated yet?");
                throw new NullReferenceException();
            }
            return Manager.main;

        }

        public static TManager GetManager<TManager>() where TManager : ManagerBase {
            Manager mainManager = GetMainManager();

            foreach (var subManager in mainManager.allManagers) {
                try {
                    if (subManager is TManager castMng) {
                        return castMng;
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            CoreLibMod.Log.LogError($"Could not retrieve manager of type {typeof(TManager).ToString()}");
            throw new Exception();
        }

    }
}