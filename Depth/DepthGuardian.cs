using System;
using FargowiltasSouls.Content.Bosses.VanillaEternity;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SeldomDespArchipelago.Systems;
using static SeldomDespArchipelago.Depth.DepthSystem;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SeldomDespArchipelago.Depth
{
    public class DepthGuardian : ModNPC
    {
        static Asset<Texture2D> texture;
        readonly int frames = 360;
        int spawnAnim = 0;
        int deathTime = 0;
        readonly Color[] colors = [
            new Color(232, 88, 88),
            new Color(88, 232, 117),
            new Color(242, 126, 219),
            new Color(255, 216, 138),
            new Color(138, 146, 255),
            new Color(252, 255, 138),
        ];
        Player Target => Main.player[(int)NPC.ai[0]];
        double? GuardedDepth => ForbiddenDepth((int)NPC.ai[1]) * 16;
        public bool Released { get => NPC.ai[2] == 1; set => NPC.ai[2] = value ? 1 : 0; }
        public float Threshold = 200;
        public float ApproachPercent => GuardedDepth is not null ? Math.Clamp(Target.position.Y-(float)GuardedDepth+thresholdBeforeAppearance, 0, Threshold)/Threshold : 0f;
        float Opacity => ApproachPercent * Math.Max(0, 1f - deathTime/100f);
        public override void SetStaticDefaults()
        {
            texture = ModContent.Request<Texture2D>($"Terraria/Images/Projectile_{ProjectileID.BabySkeletronHead}");
        }
        public override void SetDefaults()
        {
			NPC.width = 80;
			NPC.height = 102;
			NPC.aiStyle = -1;
			NPC.lifeMax = 9999;
            NPC.damage = 99999;
            NPC.noTileCollide = true;
            NPC.chaseable = false;
        }
        public override void OnSpawn(IEntitySource source)
        {
            if (source is not EntitySource_Parent pSource || pSource.Entity is not Player p) return;
            NPC.ai[0] = p.whoAmI;
            NPC.ai[1] = ModContent.GetInstance<ArchipelagoSystem>().AllowedDepth();
            NPC.ai[2] = 0f;
        }
        public override void AI()
        {
            float guardedDepth = (float)GuardedDepth;
            NPC.position.X = Target.Center.X - NPC.width/2;
            NPC.position.Y = Math.Max(guardedDepth, Target.Center.Y - NPC.height/2);
            Lighting.AddLight(NPC.Center, Opacity * 0.5f, Opacity * 0.3f, Opacity * 0.1f);
            if (spawnAnim < 60) spawnAnim+=2;
            if (Main.netMode != NetmodeID.MultiplayerClient && deathTime > 99) NPC.StrikeInstantKill();
            if (Target.dead || Released || Target.Center.Y < guardedDepth - thresholdBeforeAppearance) deathTime++;
            else if (deathTime > 0) deathTime-=2;
        }
        public override bool CanBeHitByNPC(NPC attacker) => false;
        public override bool? CanBeHitByItem(Player player, Item item) => false;
        public override bool? CanBeHitByProjectile(Projectile projectile) => false;
        public override bool CanHitPlayer(Player target, ref int cooldownSlot) => !Released;
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            double PiTime(int phasePerSecond, float advance = 0) => (Main.timeForVisualEffects * phasePerSecond + frames*advance) % frames / frames * MathHelper.TwoPi;

            for (int i = 0; i < 6; i++)
            {
                Vector2 circlePos = NPC.Center - screenPos;
                circlePos.X += (float)(Math.Cos(PiTime(3,i/6f)) * 35 * (Math.Pow(1.5-spawnAnim/120f, 3) * Math.Pow(1+deathTime/200f, 3)));
                circlePos.Y += (float)(Math.Sin(PiTime(3,i/6f)) * 35 * (Math.Pow(1.5-spawnAnim/120f, 3) * Math.Pow(1+deathTime/200f, 3)));
                Color color = colors[i];
                byte Mult(byte b) => (byte)(b*Opacity);
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