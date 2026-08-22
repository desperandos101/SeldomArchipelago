using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Achievements;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.GameContent.Personalities;
using Terraria.GameContent.UI;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Utilities;
using ReLogic.Content;
using System.Collections.Immutable;
using Newtonsoft.Json.Linq;
using System.IO;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SeldomDespArchipelago.Systems;

namespace SeldomDespArchipelago.NPCs
{
    [AutoloadHead]
    public class GhostNPC : ModNPC
    {
        int ghostType;
        public int GhostType { get => ghostType; }
        int transformType;
        public void SetGhostType(int type)
        {
            if (allGhostTypes.Contains(type))
            {
                ghostType = type;
            }
            else
            {
                throw new Exception($"Attempted to set ghostType to value {type}.");
            }
            var archipelagoSystem = ModContent.GetInstance<ArchipelagoSystem>();
            if (archipelagoSystem.world.npcLocTypeToNpcItemType is not null && archipelagoSystem.world.npcLocTypeToNpcItemType.TryGetValue(ghostType, out int newNpcType))
            {
                transformType = newNpcType;
            }
            else
            {
                transformType = 0;
            }
        }
        static readonly ImmutableHashSet<int> allGhostTypes =
        [
            NPCID.Guide,
            NPCID.Merchant,
            NPCID.Nurse,
            NPCID.Demolitionist,
            NPCID.DyeTrader,
            NPCID.BestiaryGirl,
            NPCID.Dryad,
            NPCID.Painter,
            NPCID.ArmsDealer,
            NPCID.WitchDoctor,
            NPCID.Clothier,
            NPCID.PartyGirl,
            NPCID.Truffle,
            NPCID.Pirate,
            NPCID.Steampunker,
            NPCID.Cyborg,
            NPCID.SantaClaus,
            NPCID.Princess
        ];
        public static bool GhostableType(int type) => allGhostTypes.Contains(type);
        static Dictionary<int, Asset<Texture2D>> typeToTexture = new();
        Asset<Texture2D> GetTexture() => typeToTexture[ghostType];
        public override void SetStaticDefaults()
        {
            typeToTexture[0] = ModContent.Request<Texture2D>($"Terraria/Images/NPC_{NPCID.GolferRescue}");
            foreach (var npcType in allGhostTypes)
            {
                typeToTexture[npcType] = ModContent.Request<Texture2D>($"Terraria/Images/NPC_{npcType}");
            }
            typeToTexture[NPCID.Princess] = ModContent.Request<Texture2D>($"Terraria/Images/TownNPCs/Princess_Default");
            NPC.townNPC = true;
        }
        public override void SetDefaults()
        {
            NPC.CloneDefaults(NPCID.Guide);
            NPC.aiStyle = 0;
        }
        public override void ModifyTypeName(ref string typeName) => typeName = $"{Lang.GetNPCName(ghostType)} Check";
        public override Color? GetAlpha(Color drawColor)
        {
            return drawColor;
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            var texture = GetTexture();
            drawColor = NPC.GetNPCColorTintedByBuffs(drawColor);
            drawColor.A = 50;
            float num35 = 0f;
            float num36 = Main.NPCAddHeight(NPC);
            Vector2 halfSize = new Vector2(texture.Width() / 2, texture.Height() / Main.npcFrameCount[ghostType] / 2);
            SpriteEffects spriteEffects = SpriteEffects.None;
            Rectangle frame6 = texture.Frame(1, 25, 0, 0);
            float x = NPC.position.X - screenPos.X + (float)(NPC.width / 2) - (float)texture.Width() * NPC.scale / 2f + halfSize.X * NPC.scale;
            float y = NPC.position.Y - screenPos.Y + (float)NPC.height - (float)texture.Height() * NPC.scale / (float)Main.npcFrameCount[ghostType] + 4f + halfSize.Y * NPC.scale + num36 + num35 + NPC.gfxOffY;
            spriteBatch.Draw(texture.Value, new Vector2(x, y), frame6, NPC.GetAlpha(drawColor), NPC.rotation, halfSize, NPC.scale, spriteEffects, 0f);

            return false;
        }
        public override void FindFrame(int frameHeight)
        {
            NPC.frame = GetTexture().Frame(1, 25, 0, 0);
        }
        public override bool CanChat() => true;
        public override string GetChat() => $"{ArchipelagoSystem.npcIDtoName[ghostType]} location redeemed!";
        public override bool NeedSaving() => true;
        public override bool CanBeHitByNPC(NPC attacker) => false;
        public override bool? CanBeHitByItem(Player player, Item item) => false;
        public static bool AnyGhosts(int type) => Main.npc.Any(npc => npc.active && npc.ModNPC is GhostNPC checkNPC && checkNPC.ghostType == type);
        public static void RedeemGhost(int index)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                var packet = ModContent.GetInstance<SeldomArchipelago>().GetPacket();
                packet.Write("RedeemGhost");
                packet.Write(index);
                packet.Send();
                return;
            }
            GhostNPC ghost = Main.npc[index].ModNPC as GhostNPC;
            if (ghost.transformType > 0 && !NPC.AnyNPCs(ghost.transformType))
            {
                Main.npc[index].Transform(ghost.transformType);
                if (ghost.transformType == NPCID.Truffle) AchievementsHelper.NotifyProgressionEvent(18);
            }
            else
            {
                ghost.NPC.StrikeInstantKill();
                NPC.FairyEffects(ghost.NPC.Center, Main.rand.Next(3));
            }
            ModContent.GetInstance<ArchipelagoSystem>().QueueLocation(ArchipelagoSystem.npcIDtoName[ghost.ghostType]);
        }
        public override bool ModifyDeathMessage(ref NetworkText customText, ref Color color)
        {
            customText = NetworkText.FromLiteral(GetChat());
            return false;
        }
        public override void SaveData(TagCompound tag)
        {
            tag[nameof(ghostType)] = ghostType;
            tag[nameof(transformType)] = transformType;
        }
        public override void LoadData(TagCompound tag)
        {
            ghostType = tag.GetAsInt(nameof(ghostType));
            transformType = tag.GetAsInt(nameof(transformType));
        }
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(ghostType);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            ghostType = reader.ReadInt32();
        }
    }
}
