using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.OS;
using SeldomDespArchipelago.Systems;
using SeldomDespArchipelago.Systems.Data;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using static SeldomDespArchipelago.Systems.Data.ConnectStatus;
namespace SeldomDespArchipelago.UI;
public class ConnectButton
{
	private ArchipelagoSystem ArchSystem => ModContent.GetInstance<ArchipelagoSystem>();  // shhhhhhh
	private static Item _fakeItem = new Item();
	public Asset<Texture2D> Image = ModContent.GetInstance<SeldomArchipelago>().Assets.Request<Texture2D>("UI/CollectionButton");

	// Returns true if the cursor is hovering over the drawn sprite
	public bool Draw(SpriteBatch spriteBatch, Vector2 anchorPosition, ConnectStatus status)
	{
		(string msg, Color color) t = status switch
		{
			Unset => ("Disconnected. Click the icon to connect!", Color.AntiqueWhite),
			Connecting => ("Connecting...", Color.Yellow),
			Disconnecting => ("Disconnecting...", Color.Yellow),
			Valid => ($"Connected to slot {ArchSystem.ActiveSlot()?.Name ?? "NULL"}. Press the button again to disconnect.", Color.GreenYellow),
			WrongSlot => ($"Could not find slot {ModContent.GetInstance<Config.Config>().name} in room", Color.OrangeRed),
			WrongPass => ($"The room password is incorrect.", Color.OrangeRed),
			WrongGame => ($"The slot {ArchSystem.ActiveSlot()?.Name} does not have \"{ArchipelagoSystem.APWorldName}\" registered to it.", Color.OrangeRed),
			ClientOlder => ($"The connected slot requires a newer version of the client mod.\nPlease update your client.", Color.SkyBlue),
			ClientNewer => ($"The connected slot requires an older version of the client.\nLook on the releases page for the latest client compatible with {(ArchipelagoSystem.desiredAPversion is null ? "0.6.61 or 0.6.62." : $"{ArchipelagoSystem.desiredAPversion[0]}.{ArchipelagoSystem.desiredAPversion[1]}.{ArchipelagoSystem.desiredAPversion[2]}")} and downpatch.", Color.SkyBlue),
			CalamityNeeded => ("The connected slot requires Calamity. Please reload with it enabled.", Color.SkyBlue),
			NoCalamityNeeded => ("The connected slot does not have Calamity enabled. Please reload with it disabled.", Color.SkyBlue),
			FargoNeeded => ("The connected slot requires Fargo's Souls. Please reload with it enabled.", Color.SkyBlue),
			NoFargoNeeded => ("The connected slot does not have Fargo's Souls enabled. Please reload with it disabled.", Color.SkyBlue),
			MultiplayerClient => ("You're in Multiplayer client mode! You should not be seeing this.\nContact the dev if you do.", Color.HotPink),
			_ => ("Unknown connection status! You should not be seeing this.\nContact the dev if you do.", Color.HotPink),
		};
		
		var textSize = ChatManager.GetStringSize(Terraria.GameContent.FontAssets.MouseText.Value, t.msg, new Vector2(1, 1));
		var textPos = new Vector2(anchorPosition.X - textSize.X / 2,  anchorPosition.Y - textSize.Y);
		ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, Terraria.GameContent.FontAssets.MouseText.Value, t.msg, textPos, t.color, 0, Vector2.Zero, new Vector2(1, 1));

		bool hover = false;
		Rectangle r = Image.Frame();

		var iconPos = new Vector2(anchorPosition.X, anchorPosition.Y + 30f);
		Vector2 vector = r.Size();
		Vector2 vector2 = iconPos - vector / 2f;
		if (Main.MouseScreen.Between(vector2, vector2 + vector)) {
			Main.LocalPlayer.mouseInterface = true;
			DrawTooltip(status == Valid);
			hover = true;
		}

		Rectangle rectangle2 = Image.Frame();

		Texture2D value = Image.Value;
		spriteBatch.Draw(value, iconPos, rectangle2, Color.White, 0f, rectangle2.Size() / 2f, 1f, SpriteEffects.None, 0f);
		return hover;
	}

	private void DrawTooltip(bool disconnect)
	{
		Item fakeItem = _fakeItem;
		fakeItem.SetDefaults(0, noMatCheck: true);
		string textValue = Language.GetTextValue(disconnect ? "Disconnect" : "Connect");
		fakeItem.SetNameOverride(textValue);
		fakeItem.type = 1;
		fakeItem.scale = 0f;
		fakeItem.rare = 8;
		fakeItem.value = -1;
		Main.HoverItem = _fakeItem;
		Main.instance.MouseText("", 0, 0);
		Main.mouseText = true;
	}

	public bool TryClicking()
	{
		if (!PlayerInput.IgnoreMouseInterface && Main.mouseLeft && Main.mouseLeftRelease) {
			SoundEngine.PlaySound(SoundID.AbigailCry);
			Main.mouseLeftRelease = false;
			return true;
		}
		return false;
	}
}
