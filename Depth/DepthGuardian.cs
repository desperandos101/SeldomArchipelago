using System;
using FargowiltasSouls.Content.Bosses.VanillaEternity;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SeldomDespArchipelago.Depth
{
    public class DepthGuardian : ModNPC
    {
        static Asset<Texture2D> texture;
        readonly int frames = 360;
        int spawnAnim = 0;
        readonly Color[] colors = [
            new Color(232, 88, 88),
            new Color(88, 232, 117),
            new Color(242, 126, 219),
            new Color(255, 216, 138),
            new Color(138, 146, 255),
            new Color(252, 255, 138),
        ];
        Player target;
        public override void SetStaticDefaults()
        {
            texture = ModContent.Request<Texture2D>($"Terraria/Images/Projectile_{ProjectileID.BabySkeletronHead}");
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
            Lighting.AddLight(NPC.Center, target.GetModPlayer<DepthPlayer>().DepthToScary * 0.5f, target.GetModPlayer<DepthPlayer>().DepthToScary * 0.3f, target.GetModPlayer<DepthPlayer>().DepthToScary * 0.1f);
            if (spawnAnim < 60) spawnAnim++;
        }
        public override bool CanBeHitByNPC(NPC attacker) => false;
        public override bool? CanBeHitByItem(Player player, Item item) => false;
        public override bool? CanBeHitByProjectile(Projectile projectile) => false;
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            double PiTime(int phasePerSecond, float advance = 0) => (Main.timeForVisualEffects * phasePerSecond + frames*advance) % frames / frames * MathHelper.TwoPi;

            for (int i = 0; i < 6; i++)
            {
                Vector2 circlePos = NPC.Center - screenPos;
                circlePos.X += (float)(Math.Cos(PiTime(3,i/6f)) * 35 * (1-Math.Pow(1-(float)spawnAnim/60, 3)));
                circlePos.Y += (float)(Math.Sin(PiTime(3,i/6f)) * 35 * (1-Math.Pow(1-(float)spawnAnim/60, 3)));
                Color color = colors[i];
                float opacity = target.GetModPlayer<DepthPlayer>().DepthToScary;
                byte Mult(byte b) => (byte)(b*opacity);
                color.R = Mult(color.R);
                color.G = Mult(color.G);
                color.B = Mult(color.B);
                color.A = Mult(color.A);
                spriteBatch.Draw(texture.Value, circlePos, null, color, 0, new Vector2(42, 48)/2, Vector2.One, SpriteEffects.None, 1);
            }
            return false;
        }
    }
}