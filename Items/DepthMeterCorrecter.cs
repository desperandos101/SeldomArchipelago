using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SeldomDespArchipelago.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace SeldomDespArchipelago.Items
{
    public class DepthUpdate : GlobalItem
    {
        public override void UpdateInfoAccessory(Item item, Player player)
        {
            var carry = player.GetModPlayer<DepthCarry>();
            if (player.ZoneDirtLayerHeight)
            {
                carry.depth = 1;
            }
            else if (player.ZoneRockLayerHeight)
            {
                carry.depth = 2;
            }
            else if (player.ZoneUnderworldHeight)
            {
                carry.depth = 3;
            }
            else
            {
                carry.depth = 0;
            }
        }
    }
    public class DepthCarry : ModPlayer
    {
        public int depth = 0;
    }
    public class DepthInfo : GlobalInfoDisplay
    {
        public override void ModifyDisplayParameters(InfoDisplay currentDisplay, ref string displayValue, ref string displayName, ref Color displayColor, ref Color displayShadowColor)
        {
            if (currentDisplay is not DepthMeterInfoDisplay) return;
            if (!ModContent.GetInstance<Config.Config>().fixDepthMeter) return;
            var carry = Main.LocalPlayer.GetModPlayer<DepthCarry>();
            displayValue += carry.depth switch
            {
                0 => "(SURFACE)",
                1 => "(UNDER)",
                2 => "(CAVERN)",
                3 => "(HELL)",
                _ => "(UNRECOGNIZED)",
            };
        }
    }
}
