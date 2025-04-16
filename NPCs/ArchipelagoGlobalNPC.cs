using SeldomArchipelago.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SeldomArchipelago.NPCs
{
    [ExtendsFromMod("CalamityMod")]
    public class ArchipelagoGlobalNPC : GlobalNPC
    {
        public override void OnKill(NPC npc)
        {
            if (ModLoader.HasMod("CalamityMod")) CalamityOnKill(npc.type);
        }

        void CalamityOnKill(int npc)
        {
            var SeldomArchipelago = ModContent.GetInstance<ArchipelagoSystem>();

            if (npc == NPCID.BloodNautilus) SeldomArchipelago.QueueLocation("Dreadnautilus");
            else if (npc == ModContent.NPCType<CalamityMod.NPCs.PrimordialWyrm.PrimordialWyrmHead>()) SeldomArchipelago.QueueLocation("Primordial Wyrm");
        }
    }
}