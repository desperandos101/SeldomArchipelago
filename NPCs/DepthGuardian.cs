using System.Numerics;
using FargowiltasSouls.Content.Bosses.VanillaEternity;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SeldomDespArchipelago.NPCs
{
    // For a shop to open, there must be an NPC for the character to talk to, so for the collection
    // shop, we spawn a tiny, invisible NPC that follows the player.
    public class DepthGuardian : ModNPC
    {
        static Asset<Texture2D> texture;
        Player target;
        public override void SetStaticDefaults()
        {
            texture = ModContent.Request<Texture2D>($"Terraria/Images/NPC_{NPCID.DungeonGuardian}");
        }
        public override void SetDefaults()
        {
			NPC.width = 80;
			NPC.height = 102;
			NPC.aiStyle = -1;
			NPC.lifeMax = 2050;
            NPC.noTileCollide = true;
        }
        public override void OnSpawn(IEntitySource source)
        {
            if (source is not EntitySource_Parent pSource || pSource.Entity is not Player p) return;
            target = p;
        }
        public override void AI()
        {
            NPC.position.X = target.Center.X - NPC.width/2;
            NPC.position.Y = (float)Main.worldSurface * 16;
        }
        public override bool CanBeHitByNPC(NPC attacker) => false;
        public override bool? CanBeHitByItem(Player player, Item item) => false;
        public override bool? CanBeHitByProjectile(Projectile projectile) => false;
        public override bool PreDraw(SpriteBatch spriteBatch, Microsoft.Xna.Framework.Vector2 screenPos, Color drawColor)
        {
            spriteBatch.Draw(texture.Value, NPC.position - screenPos, drawColor);
            return false;
        }
    }
}