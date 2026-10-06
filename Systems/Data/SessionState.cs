using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using System.Collections.Generic;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static SeldomDespArchipelago.Systems.ArchipelagoSystem;
using SeldomDespArchipelago.Systems.Data;
using System.Linq;
using Newtonsoft.Json.Linq;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using System.Text;
using Archipelago.MultiClient.Net.MessageLog.Parts;
using System;
using Terraria.Social;
using System.Reflection;
using System.Net.WebSockets;
using System.Net.Sockets;
using FargowiltasSouls.Content.Projectiles.Souls;
using System.Threading;

namespace SeldomDespArchipelago.Systems.Data
{
    /// <summary>
    /// Helper class for interfacing with the actively connected room.
    /// </summary>
    public class SessionState
    {
        public SlotData slotData;
        // List of locations that are currently being sent
        public List<Task<Dictionary<long, ScoutedItemInfo>>> locationQueue = new List<Task<Dictionary<long, ScoutedItemInfo>>>();
        public ArchipelagoSession session;
        public DeathLinkService deathlink;
        // Like `collectedItems`, but unique to this Archipelago session, and doesn't save, so
        // it starts at 0 each session. While less than `collectedItems`, it discards items
        // instead of collecting them. This is needed bc AP just gives us a list of items that
        // we have, and it's up to us to keep track of which ones we've already applied.
        public int currentItem;
        public bool victory;
        public static SessionState InitializeSession(out ConnectStatus status)
        {
            status = ConnectStatus.Unset;
            if (Main.netMode == NetmodeID.MultiplayerClient) return null;

            var config = ModContent.GetInstance<Config.Config>();

            LoginResult result;
            ArchipelagoSession newSession;
            try
            {
                newSession = ArchipelagoSessionFactory.CreateSession(config.address, config.port);

                result = newSession.TryConnectAndLogin(APWorldName, config.name, ItemsHandlingFlags.AllItems, APversion, null, null, config.password == "" ? null : config.password);
                if (result is LoginFailure failure)
                {
                    var error = failure.ErrorCodes.FirstOrDefault();  // don't think it's important to get multiple
                    status = error switch
                    {
                        ConnectionRefusedError.InvalidSlot => ConnectStatus.WrongSlot,
                        ConnectionRefusedError.InvalidGame => ConnectStatus.WrongGame,
                        ConnectionRefusedError.IncompatibleVersion => ConnectStatus.ClientOlder,
                        ConnectionRefusedError.InvalidPassword => ConnectStatus.WrongPass,
                        _ => ConnectStatus.Failed,
                    };
                    return null;
                }
            }
            catch (Exception e)
            {
                status = e is System.UriFormatException ? ConnectStatus.BadURL : ConnectStatus.Failed;
                return null;
            }

            SessionState sess = new()
            {
                session = newSession
            };

            var success = (LoginSuccessful)result;
            sess.slotData = new SlotData(sess.session, success);

            #region Validate
            bool versionSlotData = success.SlotData.TryGetValue("version", out var versionObj);
            bool newerVersion = false;
            if (versionSlotData)
            {
                desiredAPversion = ((JArray)versionObj).ToObject<int[]>();
                newerVersion = desiredAPversion[0] != APversion.Major || desiredAPversion[1] != APversion.Minor || desiredAPversion[2] != APversion.Build;
            }

            if (!versionSlotData || newerVersion)
            {
                status = ConnectStatus.ClientNewer;
                sess.Reset();
                return null;
            }

            bool calamityActive = ModLoader.HasMod("CalamityMod");
            if (calamityActive != sess.slotData.Calamity)
            {
                if (calamityActive) status = ConnectStatus.NoCalamityNeeded;
                else status = ConnectStatus.CalamityNeeded;
                return null;
            }
            bool fargoActive = ModLoader.HasMod("FargowiltasSouls");
            if (fargoActive != sess.slotData.Fargo)
            {
                if (fargoActive) status = ConnectStatus.NoFargoNeeded;
                else status = ConnectStatus.FargoNeeded;
                return null;
            }

            #endregion

            status = ConnectStatus.Valid;

            sess.session.Socket.SocketClosed += sess.OnClose;
            sess.session.Socket.ErrorReceived += sess.HandleError;

            sess.session.MessageLog.OnMessageReceived += sess.ApMessageToChat;

            if ((bool)success.SlotData["deathlink"])
            {
                sess.deathlink = sess.session.CreateDeathLinkService();
                sess.deathlink.EnableDeathLink();

                sess.deathlink.OnDeathLinkReceived += ReceiveDeathlink;
            }

            return sess;
        }
        private void HandleError(Exception e, string msg)
        {
            Chat($"EXCEPTION {nameof(e.GetType)}: {msg}", Microsoft.Xna.Framework.Color.Red);
            OnClose(msg);
        }
        private void OnClose(string _)
        {
            Chat("The server connection has been closed.", Microsoft.Xna.Framework.Color.Orange);
            ConnectionClosed?.Invoke(this, EventArgs.Empty);
        }
        public event EventHandler ConnectionClosed;
        public void Reset()
        {
            typeof(SocialAPI).GetField("_mode", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, SocialMode.Steam);

            if (session != null)
            {
                session.Socket.DisconnectAsync();
            }
            session = null;
        }
        public void QueueLocation(string locationName)
        {
            var location = session.Locations.GetLocationIdFromName(APWorldName, locationName);
            if (location == -1) return;
            if (session.Locations.AllLocationsChecked.Contains(location))
            {
                ModLoader.GetMod(nameof(SeldomDespArchipelago)).Logger.Info($"[AP] Location {locationName} already collected.");
                return;
            }
            locationQueue.Add(session.Locations.ScoutLocationsAsync(new[] { location }));
            session.Locations.CompleteLocationChecks(new[] { location });
        }
        public void QueueLocationClient(string locationName)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                QueueLocation(locationName);
                return;
            }
            var packet = ModContent.GetInstance<SeldomArchipelago>().GetPacket();
            packet.Write(locationName);
            packet.Send();
        }
        public void ApMessageToChat(LogMessage message)
        {
            var config = ModContent.GetInstance<Config.Config>();

            string normalMsg() => string.Concat(from part in message.Parts select part.Text);
            string colorMsg()
            {
                if (!config.colorText) return normalMsg();
                StringBuilder builder = new StringBuilder();
                foreach (var part in message.Parts)
                {
                    string msg = part.Text;
                    var color = part.Color;
                    string colorHex = color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
                    builder.Append($"[c/{colorHex}:{msg}]");
                }
                return builder.ToString();
            }

            if (config.chatSettings == Config.ChatSetting.Disable) return;

            bool thisSlotMentioned = false;
            bool playerPart = false;
            foreach (var part in message.Parts)
            {
                if (part.Type == MessagePartType.Player)
                {
                    playerPart = true;
                    if (part.Text == slotData.Name) thisSlotMentioned = true;
                }
            }
            if (playerPart && !thisSlotMentioned)
            {
                switch (config.chatSettings)
                {
                    case Config.ChatSetting.All: Chat(colorMsg()); break;
                    case Config.ChatSetting.Grey: Chat(normalMsg(), Microsoft.Xna.Framework.Color.Gray); break;
                    case Config.ChatSetting.Filter: break;
                    default: throw new Exception("Unhandled chat configuration");
                }
            }
            else Chat(colorMsg());
        }
        public void RedeemCache(OfflineCache cache)
        {
            foreach (string loc in cache.locationBacklog)
            {
                QueueLocation(loc);
            }
        }
    }
}