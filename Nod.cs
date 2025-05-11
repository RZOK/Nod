using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.IO;
using System.Reflection;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Nod
{
    public class Nod : Mod
    {
        private static ILHook SpriteBatch_DrawStringHook;

        public override void Load()
        {
            MethodInfo[] methods = typeof(SpriteBatch).GetMethods();
            SpriteBatch_DrawStringHook ??= new ILHook(typeof(SpriteBatch).GetMethod("DrawString", BindingFlags.Instance | BindingFlags.Public, new Type[9] { typeof(SpriteFont), typeof(string), typeof(Vector2), typeof(Color), typeof(float), typeof(Vector2), typeof(Vector2), typeof(SpriteEffects), typeof(float) }), DontDrawD);
        }
        public override void Unload()
        {
            SpriteBatch_DrawStringHook.Undo();
        }

        public void DontDrawD(ILContext il)
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
                LogIlError("No D", "Couldn't navigate to before kerning increase");
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
                LogIlError("No D", "Couldn't navigate to before PushSprite");
                return;
            }
            cursor.Emit(OpCodes.Ldloc, 15);
            cursor.EmitDelegate(CharIsD);
            //cursor.Emit(OpCodes.Brtrue, label);
            DumpILNotStupid(il);
        }
        private void DumpILNotStupid(ILContext il)
        {
            string methodName = il.Method.FullName.Replace(':', '_').Replace('<', '[').Replace('>', ']');
            if (methodName.Contains('?')) // MonoMod IL copies are created with mangled names like DMD<Terraria.Player::beeType>?38504011::Terraria.Player::beeType(Terraria.Player)
                methodName = methodName[(methodName.LastIndexOf('?') + 1)..];
            methodName = string.Join("_", methodName.Split(Path.GetInvalidFileNameChars())); // Catch any other illegal characters, just in case.

            string filePath = Path.Combine(Logging.LogDir, "ILDumps", Name, methodName.Split("_")[0] + ".txt");
            string folderPath = Path.GetDirectoryName(filePath);

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);
            File.WriteAllText(filePath, il.ToString());
        }
        private void CharIsD(char c) => Console.Write(c);

        public void LogIlError(string name, string reason)
        {
            Logger.Warn($"IL edit \"{name}\" failed! {reason}");
            SoundEngine.PlaySound(SoundID.DoorClosed);
        }
    }
}