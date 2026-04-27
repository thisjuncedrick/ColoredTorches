using HarmonyLib;
using StardewValley;

namespace ColoredTorches.Patches;

internal sealed class FencePatches
{
  public static void Apply(Harmony harmony)
  {
    harmony.Patch(
      original: AccessTools.Method(typeof(Fence), nameof(Fence.performObjectDropInAction)),
      prefix: new HarmonyMethod(typeof(FencePatches), nameof(PerformObjectDropInAction_Prefix))
    );
  }

  private static bool PerformObjectDropInAction_Prefix(Fence __instance, Item dropInItem, bool probe, Farmer who, ref bool __result)
  {
    if (!TorchData.TryGet(dropInItem.ItemId, out var _))
      return true; // not our torch let original run

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
