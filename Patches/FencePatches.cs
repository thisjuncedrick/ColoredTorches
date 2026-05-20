using HarmonyLib;
using StardewValley;
using System.Diagnostics.CodeAnalysis;

namespace ColoredTorches.Patches;

internal sealed class FencePatches
{
  public static void Apply(Harmony harmony)
  {
    ModEntry.Log($"{nameof(FencePatches)} Applied");

    harmony.Patch(
      original: AccessTools.Method(typeof(Fence), nameof(Fence.performObjectDropInAction)),
      prefix: new HarmonyMethod(typeof(FencePatches), nameof(PerformObjectDropInAction_Prefix))
    );
  }

  [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Required by Harmony")]
  private static bool PerformObjectDropInAction_Prefix(Fence __instance, Item dropInItem, bool probe, Farmer who, ref bool __result)
  {
    if (!TorchData.Contains(dropInItem.ItemId))
      return true;

    if (__instance.heldObject.Value != null || __instance.isGate.Value)
    {
      __result = false;
      return true;
    }

    if (!probe)
    {
      var torch = (StardewValley.Object)dropInItem.getOne();
      __instance.heldObject.Value = torch;
      __instance.Location.playSound("axe");
      torch.Location = __instance.Location;
      torch.initializeLightSource(__instance.TileLocation);
    }

    __result = true;
    return false;
  }

}
