using System;
using SeldomDespArchipelago.Systems;
using Terraria;
using Terraria.ModLoader;

namespace SeldomDespArchipelago.Depth
{
    public static class DepthSystem
    {
        public static readonly int thresholdBeforeAppearance = 300;
        public static double? ForbiddenDepth()
        {
            var archipelagoSystem = ModContent.GetInstance<ArchipelagoSystem>();
            return ForbiddenDepth(archipelagoSystem.AllowedDepth());
        }
        public static double? ForbiddenDepth(int type) => type switch {
            0 => Main.worldSurface,
            1 => Main.rockLayer,
            2 => Main.UnderworldLayer,
            3 => null,
            _ => throw new Exception($"Too many Progressive Depths! {type} counted")
        };
        public static string DepthUnlockText(int newCount) {
            if (newCount < 1)
            {
                throw new Exception($"{newCount} passed into DepthUnlockText");
            }
            return newCount switch
            {
                1 => "The Depth Guardian retreats from the Underground, guarding only the Caverns and deeper...",
                2 => "The Depth Guardian abandons the Caverns, now only watching the gates to the Underworld...",
                3 => "The Depth Guardian is sealed away, freeing the Underworld.",
                _ => "The excess \"Progressive Depth\" item is wasted, for the Depth Guardian is already banished."
            };
        }
    }
}