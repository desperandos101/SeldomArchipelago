using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.OS;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
namespace SeldomDespArchipelago.UI;
public class ConnectButton
{
	private static Item _fakeItem = new Item();
	public string TooltipTextKey => "Connected";
	public Asset<Texture2D> Image = ModContent.GetInstance<SeldomArchipelago>().Assets.Request<Texture2D>("UI/CollectionButton");

	public void Draw(SpriteBatch spriteBatch, Vector2 anchorPosition)
	{
		Rectangle r = Image.Frame();

		Vector2 vector = r.Size();
		Vector2 vector2 = anchorPosition - vector / 2f;
		bool flag = false;
		if (Main.MouseScreen.Between(vector2, vector2 + vector)) {
			Main.LocalPlayer.mouseInterface = true;
			flag = true;
			DrawTooltip();
			TryClicking();
		}

		Rectangle rectangle2 = Image.Frame();

		Texture2D value = Image.Value;
		spriteBatch.Draw(value, anchorPosition, rectangle2, Color.White, 0f, rectangle2.Size() / 2f, 1f, SpriteEffects.None, 0f);
	}

	private void DrawTooltip()
	{
		Item fakeItem = _fakeItem;
		fakeItem.SetDefaults(0, noMatCheck: true);
		string textValue = Language.GetTextValue(TooltipTextKey);
		fakeItem.SetNameOverride(textValue);
		fakeItem.type = 1;
		fakeItem.scale = 0f;
		fakeItem.rare = 8;
		fakeItem.value = -1;
		Main.HoverItem = _fakeItem;
		Main.instance.MouseText("", 0, 0);
		Main.mouseText = true;
	}

	private void TryClicking()
	{
		if (!PlayerInput.IgnoreMouseInterface && Main.mouseLeft && Main.mouseLeftRelease) {
			SoundEngine.PlaySound(SoundID.AbigailCry);
			Main.mouseLeftRelease = false;
			OpenLink();
		}
	}

	private void OpenLink()
	{
		Console.WriteLine("BGAWK");
	}
}
