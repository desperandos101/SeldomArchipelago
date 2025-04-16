using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace SeldomArchipelago.Locking
{
    internal class InvasionLock : ModSystem
    {
        public static List<int> invasionList = [];
        public override void PostUpdateInvasions()
        {
            if (invasionList.Count == 0)
            {
                return;
            }
            if (Main.invasionType == 0)
            {
                int invType = invasionList[0];
                Main.StartInvasion(invType);
                invasionList.RemoveAt(0);
            }
        }
    }
}
