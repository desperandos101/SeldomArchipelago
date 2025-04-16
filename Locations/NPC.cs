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
using Microsoft.Xna.Framework;

namespace SeldomArchipelago.Locations
{
    public class NPCLoc : GlobalNPC
    {
        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            var system = ModContent.GetInstance<ArchipelagoSystem>();
            var session = system.Session();
            string name = LocationSystem.GetNPCLocKey(npc.TypeName);
            if (session is not null && session.flagSystem.NPCRegionUnlocked(npc) && session.ArchipelagoEnemy(name) && session.locGroupRewardNames[name].Count > 0 && Main.rand.NextBool(6))
            {
                Dust.NewDust(npc.position, npc.width, npc.height, DustID.MagicMirror);
            }
        }
        public override void OnKill(NPC npc)
        {
            if (npc.lastInteraction == 255) return;
            string name = LocationSystem.GetNPCLocKey(npc.TypeName);
            var system = ModContent.GetInstance<ArchipelagoSystem>();
            var session = system.Session();
            int bannerID = Item.NPCtoBanner(npc.BannerID());
            if (!session.flagSystem.NPCRegionUnlocked(npc))
            {
                if (session.ArchipelagoEnemy(name)) NPC.killCount[bannerID] = 0;
                return;
            }
            if (session is not null && session.enemyToKillCount.TryGetValue(name, out int value)) {
                int killCount = NPC.killCount[bannerID];
                int killCeiling = value;
                if (killCount % killCeiling == 0)
                {
                    system.QueueLocationKey(name);
                }
            }
            /*
            Item item = ArchipelagoItem.ArchipelagoItem.CreateDummyItem().Item;
            Item.NewItem(new EntitySource_Death(npc, null), npc.Center, item);
            */
        } 
    }
}
