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
            string name = npc.TypeName;
            var system = ModContent.GetInstance<ArchipelagoSystem>();
            if (system.session is not null && system.session.locGroupRewardNames.ContainsKey(name)) {
                Main.NewText("ARCHI ENEMY DETECTED: " +  name);
            }
            /*
            Item item = ArchipelagoItem.ArchipelagoItem.CreateDummyItem().Item;
            Item.NewItem(new EntitySource_Death(npc, null), npc.Center, item);
            */
        } 
    }
}
