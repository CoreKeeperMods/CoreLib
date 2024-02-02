using HarmonyLib;
using UnityEngine;

namespace CoreLib.UserInterface.Patches
{
    public class UIManager_Patch
    {
        [HarmonyPatch(typeof(UIManager), nameof(UIManager.Init))]
        [HarmonyPostfix]
        public static void OnInit(UIManager __instance)
        {
            var UITransform = __instance.chestInventoryUI.transform.parent;

            foreach (ModUIAuthoring interfacePrefab in UserInterfaceModule.interfacePrefabs)
            {
                var interfaceGameObject = Object.Instantiate(interfacePrefab.gameObject, UITransform);
                var interfaceComponent = interfaceGameObject.GetComponent<IModUI>();
                interfaceGameObject.transform.localPosition = interfacePrefab.initialInterfacePosition;
                UserInterfaceModule.modInterfaces.Add(interfacePrefab.modInterfaceID, interfaceComponent);
            }
        }
        
        [HarmonyPatch(typeof(UIManager), nameof(UIManager.HideAllInventoryAndCraftingUI))]
        [HarmonyPostfix]
        public static void OnHide()
        {
            foreach (IModUI modUI in UserInterfaceModule.modInterfaces.Values)
            {
                modUI.HideUI();
            }

            UserInterfaceModule.ClearModUIData();
        }
        
        [HarmonyPatch(typeof(UIManager), nameof(UIManager.isAnyInventoryShowing), MethodType.Getter)]
        [HarmonyPostfix]
        public static void OnIsAnyMenuActive(ref bool __result)
        {
            if (__result) return;

            __result |= UserInterfaceModule.currentInterface != null;
        }
    }
}