using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using ReLogic.Content;
using ReLogic.Graphics;
using Steamworks;
using System;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Nod
{
    public class Nod : Mod
    {
        private static ILHook SpriteBatch_DrawStringILHook;
        private static ILHook DynamicSpriteFont_InternalDrawILHook;
        private static ILHook MenuLoader_UpdateAndDrawModMenuInnerILHook;

        public override void Load()
        {
            SpriteBatch_DrawStringILHook ??= new ILHook(typeof(SpriteBatch).GetMethod("DrawString", BindingFlags.Instance | BindingFlags.Public, new Type[9] { typeof(SpriteFont), typeof(string), typeof(Vector2), typeof(Color), typeof(float), typeof(Vector2), typeof(Vector2), typeof(SpriteEffects), typeof(float) }), DontDrawD_SpriteBatch);
            DynamicSpriteFont_InternalDrawILHook ??= new ILHook(typeof(DynamicSpriteFont).GetMethod("InternalDraw", BindingFlags.Instance | BindingFlags.NonPublic), DontDrawD_DynamicSpriteFont);
            MenuLoader_UpdateAndDrawModMenuInnerILHook ??= new ILHook(typeof(MenuLoader).GetMethod("UpdateAndDrawModMenuInner", BindingFlags.Static | BindingFlags.NonPublic), ChangeMenuLogo);
        }

        public override void Unload()
        {
            SpriteBatch_DrawStringILHook.Undo();
            DynamicSpriteFont_InternalDrawILHook.Undo();
            MenuLoader_UpdateAndDrawModMenuInnerILHook.Undo();
        }

        public void DontDrawD_DynamicSpriteFont(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            cursor.Index = cursor.Instrs.Count;

            if (!cursor.TryGotoPrev(MoveType.Before,
                i => i.MatchLdloca(1),
                i => i.MatchLdflda(typeof(Vector2), nameof(Vector2.X)),
                i => i.MatchDup(),
                i => i.MatchLdindR4(),
                i => i.MatchLdloc(10),
                i => i.MatchLdfld(typeof(Vector3), nameof(Vector3.Y)),
                i => i.MatchLdloc(10),
                i => i.MatchLdfld(typeof(Vector3), nameof(Vector3.Z)),
                i => i.MatchAdd()))
            {
                LogIlError("DynamicSpriteFont", "Couldn't navigate to before kerning increase");
                return;
            }
            ILLabel label = cursor.MarkLabel();
            if (!cursor.TryGotoPrev(MoveType.Before,
               i => i.MatchLdarg2(),
               i => i.MatchLdloc(9),
               i => i.MatchLdfld(out _),
               i => i.MatchLdloc(12),
               i => i.MatchLdloc(9),
               i => i.MatchLdfld(out _)))
            {
                LogIlError("DynamicSpriteFont", "Couldn't navigate to before spriteBatch.Draw");
                return;
            }
            cursor.Emit(OpCodes.Ldloc, 8);
            cursor.EmitDelegate(CharIsD);
            cursor.Emit(OpCodes.Brtrue, label);
        }

        public void DontDrawD_SpriteBatch(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            cursor.Index = cursor.Instrs.Count;

            if (!cursor.TryGotoPrev(MoveType.Before,
                i => i.MatchLdloca(10),
                i => i.MatchLdflda(typeof(Vector2), nameof(Vector2.X)),
                i => i.MatchDup(),
                i => i.MatchLdindR4(),
                i => i.MatchLdloc(17),
                i => i.MatchLdfld(typeof(Vector3), nameof(Vector3.Y)),
                i => i.MatchLdloc(17),
                i => i.MatchLdfld(typeof(Vector3), nameof(Vector3.Z)),
                i => i.MatchAdd(),
                i => i.MatchAdd(),
                i => i.MatchStindR4()))
            {
                LogIlError("SpriteBatch", "Couldn't navigate to before kerning increase");
                return;
            }
            ILLabel label = cursor.MarkLabel();
            if (!cursor.TryGotoPrev(MoveType.Before,
               i => i.MatchLdarg0(),
               i => i.MatchLdloc0(),
               i => i.MatchLdloc(19),
               i => i.MatchLdfld(typeof(Rectangle), nameof(Rectangle.X)),
               i => i.MatchConvR4(),
               i => i.MatchLdloc0(),
               i => i.MatchCallvirt(out _)))
            {
                LogIlError("DynamicSpriteFont", "Couldn't navigate to before PushSprite");
                return;
            }
            cursor.Emit(OpCodes.Ldloc, 15);
            cursor.EmitDelegate(CharIsD);
            cursor.Emit(OpCodes.Brtrue, label);
        }

        private bool CharIsD(char c) => c == 'D' || c == 'd';

        private void ChangeMenuLogo(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(MoveType.Before,
                i => i.MatchLdarg0(),
                i => i.MatchLdloc0(),
                i => i.MatchLdloc1(),
                i => i.MatchLdcI4(0),
                i => i.MatchLdcI4(0)))
            { 
                LogIlError("MenuLoader", "Couldn't navigate to before logo draw");
                return;
            }
            cursor.Index += 2;
            cursor.EmitDelegate(SwapMenuLogo);
        }
        private Texture2D SwapMenuLogo(Texture2D icon)
        {
            if (MenuLoader.CurrentMenu.DisplayName == "tModLoader")
                icon = ModContent.Request<Texture2D>("Nod/Logo").Value;
            return icon;
        }
        public void LogIlError(string name, string reason)
        {
            Logger.Warn($"IL edit \"{name}\" failed! {reason}");
            SoundEngine.PlaySound(SoundID.DoorClosed);
        }
    }
}