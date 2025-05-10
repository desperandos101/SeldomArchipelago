using System;
using System.Linq;
using System.Numerics;
using Microsoft.Xna.Framework;
using static SeldomArchipelago.Systems.ArchipelagoSystem;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using SeldomArchipelago.Systems;

namespace SeldomArchipelago.Locking
{
    public class TileLock : GlobalTile
    {
        private FlagSystem Flags => GetSession().flagSystem;
        public override bool CanKillTile(int i, int j, int type, ref bool blockDamaged) => Flags.TileRegionUnlocked(i, j);
        public override bool CanDrop(int i, int j, int type) => Flags.TileRegionUnlocked(i, j);
    }
    public class ShakingTreeLock : GlobalItem
    {
        public override void OnSpawn(Item item, IEntitySource source)
        {
            if (source is EntitySource_ShakeTree treeSource)
            {
                Point point = treeSource.TileCoords;
                FlagSystem flags = GetSession().flagSystem;
                if (!flags.TileRegionUnlocked(point.X, point.Y)) item.TurnToAir();
            }
        }
    }
}
