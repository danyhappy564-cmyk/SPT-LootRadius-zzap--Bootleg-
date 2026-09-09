using System.Reflection;
using DrakiaXYZ.LootRadius.Helpers;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace DrakiaXYZ.LootRadius.Patches
{
    internal class QuestItemDragPatch : ModulePatch
    {
        private static FieldInfo _itemOwnerField;

        protected override MethodBase GetTargetMethod()
        {
            // Named _itemOwner in 4.1; 3.11 had to find it by type because it was obfuscated.
            _itemOwnerField = AccessTools.Field(typeof(GridView), "_itemOwner");

            return typeof(GridView).GetMethod(nameof(GridView.CanDrag));
        }

        [PatchPrefix]
        public static bool PatchPrefix(GridView __instance, ref bool __result, ItemContext itemContext)
        {
            // If not the RadiusStash GridView, run original
            IItemOwner gridOwner = _itemOwnerField.GetValue(__instance) as IItemOwner;
            if (gridOwner?.ID != LootRadiusStashGrid.GRIDID)
            {
                return true;
            }

            // If this is a quest item, return false
            if (itemContext.Item.QuestItem)
            {
                __result = false;
                return false;
            }

            // Otherwise allow original function to run
            return true;
        }
    }
}
