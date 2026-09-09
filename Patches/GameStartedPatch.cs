using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Comfort.Common;
using DrakiaXYZ.LootRadius.Helpers;
using EFT;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;

namespace DrakiaXYZ.LootRadius.Patches
{
    public class GameStartedPatch : ModulePatch
    {
        private static Stash _stash
        {
            get { return LootRadiusPlugin.RadiusStash; }
            set { LootRadiusPlugin.RadiusStash = value; }
        }

        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));
        }

        [PatchPostfix]
        public static void PatchPostfix()
        {
            // Setup the radius stash on raid start
            if (_stash != null)
            {
                return;
            }

            foreach (var player in Singleton<GameWorld>.Instance.RegisteredPlayers)
            {
                if (player.IsAI) continue;

                // We will use a stash ID generated based on the profile ID, so it's constant, but doesn't collide with BSG's uses
                string stashId = GetProfileStashId(player.ProfileId);
                var stash = Singleton<ItemFactory>.Instance.CreateFakeStash(stashId);

                // A Stash keeps its grid in both `_grid` and `Grids[0]`, and the game itself points
                // them at the same object - see the Stash constructor. Setting only Grids would
                // leave `_grid` on the factory's original grid.
                var stashGrid = new LootRadiusStashGrid(stash);
                stash._grid = stashGrid;
                stash.Grids = new Grid[] { stashGrid };

                var itemController = new ItemController(stash, LootRadiusStashGrid.GRIDID, "Nearby Items", false, EOwnerType.Profile);
                Singleton<GameWorld>.Instance.ItemOwners.Add(itemController, default(GameWorld.ItemOwnerWorldData));

                if (player.ProfileId == GamePlayerOwner.MyPlayer.ProfileId)
                {
                    _stash = stash;
                }
            }
        }

        private static string GetProfileStashId(string profileId)
        {
            byte[] encodedProfileId = Encoding.UTF8.GetBytes(profileId);
            // SHA256Managed is obsolete; SHA256.Create is the supported form and hashes identically,
            // so existing profiles keep the same stash id.
            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(encodedProfileId);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString().Substring(0, 24).ToLower();
            }
        }
    }
}
