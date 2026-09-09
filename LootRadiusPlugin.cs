using BepInEx;
using DrakiaXYZ.LootRadius.Helpers;
using DrakiaXYZ.LootRadius.Patches;
using EFT.InventoryLogic;

namespace DrakiaXYZ.LootRadius
{
    [BepInPlugin("xyz.drakia.lootradius", "DrakiaXYZ-LootRadius", "1.5.0")]
    [BepInDependency("com.SPT.core", "4.1.0")]
    public class LootRadiusPlugin : BaseUnityPlugin
    {
        // StashItemClass in 3.11.
        public static Stash RadiusStash;

        private void Awake()
        {
            Settings.Init(Config);

            new GameStartedPatch().Enable();
            new LootPanelOpenPatch().Enable();
            new LootPanelClosePatch().Enable();
            new QuestItemDragPatch().Enable();
            new LootRadiusQuickMovePatch().Enable();
        }
    }
}
