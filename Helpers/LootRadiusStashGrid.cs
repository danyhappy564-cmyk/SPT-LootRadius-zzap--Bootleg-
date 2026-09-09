using System;
using Comfort.Common;
using Diz.LanguageExtensions;
using EFT;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;

namespace DrakiaXYZ.LootRadius.Helpers
{
    /**
     * Custom grid implementation that doesn't do parent ownership validation, and only allows removing items
     *
     * SPT 4.1 note: this derives from Stash.StashGrid rather than the old StashGridClass, because a
     * Stash now keeps its grid in a strongly typed `_grid` field of that exact type. Deriving from
     * the plain Grid base would still fill Stash.Grids[], but would leave `_grid` pointing at the
     * factory's original grid - and the two are the same object in a real stash.
     */
    internal class LootRadiusStashGrid : Stash.StashGrid
    {
        public const string GRIDID = "67e0b18aeef9ae200b0495f0";
        public const string GRIDNAME = "lootRadiusGrid";

        public GridView[] GridViews { get; set; } = null;

        public override GridItemCollection ItemCollection { get; } = new LootRadiusGridItemCollection();

        /**
         * Stash.StashGrid only takes (template, parent), so the shape of the grid is described by a
         * throwaway Grid built with the same arguments the 3.11 version passed to its base.
         */
        public LootRadiusStashGrid(Stash parentItem)
            : base(new Grid(GRIDNAME, 10, 10, true, false, Array.Empty<ItemFilter>(), parentItem, -1), parentItem)
        {
        }

        /**
         * Don't allow moving items around in the custom grid, but allow adding new items
         */
        public override bool CheckCompatibility(Item item)
        {
            return !this.Contains(item);
        }

        /**
         * Simplified item adding, as we know the incoming data is sane. This removes any chance of accidentally changing the item address
         */
        public override OperationResult<GridAddResult> AddInternal(Item item, LocationInGrid location, bool simulate, bool ignoreRestrictions)
        {
            if (location == null)
            {
                return new NoFreeSpaceError(item, this);
            }

            if (!ignoreRestrictions && !this.CheckCompatibility(item))
            {
                return new ItemFiltersWontAllowError(item, this);
            }

            IContainerResizeResult resizeResult = default(NoContainerResizeResult);
            var newAddress = this.CreateItemAddress(location);
            if (simulate)
            {
                return new GridAddResult(this, item, newAddress, item.StackObjectsCount, resizeResult, true);
            }

            IntVec2 originalGridSize = new IntVec2(this.GridWidth, this.GridHeight);
            this.PlaceItem(item, location);
            IntVec2 newGridSize = new IntVec2(this.GridWidth, this.GridHeight);

            if (originalGridSize != newGridSize)
            {
                resizeResult = new GridResizeResult(this, originalGridSize, newGridSize);
            }

            return new GridAddResult(this, item, newAddress, item.StackObjectsCount, resizeResult, false);
        }

        /**
         * More simple item removal handling
         */
        public override OperationResult<ContainerRemoveResult> RemoveInternal(Item item, bool simulate, bool ignoreRestrictions)
        {
            if (!base.Contains(item))
            {
                return new ItemNotInGridError(item, this);
            }

            LocationInGrid locationInGrid = this.ItemCollection[item];
            if (!simulate)
            {
                // 3.11 passed `true` here; 4.1's own RemoveInternal passes false and then fixes the
                // space buffer itself. This grid never stretches and holds no overlapping items, so
                // there is no buffer to revalidate either way.
                base.RemoveItem(item, locationInGrid, false);
            }

            return new ContainerRemoveResult(item, base.CreateItemAddress(locationInGrid), simulate);
        }

        public void OwnerRemoveItemEvent(RemoveItemEventArgs args)
        {
            if (args.Status != CommandStatus.Succeed)
            {
                return;
            }

            // Child items of items in the grid, we don't want to actually remove them, let the grid handle it
            if (args.From.Container.ParentItem != args.Item)
            {
                return;
            }

            var owner = Singleton<GameWorld>.Instance.FindOwnerById(args.OwnerId);
            owner.RemoveItemEvent -= this.OwnerRemoveItemEvent;

            // If we have GridViews we can update, try to remove this item from them
            if (GridViews != null && this.ItemCollection.ContainsKey(args.Item))
            {
                var locationInGrid = this.ItemCollection[args.Item];
                var item = args.Item;
                var location = CreateItemAddress(locationInGrid);

                foreach (var gridView in GridViews)
                {
                    // GridView.OnItemRemoved is an explicit IRemoveHandler implementation in 4.1,
                    // so it is no longer reachable through the GridView reference itself.
                    IRemoveHandler handler = gridView;
                    handler.OnItemRemoved(new RemoveItemEventArgs(item, location, CommandStatus.Begin, owner));
                    handler.OnItemRemoved(new RemoveItemEventArgs(item, location, CommandStatus.Succeed, owner));
                }
            }

            this.RemoveInternal(args.Item, false, false);
        }

        /**
         * Custom grid collection that doesn't do address validation
         */
        internal class LootRadiusGridItemCollection : GridItemCollection
        {
            public override void Add(Item item, Grid grid, LocationInGrid location)
            {
                // dictionary_0 / list_0 in 3.11; both are plainly named public fields in 4.1.
                this.Items[item] = location;
                this.ItemsList.Add(item);

                if (item.CurrentAddress == null)
                {
                    item.CurrentAddress = grid.CreateItemAddress(location);
                }
            }

            public override void Remove(Item item, Grid grid)
            {
                this.Items.Remove(item);
                this.ItemsList.Remove(item);

                if (item.CurrentAddress?.Container?.ID == grid.ID)
                {
                    item.CurrentAddress = null;
                }
            }
        }
    }
}
