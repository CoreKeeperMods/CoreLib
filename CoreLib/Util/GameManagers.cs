using System;

namespace CoreLib {
    public static class GameManagers {

        public static Manager GetMainManager() {
            if (Manager._instance == null) {
                CoreLibPlugin.Logger.LogError("Could not retrieve Manager instance, has it been instantiated yet?");
                throw new System.NullReferenceException();
            }
            return Manager._instance;

        }

        public static TManager GetManager<TManager>() where TManager : ManagerBase {
            Manager mainManager = GetMainManager();

            foreach (var subManager in mainManager.allManagers) {
                try {
                    var castMng = subManager.TryCast<TManager>();
                    if (castMng.GetType() == typeof(TManager)) {
                        return castMng;
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            CoreLibPlugin.Logger.LogError($"Could not retrieve manager of type {typeof(TManager).ToString()}");
            throw new System.Exception();
        }

    }
}
