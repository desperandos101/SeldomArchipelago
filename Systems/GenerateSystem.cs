using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace SeldomArchipelago.Systems
{
    class GenerateSystem : GenPass
    {
        public GenerateSystem(string name, float loadWeight) : base(name, loadWeight) { }
        protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
        {
            progress.Message = "Connecting to Archipelago";
            ArchipelagoSystem.ConnectToArchipelago(out var result, out var newSession);
            progress.Message = $"Hello, {newSession.Players.GetPlayerName(1)}!";
            Thread.Sleep(100);
        }
    }
}
