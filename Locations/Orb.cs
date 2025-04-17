using System.Configuration;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.DataStructures;
using System;
using SeldomArchipelago;
using Terraria.GameContent.ItemDropRules;
using System.Linq;
using SeldomArchipelago.ArchipelagoItem;
using System.Collections.Generic;
using System.Numerics;
using System.Media;
using SeldomArchipelago.Systems;

namespace SeldomArchipelago.Locations
{
    public class EntitySource_TileBreak_Rando : IEntitySource
    {
        public string Context { get; }
    }
    public class CheckTileDrop : GlobalItem
    {
        public override void OnSpawn(Item item, IEntitySource source)
        {
            if (!ArchipelagoSystem.GetSession().randomizeChests) return;
            if (source is EntitySource_TileBreak && item.type == ItemID.MusketBall && item.stack == 100)
            { //specifically to get rid of musket balls that always drop from shadow orbs
                item.TurnToAir();
            }
            else if (source is EntitySource_TileBreak tileSource)
            {
                int[] orbItems =
                {
                    ItemID.Musket,
                    ItemID.ShadowOrb,
                    ItemID.Vilethorn,
                    ItemID.BallOHurt,
                    ItemID.BandofStarpower,
                    ItemID.TheUndertaker,
                    ItemID.CrimsonHeart,
                    ItemID.PanicNecklace,
                    ItemID.CrimsonRod,
                    ItemID.TheRottedFork
                };
                if (orbItems.Contains(item.type))
                {
                    item.SetDefaults(ModContent.ItemType<ArchipelagoItem.ArchipelagoItem>());
                    var archItem = item.ModItem as ArchipelagoItem.ArchipelagoItem;
                    archItem.SetCheck(LocationSystem.EvilOrb);
                }
            }
        }
    }
}