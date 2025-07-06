using Microsoft.Xna.Framework;
using SeldomArchipelago.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace SeldomArchipelago.Locking
{
    internal class FishLock : ModPlayer
    {
        public override void CatchFish(FishingAttempt attempt, ref int itemDrop, ref int npcSpawn, ref AdvancedPopupRequest sonar, ref Vector2 sonarPosition)
        {
            var flags = ArchipelagoSystem.GetFlags();
            if (flags is null) return;
            if (!flags.PlayerBiomeUnlocked(Main.LocalPlayer))
            {
                itemDrop = 0;
            }
        }
    }
}
