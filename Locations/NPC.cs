using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using SeldomArchipelago.ArchipelagoItem;
using SeldomArchipelago.Systems;

namespace SeldomArchipelago.Locations
{
    public class NPCLoc : GlobalNPC
    {
        public override void OnKill(NPC npc)
        {
            if (npc.lastInteraction == 255) return;
            string name = npc.TypeName;
            var session = ModContent.GetInstance<ArchipelagoSystem>().Session;
            if (session is not null && session.enemyToKillCount.ContainsKey(name)) {
                int bannerID = Item.NPCtoBanner(npc.BannerID());
                int killCount = NPC.killCount[bannerID];
                Main.NewText($"{name}: {killCount}");
                int killCeiling = session.enemyToKillCount[name];
                if (killCount % killCeiling == 0)
                {
                    Main.NewText($"{name}: {killCeiling} batch killed!");
                    Item item = ArchipelagoItem.ArchipelagoItem.CreateItem(name).Item;
                    Item.NewItem(new EntitySource_Death(npc, null), npc.Center, item);
                }
            }
            /*
            Item item = ArchipelagoItem.ArchipelagoItem.CreateDummyItem().Item;
            Item.NewItem(new EntitySource_Death(npc, null), npc.Center, item);
            */
        } 
    }
}
