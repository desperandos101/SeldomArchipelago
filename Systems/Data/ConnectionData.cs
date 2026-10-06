using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace SeldomDespArchipelago.Systems.Data
{
    public enum ConnectStatus
        {
        Unset,
        Connecting,
        Disconnecting,
        Valid,
        BadURL,
        Failed,
        WrongSlot,
        WrongPass,
        WrongGame,
        ClientOlder,
        ClientNewer,
        CalamityNeeded,
        NoCalamityNeeded,
        FargoNeeded,
        NoFargoNeeded,
        MultiplayerClient,
    }
    public static class ConnectionData
    {
        static ConnectStatus status = ConnectStatus.Unset;
        static (string, Color) statusText = ("Disconnected. Click the icon to connect!", Color.White);
        public static ConnectStatus Status {
            get => status;
            set
            {
                SlotData? Slot() => ModContent.GetInstance<ArchipelagoSystem>().ActiveSlot();
                status = value;
                statusText = value switch
                {
                    ConnectStatus.Unset => ("Disconnected. Click the icon to connect!", Color.AntiqueWhite),
                    ConnectStatus.Connecting => ("Connecting...", Color.Yellow),
                    ConnectStatus.Disconnecting => ("Disconnecting...", Color.Yellow),
                    ConnectStatus.Valid => ($"Connected to slot {Slot()?.Name ?? "NULL"}. Press the button again to disconnect.", Color.GreenYellow),
                    ConnectStatus.BadURL => ($"The provided address + port is invalid.", Color.Orange),
                    ConnectStatus.Failed => ($"Failed to connect to multiworld at {ModContent.GetInstance<Config.Config>().address}:{ModContent.GetInstance<Config.Config>().port}.", Color.Orange),
                    ConnectStatus.WrongSlot => ($"Could not find slot {ModContent.GetInstance<Config.Config>().name} in room", Color.OrangeRed),
                    ConnectStatus.WrongPass => ($"The room password is incorrect.", Color.OrangeRed),
                    ConnectStatus.WrongGame => ($"The slot {Slot()?.Name} does not have \"{ArchipelagoSystem.APWorldName}\" registered to it.", Color.OrangeRed),
                    ConnectStatus.ClientOlder => ($"The connected slot requires a newer version of the client mod.\nPlease update your client.", Color.SkyBlue),
                    ConnectStatus.ClientNewer => ($"The connected slot requires an older version of the client.\nLook on the releases page for the latest client compatible with {(ArchipelagoSystem.desiredAPversion is null ? "0.6.61 or 0.6.62." : $"{ArchipelagoSystem.desiredAPversion[0]}.{ArchipelagoSystem.desiredAPversion[1]}.{ArchipelagoSystem.desiredAPversion[2]}")} and downpatch.", Color.SkyBlue),
                    ConnectStatus.CalamityNeeded => ("The connected slot requires Calamity. Please reload with it enabled.", Color.SkyBlue),
                    ConnectStatus.NoCalamityNeeded => ("The connected slot does not have Calamity enabled. Please reload with it disabled.", Color.SkyBlue),
                    ConnectStatus.FargoNeeded => ("The connected slot requires Fargo's Souls. Please reload with it enabled.", Color.SkyBlue),
                    ConnectStatus.NoFargoNeeded => ("The connected slot does not have Fargo's Souls enabled. Please reload with it disabled.", Color.SkyBlue),
                    ConnectStatus.MultiplayerClient => ("You're in Multiplayer client mode! You should not be seeing this.\nContact the dev if you do.", Color.HotPink),
                    _ => throw new System.Exception("Invalid ConnectionStatus " + value)
                };
            }
        }
        public static bool SafeStatus => status != ConnectStatus.Connecting && status != ConnectStatus.Disconnecting;
        public static (string, Color) StatusText => statusText;
    }
}