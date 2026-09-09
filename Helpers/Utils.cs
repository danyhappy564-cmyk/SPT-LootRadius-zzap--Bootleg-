using Comfort.Common;
using EFT;
using EFT.Interactive;

namespace DrakiaXYZ.LootRadius.Helpers
{
    internal class Utils
    {
        public static LootItem FindLootById(string id)
        {
            // GameWorld.LootList is List<IKillable> in 4.1, so the LootItem test does the narrowing.
            foreach (var loot in Singleton<GameWorld>.Instance.LootList)
            {
                if (loot is LootItem lootItem && lootItem.ItemId == id)
                {
                    return lootItem;
                }
            }

            return null;
        }
    }
}
