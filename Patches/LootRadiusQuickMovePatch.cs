using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using DrakiaXYZ.LootRadius.Helpers;
using EFT;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;

namespace DrakiaXYZ.LootRadius.Patches
{
    class LootRadiusQuickMovePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            // InteractionsHandlerClass in 3.11.
            return typeof(ItemManipulator).GetMethod(nameof(ItemManipulator.QuickFindAppropriatePlace));
        }

        [PatchPrefix]
        public static void PatchPrefix(ref IEnumerable<CompoundItem> targets)
        {
            // If we're not in-raid, do nothing
            if (!Singleton<GameWorld>.Instantiated)
            {
                return;
            }

            // Don't do anything if the only target isn't the loot radius grid
            if (targets.Count() != 1
                || targets.ElementAt(0).Grids?.Length == 0
                || targets.ElementAt(0).Grids?.ElementAt(0)?.ID != LootRadiusStashGrid.GRIDNAME)
            {
                return;
            }

            // Replace the targets with the default inventory only
            var defaultInventory = Singleton<GameWorld>.Instance.MainPlayer.Inventory.Equipment;
            targets = new List<CompoundItem>
            {
                defaultInventory
            };
        }
    }
}
