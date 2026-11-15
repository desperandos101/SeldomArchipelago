using System.Collections.Generic;
using SeldomDespArchipelago.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SeldomDespArchipelago.NPCs
{
    public class ArchipelagoGlobalNPC : GlobalNPC
    {
        public override void OnKill(NPC npc)
        {
            // Holiday mini-bosses must be manually sent if their respective moon is not active
            var system = ModContent.GetInstance<ArchipelagoSystem>();
            if (system.ActiveSlot() is not null)
            {
                if (!Main.pumpkinMoon)
                {
                    switch (npc.type)
                    {
                        case NPCID.MourningWood: system.QueueLocation("Mourning Wood"); break;
                        case NPCID.Pumpking: system.QueueLocation("Pumpking"); break;
                    }
                }
                if (!Main.snowMoon)
                {
                    switch (npc.type)
                    {
                        case NPCID.Everscream: system.QueueLocation("Everscream"); break;
                        case NPCID.SantaNK1: system.QueueLocation("Santa-NK1"); break;
                        case NPCID.IceQueen: system.QueueLocation("Ice Queen"); break;
                    }
                }
            }


            if (ModLoader.HasMod("CalamityMod")) CalamityOnKill(npc.type);
            if (ModLoader.HasMod("FargowiltasSouls")) FargoOnKill(npc.type);
        }

        [JITWhenModsEnabled("CalamityMod")]
        void CalamityOnKill(int npc)
        {
            var seldomArchipelago = ModContent.GetInstance<ArchipelagoSystem>();

            if (npc == NPCID.BloodNautilus) seldomArchipelago.QueueLocation("Dreadnautilus");
            else if (npc == ModContent.NPCType<CalamityMod.NPCs.PrimordialWyrm.PrimordialWyrmHead>()) seldomArchipelago.QueueLocation("Primordial Wyrm");
        }

        void FargoOnKill(int npc)
        {
            var seldomArchipelago = ModContent.GetInstance<ArchipelagoSystem>();

            if (npc == NPCID.DD2Betsy) seldomArchipelago.QueueLocation("Betsy");
        }
    }
}