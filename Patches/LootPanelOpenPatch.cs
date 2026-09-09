using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Comfort.Common;
using DrakiaXYZ.LootRadius.Helpers;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace DrakiaXYZ.LootRadius.Patches
{
    public class LootPanelOpenPatch : ModulePatch
    {
        private static FieldInfo _rightPaneField;
        private static FieldInfo _containedGridsViewField;
        private static LayerMask _interactiveLayerMask = 1 << LayerMask.NameToLayer("Interactive");

        private static Stash _stash
        {
            get { return LootRadiusPlugin.RadiusStash; }
            set { LootRadiusPlugin.RadiusStash = value; }
        }

        protected override MethodBase GetTargetMethod()
        {
            // Named _rightPanelItem in 4.1. 3.11 found it by searching for the single CompoundItem[]
            // field because the name was obfuscated; ItemUiContext has since gained two more
            // CompoundItem[] members (PlayerCollections, PlayerStash), and while those are
            // properties rather than fields, matching by name is no longer a guess.
            _rightPaneField = AccessTools.Field(typeof(ItemUiContext), "_rightPanelItem");

            // containedGridsView_0 in 3.11. _simplePanel is public now, so it needs no reflection.
            _containedGridsViewField = AccessTools.Field(typeof(SearchableItemView), "_containedGridsView");

            return typeof(ItemsPanel).GetMethod(nameof(ItemsPanel.Show));
        }

        // ItemsPanel.Show grew from 4 parameters to 14 in 4.1. Harmony binds by name, so only the
        // ones actually used are declared here.
        [PatchPostfix]
        public static async void PatchPostfix(
            ItemsPanel __instance,
            Task __result,
            ItemContext sourceContext,
            CompoundItem lootItem,
            InventoryController inventoryController,
            ItemsPanel.EItemsTab currentTab,
            SortingTable sortingTable,
            UIParent ___UI
        )
        {
            // Wait for original to finish
            await __result;

            // If lootItem isn't null, don't do anything, it means there's a right hand panel already
            if (lootItem != null)
            {
                return;
            }

            if (_stash == null || !(_stash.Grid is LootRadiusStashGrid grid))
            {
                return;
            }

            Vector3 playerPosition = Singleton<GameWorld>.Instance.MainPlayer.Position;

            // First find any items directly near the player's feet, to allow them to loot things like items slightly under the floor
            Collider[] floorItemColliders = Physics.OverlapSphere(playerPosition, 0.35f, _interactiveLayerMask);
            AddAllowedItems(grid, floorItemColliders, true);

            // Then collect items around the player body, based on the loot radius
            playerPosition += (Vector3.up * 0.5f);
            Collider[] nearbyItemColliders = Physics.OverlapSphere(playerPosition, Settings.LootRadius.Value, _interactiveLayerMask);
            AddAllowedItems(grid, nearbyItemColliders, false);

            // Show the stash in the inventory panel. SimpleStashPanel.Show gained a sorting table and
            // a search-availability mode in 4.1; loot picked up off the ground is already "searched",
            // so the panel gets full availability.
            var simpleStashPanel = __instance._simpleStashPanel;
            simpleStashPanel.Show(
                _stash,
                inventoryController,
                sourceContext.CreateChild(_stash),
                true,
                sortingTable,
                SimpleStashPanel.EStashSearchAvailability.All,
                inventoryController,
                currentTab);

            ___UI.AddDisposable(simpleStashPanel);

            _rightPaneField.SetValue(ItemUiContext.Instance, new CompoundItem[] { _stash });

            var containedGridsView = _containedGridsViewField.GetValue(simpleStashPanel._simplePanel) as ContainedGridsView;
            grid.GridViews = containedGridsView.GridViews;
        }

        private static void AddAllowedItems(LootRadiusStashGrid grid, Collider[] colliders, bool ignoreLineOfSight)
        {
            foreach (Collider collider in colliders)
            {
                var item = collider.gameObject.GetComponentInParent<LootItem>();
                if (item != null && item.Item.Parent.Container.ID != grid.ID && (ignoreLineOfSight || IsLineOfSight(item.transform.position)))
                {
                    item.ItemOwner.RemoveItemEvent += grid.OwnerRemoveItemEvent;

                    grid.AddInternal(item.Item, grid.FindFreeSpace(item.Item), false, true);
                }
            }
        }

        /**
         * Return true if the end position is within line of sight of the player
         */
        private static bool IsLineOfSight(Vector3 endPos)
        {
            // Start at the player's head
            Vector3 startPos = Singleton<GameWorld>.Instance.MainPlayer.MainParts[BodyPartType.head].Position;

            // LineCast returns true if it hits a HighPolyCollider, indicating the item isn't within line of sight of the player's head
            // LayerMaskClass in 3.11.
            if (Physics.Linecast(startPos, endPos, LayersMaskController.HighPolyWithTerrainMask))
            {
                return false;
            }

            return true;
        }
    }
}
