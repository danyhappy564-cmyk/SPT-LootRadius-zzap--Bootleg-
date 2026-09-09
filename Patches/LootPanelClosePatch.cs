using System.Linq;
using System.Reflection;
using Comfort.Common;
using DrakiaXYZ.LootRadius.Helpers;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using SPT.Reflection.Patching;

namespace DrakiaXYZ.LootRadius.Patches
{
    /**
     * SPT 4.1 note: ItemsPanel no longer declares Close(). Closing now runs the inherited
     * UIElement.Close(), which disposes the panel's UI list - that disposal is what sets the
     * panel's own _wasClosed flag. So the hook is the base method plus a type test.
     *
     * The alternative was the compiler-generated ItemsPanel.CG_method_1, which is precisely "the
     * panel closed" but whose name is an index-based deobfuscation artefact that moves between
     * builds. UIElement.Close is a real method on a real type and will not.
     */
    public class LootPanelClosePatch : ModulePatch
    {
        private static Stash _stash
        {
            get { return LootRadiusPlugin.RadiusStash; }
            set { LootRadiusPlugin.RadiusStash = value; }
        }

        protected override MethodBase GetTargetMethod()
        {
            return typeof(UIElement).GetMethod(nameof(UIElement.Close));
        }

        [PatchPostfix]
        public static void PatchPostfix(UIElement __instance)
        {
            // Every UI element in the game runs through here, so bail immediately on anything else.
            if (!(__instance is ItemsPanel))
            {
                return;
            }

            if (_stash == null)
            {
                return;
            }

            if (!(_stash.Grid is LootRadiusStashGrid grid))
            {
                return;
            }

            foreach (var item in grid.ItemCollection.Keys.ToList())
            {
                // If the item is actually inside the radius grid, toss it
                if (item.CurrentAddress?.Container.ID == grid.ID)
                {
                    var player = Singleton<GameWorld>.Instance.MainPlayer;
                    item.CurrentAddress = player.InventoryController.CreateItemAddress();
                    player.InventoryController.ThrowItem(item, true);
                }
            }

            // Clear all the items from the loot radius grid
            grid.RemoveAll();
            grid.GridViews = null;
        }
    }
}
