using ReLogic.Utilities;
using SeldomDespArchipelago.NPCs;
using SeldomDespArchipelago.Systems;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.Achievements;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Achievements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SeldomDespArchipelago.Depth
{
    public class DepthPlayer : ModPlayer
    {
        public static float DangerousDepth = (float)Main.worldSurface * 16 - 300;
        public float Threshold = 200;
        public float DepthToScary => Math.Clamp(Player.position.Y-DangerousDepth, 0, Threshold)/Threshold;
        public int? activeGuard = null;
        public override void PostUpdate()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            if (Player.position.Y > DangerousDepth && activeGuard is null)
            {
                activeGuard = NPC.NewNPC(new EntitySource_Parent(Player), 1, 2, ModContent.NPCType<DepthGuardian>(), 0, 0);
            }
            else if (Player.position.Y < DangerousDepth && activeGuard is int activeGuardID)
            {
                Main.npc[activeGuardID].StrikeInstantKill();
                activeGuard = null;
            }
        }
    }
}