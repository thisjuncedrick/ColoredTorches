using HarmonyLib;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewValley;

using System.Reflection;
using System.Reflection.Emit;
namespace ColoredTorches.Patches;

internal sealed class VisualPatches
{
  private static readonly FieldInfo MouseCursors = AccessTools.Field(typeof(Game1), nameof(Game1.mouseCursors));
  private static readonly MethodInfo ColorWhite = AccessTools.PropertyGetter(typeof(Color), nameof(Color.White));
  private static readonly MethodInfo ColorPaleGoldenrod = AccessTools.PropertyGetter(typeof(Color), nameof(Color.PaleGoldenrod));

  private static readonly MethodInfo GetTexture = AccessTools.Method(typeof(VisualPatches), nameof(GetCursorTexture));
  private static readonly MethodInfo GetFireColor = AccessTools.Method(typeof(VisualPatches), nameof(GetFlameColor));

  public static void Apply(Harmony harmony)
  {
    var torchDrawTranspiler = new HarmonyMethod(typeof(VisualPatches), nameof(TorchDraw_Transpiler));
    harmony.Patch(
      original: AccessTools.Method(typeof(Torch), nameof(Torch.draw),
        new[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float), typeof(float) }),
      transpiler: torchDrawTranspiler
    );

    harmony.Patch(
      original: AccessTools.Method(typeof(Torch), nameof(Torch.draw),
        new[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float) }),
      transpiler: torchDrawTranspiler
    );
  }

  private static IEnumerable<CodeInstruction> TorchDraw_Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    var matcher = new CodeMatcher(instructions);

    // Only loop 2 times because there's only 2 draw call that we want. The base torch draw and the halo draw
    for (int i = 0; i < 2; i++)
    {
      matcher.SearchForward(i => i.LoadsField(MouseCursors));
      if (!matcher.IsValid) break;

      // Patch Texture: Replace Game1.mouseCursors with GetCursorTexture(thois)
      // Doesn't matter if its glow or flame since they both use the same texture, and the color is what differentiates them.
      matcher.RemoveInstruction()
             .Insert(
               new CodeInstruction(OpCodes.Ldarg_0),
               new CodeInstruction(OpCodes.Call, GetTexture)
             );


      // Search for the next color call, whether it's White OR Goldenrod (Halo)
      matcher.SearchForward(i => i.Calls(ColorWhite) || i.Calls(ColorPaleGoldenrod));
      if (!matcher.IsValid) break;

      // Patch color: Replace Color call with ItemHelper.GetLightColor(this)
      matcher.RemoveInstruction()
             .Insert(
               new CodeInstruction(OpCodes.Ldarg_0),
               new CodeInstruction(OpCodes.Call, GetFireColor)
             );
    }

    return matcher.InstructionEnumeration();
  }

  /* Helper */
  private static Texture2D GetCursorTexture(Item i)
  {
    if (i != null && TorchData.TryGet(i.ItemId, out _))
      return ModEntry.MouseCursors;

    return Game1.mouseCursors;
  }

  private static Color GetFlameColor(Item i) 
  {
    if (i != null && TorchData.TryGet(i.ItemId, out var settings))
      return settings.Color;

    return Color.White;
  }
}
