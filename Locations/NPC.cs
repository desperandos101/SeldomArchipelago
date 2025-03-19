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

namespace SeldomArchipelago.Locations
{
    public class NPCLoc : GlobalNPC
    {
        public override void OnKill(NPC npc)
        {
            Item item = ArchipelagoItem.ArchipelagoItem.CreateDummyItem().Item;
            Item.NewItem(new EntitySource_Death(npc, null), npc.Center, item);
        } 
    }
}
