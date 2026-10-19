using Microsoft.Xna.Framework;
using ReLogic.Utilities;
using SeldomDespArchipelago.NPCs;
using SeldomDespArchipelago.Systems;
using SeldomDespArchipelago.Systems.Data;
using static SeldomDespArchipelago.Depth.DepthSystem;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.Achievements;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Achievements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SeldomDespArchipelago.Depth
{
    public class DepthPlayer : ModPlayer
    {
        public int? activeGuard = null;
        public override void PostUpdate()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            if (ForbiddenDepth() is not double forbiddenDepth) return;
            float dangerousDepth = (float)forbiddenDepth * 16 - thresholdBeforeAppearance;

            if (Player.position.Y > dangerousDepth && activeGuard is null)
            {
                activeGuard = NPC.NewNPC(new EntitySource_Parent(Player), 1, 2, ModContent.NPCType<DepthGuardian>(), 0, 0);
            }
            else if (activeGuard is int activeGuardID && !Main.npc[activeGuardID].active)
            {
                activeGuard = null;
            }
        }
        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genDust, ref PlayerDeathReason damageSource)
        {
            if (Main.npc[damageSource.SourceNPCIndex].ModNPC is DepthGuardian)
            {
                damageSource.CustomReason = NetworkText.FromLiteral($"{Player.name} went in too deep.");
            }
            return true;
        }
        public void ReleaseGuard()
        {
            if (activeGuard is not int activeGuardID) return;
            ((DepthGuardian)Main.npc[activeGuardID].ModNPC).Released = true;
        }
        public override bool CanBeTeleportedTo(Vector2 teleportPosition, string context) => ForbiddenDepth() is double depth ? teleportPosition.Y < depth * 16 : true;
        public override IEnumerable<Item> AddStartingItems(bool mediumCoreDeath) {
            if (mediumCoreDeath || Player.difficulty != PlayerDifficultyID.Creative) return [];
            return [new Item(ItemID.CopperShortsword)];
        }
    }
}