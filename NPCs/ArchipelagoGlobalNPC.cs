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
            if (ModLoader.HasMod("CalamityMod")) CalamityOnKill(npc.type);
            if (ModLoader.HasMod("FargowiltasSouls")) FargoOnKill(npc.type);
        }

        public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
        {
            var seldomArchipelago = ModContent.GetInstance<ArchipelagoSystem>();
            if (seldomArchipelago.ActiveSlot() is null) return;
            var player = spawnInfo.Player;
            bool spawnDepthGuardian = seldomArchipelago.AllowedDepth() switch {
                0 => player.ZoneUnderworldHeight || player.ZoneRockLayerHeight || player.ZoneDirtLayerHeight,
                1 => player.ZoneUnderworldHeight || player.ZoneRockLayerHeight,
                2 => player.ZoneUnderworldHeight,
                3 => false,
                _ => throw new System.Exception("Too many Progressive Depths!")
            };
            if (spawnDepthGuardian)
            {
                pool[NPCID.DungeonGuardian] = 9999f;
            }
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