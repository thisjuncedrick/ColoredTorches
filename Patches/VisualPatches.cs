using HarmonyLib;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using SObject = StardewValley.Object;

namespace ColoredTorches.Patches;

internal sealed class VisualPatches
{
  private static readonly FieldInfo MouseCursors = 
    AccessTools.Field(typeof(Game1), nameof(Game1.mouseCursors));
  private static readonly MethodInfo ColorWhite = 
    AccessTools.PropertyGetter(typeof(Color), nameof(Color.White));
  private static readonly MethodInfo ColorPaleGoldenrod = 
    AccessTools.PropertyGetter(typeof(Color), nameof(Color.PaleGoldenrod));
  private static readonly MethodInfo DrawBasicTorchMethod = 
    AccessTools.Method(typeof(Torch), nameof(Torch.drawBasicTorch));

  private static readonly MethodInfo DrawBasicTorchPatchMethod = 
    AccessTools.Method(typeof(VisualPatches), nameof(DrawBasicTorchPatch));
  private static readonly MethodInfo GetTorchTextureMethod = 
    AccessTools.Method(typeof(VisualPatches), nameof(GetTorchTexture));
  private static readonly MethodInfo GetTorchColorMethod = 
    AccessTools.Method(typeof(VisualPatches), nameof(GetTorchColor));

  public static void Apply(Harmony harmony)
  {
    ModEntry.Log($"{nameof(VisualPatches)} Applied");

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

    harmony.Patch(
      original: AccessTools.Method(typeof(SObject), nameof(SObject.draw),
        new[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float) }),
      transpiler: new HarmonyMethod(typeof(VisualPatches), nameof(SObjectDraw_Transpiler))
    );
  }

  private static IEnumerable<CodeInstruction> TorchDraw_Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    var matcher = new CodeMatcher(instructions);

    // Only loop 2 times because there's only 2 draw call that we want. The base torch draw and the halo draw
    for (int i = 0; i < 2; i++)
    {
      matcher.SearchForward(i => i.LoadsField(MouseCursors));
      if (!matcher.IsValid)
      {
        ModEntry.Log($"Patch {nameof(TorchDraw_Transpiler)} failed: MouseCursors field not found", LogLevel.Warn);
        break;
      }

      // Patch Texture: Replace Game1.mouseCursors with GetTorchTexture(thois)
      // Doesn't matter if its glow or flame since they both use the same texture, and the color is what differentiates them.
      matcher.RemoveInstruction()
             .Insert(
               new CodeInstruction(OpCodes.Ldarg_0),
               new CodeInstruction(OpCodes.Call, GetTorchTextureMethod)
             );


      // Search for the next color call, whether it's White OR Goldenrod (Halo)
      matcher.SearchForward(i => i.Calls(ColorWhite) || i.Calls(ColorPaleGoldenrod));
      if (!matcher.IsValid)
      {
        ModEntry.Log($"Patch {nameof(TorchDraw_Transpiler)} failed: Color call not found", LogLevel.Warn);
        break;
      }

      // Patch color: Replace Color call with ItemHelper.GetLightColor(this)
      matcher.RemoveInstruction()
             .Insert(
               new CodeInstruction(OpCodes.Ldarg_0),
               new CodeInstruction(OpCodes.Call, GetTorchColorMethod)
             );
    }

    return matcher.InstructionEnumeration();
  }

  private static IEnumerable<CodeInstruction> SObjectDraw_Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    var matcher = new CodeMatcher(instructions);

    for (int i = 0; i < 2; i++)
    {
      matcher.SearchForward(i => i.Calls(DrawBasicTorchMethod));
      if (!matcher.IsValid)
      {
        ModEntry.Log($"Patch {nameof(SObjectDraw_Transpiler)} failed: DrawBasicTorch call not found", LogLevel.Warn);
        break;
      }

      matcher.Insert(new CodeInstruction(OpCodes.Ldarg_0))
             .Advance(1)
             .RemoveInstruction()
             .Insert(new CodeInstruction(OpCodes.Call, DrawBasicTorchPatchMethod));
    }

    return matcher.InstructionEnumeration();
  }

  [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Required by Harmony")]
  private static void DrawBasicTorchPatch(SpriteBatch spriteBatch, float x, float y, float layerDepth, float alpha, SObject sprinkler)
  {
    if (sprinkler.modData.TryGetValue(ModEntry.MODDATA_KEY, out string torchId)
        && TorchData.TryGet(torchId, out var settings)) 
      DrawBasicTorchProxy(spriteBatch, x, y, layerDepth, settings.Color);
    else
      Torch.drawBasicTorch(spriteBatch, x, y, layerDepth);
  }

  /* Helper */
  private static Texture2D GetTorchTexture(Item i) =>
    (i != null && TorchData.Contains(i.ItemId))
      ? ModEntry.MouseCursors
      : Game1.mouseCursors;

  private static Color GetTorchColor(Item i) =>
    (i != null && TorchData.TryGet(i.ItemId, out var settings))
      ? settings.Color
      : Color.White;

  private static void DrawBasicTorchProxy(SpriteBatch b, float x, float y, float layerDepth, Color color)
  {
    Rectangle value = new(336, 48, 16, 16);
    value.Y += 8;
    value.Height /= 2;
    
    b.Draw(
      texture:    Game1.objectSpriteSheet,
      position:   Game1.GlobalToLocal(Game1.viewport, new Vector2(x, y + 32f)),
      sourceRectangle: value,
      color:      Color.White,
      rotation:   0f,
      origin:     Vector2.Zero,
      scale:      4f,
      effects:    SpriteEffects.None,
      layerDepth: layerDepth
    );

    b.Draw(
      texture:    Game1.mouseCursors,
      position:   Game1.GlobalToLocal(Game1.viewport, new Vector2(x + 32f + 2f, y + 16f)),
      sourceRectangle: new Rectangle(88, 1779, 30, 30),
      color:      color * (Game1.currentLocation.IsOutdoors ? 0.35f : 0.43f),
      rotation:   0f,
      origin:     new Vector2(15f, 15f), 
      scale:      4f + (float)(64.0 * Math.Sin((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(x * 777f) + (double)(y * 9746f)) % 3140.0 / 1000.0) / 50.0),
      effects:    SpriteEffects.None,
      layerDepth: 1f
    );

    value.X = 276 + (int)((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(x * 3204f) + (double)(y * 49f)) % 700.0 / 100.0) * 8;
    value.Y = 1965;
    value.Width = 8;
    value.Height = 8;

    b.Draw(
      texture:    ModEntry.MouseCursors,
      position:   Game1.GlobalToLocal(Game1.viewport, new Vector2(x + 32f + 4f, y + 16f + 4f)),
      sourceRectangle: value,
      color:      color * 0.75f,
      rotation:   0f,
      origin:     new Vector2(4f, 4f),
      scale:      3f,
      effects:    SpriteEffects.None,
      layerDepth: layerDepth + 0.0001f
    );
  }
}
