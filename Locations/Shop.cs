using SeldomArchipelago.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using static SeldomArchipelago.Systems.ArchipelagoSystem;

namespace SeldomArchipelago.Locations
{
    public class Shop : GlobalNPC
    {
        public override void ModifyActiveShop(NPC npc, string shopName, Item[] items)
        {
            var session = GetSession();
            if (session is null) return;
            string shopLocKey = LocationSystem.GetShopLocName(npc.TypeName);
            if (!session.locGroupRewardNames.TryGetValue(shopLocKey, out var rewardNames)) return;
            foreach (Item item in items)
            {
                if (item is null) continue;
                if (session.shopItems.Contains(item.Name)) item.TurnToAir(); // TODO: Find out how to only pull from english Lang
            }
            int cursorNull = 0;
            for (int cursorItem = 0; cursorItem < items.Length; cursorItem++)
            {
                var item = items[cursorItem];
                if (item is not null && item.type > ItemID.None)
                {
                    if (cursorItem > cursorNull)
                    {
                        items[cursorNull] = new Item();
                        items[cursorNull].SetDefaults(item.type);
                    }
                    cursorNull++;
                }
            }
            foreach ((string, string) tuple in rewardNames)
            {
                items[cursorNull] = new Item();
                items[cursorNull].SetDefaults(ModContent.ItemType<ArchipelagoItem.ArchipelagoItem>());
                var archItem = items[cursorNull].ModItem as ArchipelagoItem.ArchipelagoItem;
                archItem.SetShopCheck(shopLocKey, tuple.Item1, tuple.Item2);
                cursorNull++;
            }
        }
    }
}
