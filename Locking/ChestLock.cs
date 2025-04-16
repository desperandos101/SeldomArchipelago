using Microsoft.Xna.Framework;
using SeldomArchipelago.Systems;
using System;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using Terraria.ObjectData;
using SeldomArchipelago.ArchipelagoItem;
using static SeldomArchipelago.Systems.ArchipelagoSystem;


namespace SeldomArchipelago.Locking
{
    public class ChestLock : GlobalTile
	{
        public override void RightClick(int i, int j, int type)
        {
            Terraria.Tile tile = Main.tile[i, j];
            int left = i;
			int top = j;
			if (tile.TileFrameX % 36 != 0) {
				left--;
			}

			if (tile.TileFrameY != 0) {
				top--;
			}

			FlagID? flag = FlagSystem.GetChestRegion(left, top);
			if (flag is null) return;
            var session = ModContent.GetInstance<ArchipelagoSystem>().Session();

            bool chestUnlocked = session.flagSystem.FlagIsActive((FlagID)flag);
            if (!chestUnlocked)
            {
                Main.playerInventory = false;
                Main.NewText("You need to unlock this chest's biome before opening it!");
                return;
            }

            int chestID = Chest.FindChestByGuessing(left, top);
			if (chestID == -1) throw new Exception($"Chest couldn't be found at X={left} Y={top}, despite being identified as belonging to flag {flag}.");
			Chest chest = Main.chest[chestID];

            if (chest.item[0].ModItem is ArchipelagoItem.ArchipelagoItem archItem)
            {
                archItem.SetCheck();
            }
        }
    }
}