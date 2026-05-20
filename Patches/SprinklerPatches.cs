using HarmonyLib;
using StardewModdingAPI;

using StardewValley;

using System.Reflection;
using System.Reflection.Emit;

using SObject = StardewValley.Object;

namespace ColoredTorches.Patches;

internal sealed class SprinklerPatches
{
  private static readonly MethodInfo QualifiedItemIdGetter = 
    AccessTools.PropertyGetter(typeof(Item), nameof(Item.QualifiedItemId));
  
  private static readonly MethodInfo StringEquality = 
    AccessTools.Method(typeof(string), "op_Equality");
  
  private static readonly MethodInfo IsValidTorchMethod = 
    AccessTools.Method(typeof(SprinklerPatches), nameof(IsValidTorch));

  private static readonly MethodInfo GetAttachTorchIdMethod = 
    AccessTools.Method(typeof(SprinklerPatches), nameof(GetAttachedTorchId));
  
  public static void Apply(Harmony harmony)
  {
    ModEntry.Log($"{nameof(SprinklerPatches)} Applied");

    harmony.Patch(
      original: AccessTools.Method(typeof(SObject), nameof(SObject.performObjectDropInAction)),
      transpiler: new HarmonyMethod(typeof(SprinklerPatches), nameof(PerformObjectDropInAction_Transpiler)),
      postfix: new HarmonyMethod(typeof(SprinklerPatches), nameof(PerformObjectDropInAction_Postfix))
    );

    harmony.Patch(
        original: AccessTools.Method(typeof(SObject), nameof(SObject.performToolAction)),
        transpiler: new HarmonyMethod(typeof(SprinklerPatches), nameof(PerformToolAction_Transpiler))
    );
  }

  private static IEnumerable<CodeInstruction> PerformObjectDropInAction_Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    var matcher = new CodeMatcher(instructions);

    // Look for this excact condition check (obj.QualifiedItemId == "(O)93")
    matcher.MatchStartForward(
      new CodeMatch(OpCodes.Callvirt, QualifiedItemIdGetter),
      new CodeMatch(i => i.LoadsConstant("(O)93")),
      new CodeMatch(OpCodes.Call, StringEquality)
    );

    if (!matcher.IsValid)
    {
      ModEntry.Log($"Patch {nameof(PerformObjectDropInAction_Transpiler)} failed: IL pattern not found.", LogLevel.Warn);
      return instructions;
    }

    // Replace torch item check with a call to our helper that checks if the object
    // is a torch plus our custom ones.
    return matcher
        .RemoveInstructions(3)
        .Insert(new CodeInstruction(OpCodes.Call, IsValidTorchMethod))
        .InstructionEnumeration();
  }

  private static void PerformObjectDropInAction_Postfix(SObject __instance, Item dropInItem, bool probe, bool __result)
  {
    if (probe || !__result || !__instance.IsSprinkler() || __instance.SpecialVariable != 999999) 
      return;
    
    if (dropInItem is not SObject obj || !TorchData.TryGet(obj.ItemId, out _))
      return;

    ModEntry.Log($"Attached torch {obj.ItemId} to sprinkler at {__instance.TileLocation}");
    __instance.modData[ModEntry.MODDATA_KEY] = obj.ItemId;
  }

  private static IEnumerable<CodeInstruction> PerformToolAction_Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    var matcher = new CodeMatcher(instructions);

    // Look for sprinkler's special variable check.
    matcher.SearchForward(i => i.LoadsConstant(999999));
    if (!matcher.IsValid)
    {
      ModEntry.Log($"Patch {nameof(PerformToolAction_Transpiler)} failed: Special variable check not found", LogLevel.Warn);
      return instructions;
    }

    // Scan forward a until the hardcoded torch string literal assignment is found.
    matcher.SearchForward(i => i.LoadsConstant("(O)93"));
    if (!matcher.IsValid) 
    {
      ModEntry.Log($"Patch {nameof(PerformToolAction_Transpiler)} failed: Torch string check not found", LogLevel.Warn);
      return instructions;
    }

    // Replace the static string check a with a proxy method call that accepts 'this' sprinkler
    matcher.RemoveInstruction() 
           .Insert(
               new CodeInstruction(OpCodes.Ldarg_0), 
               new CodeInstruction(OpCodes.Call, GetAttachTorchIdMethod)
           );

    return matcher.InstructionEnumeration();
  }

  /* Helper */
  private static string GetAttachedTorchId(SObject sprinkler) =>
    sprinkler.modData.TryGetValue(ModEntry.MODDATA_KEY, out string? torchId) && TorchData.Contains(torchId)
      ? torchId
      : "(O)93";

  private static bool IsValidTorch(Item item) =>
     item is SObject obj && (obj.QualifiedItemId == "(O)93" || TorchData.Contains(obj.ItemId));
}
