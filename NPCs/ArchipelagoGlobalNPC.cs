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