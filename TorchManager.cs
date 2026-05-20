using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace ColoredTorches;

internal sealed class TorchManager
{
  private const string CPId = "ceddieeee.ColoredTorches_CP";

  private readonly Dictionary<long, string> PlayerLightCache = new();
  private static readonly Dictionary<string, Func<ModConfig, TorchColorConfig>> TorchMap = new()
  {
    [$"{CPId}_RedTorch"]    = c => c.Red,
    [$"{CPId}_OrangeTorch"] = c => c.Orange,
    [$"{CPId}_YellowTorch"] = c => c.Yellow,
    [$"{CPId}_GreenTorch"]  = c => c.Green,
    [$"{CPId}_BlueTorch"]   = c => c.Blue,
    [$"{CPId}_PurpleTorch"] = c => c.Purple,
  };

  public TorchManager(ModConfig config)
  {
    RebuildCache(config);
  }

  public void RebuildCache(ModConfig config)
  {
    TorchData.Clear();

    ModEntry.Log("Rebuilding torch cache...");

    foreach (var (id, selector) in TorchMap)
    {
      var cfg = selector(config);
      Add(id, cfg.RGB, cfg.Alpha, cfg.Radius);
    }
  }

  private static void Add(string id, string rgb, float alpha, float radius)
  {
    if (!TryParseColor(rgb, out Color color))
    {
      ModEntry.Log($"Invalid RGB '{rgb}' for {id}, defaulting to White", LogLevel.Error);
      color = Color.White;
    }

    TorchData.Set(id, new TorchSettings
    {
      Color = color,
      InvertedColor = InvertColor(color, alpha),
      Radius = radius,
    });
  }

  public void OnTick(object? sender, UpdateTickedEventArgs e)
  {
    if (!e.IsMultipleOf(7))
      return;

    var location = Game1.currentLocation;
    if (location is null)
      return;

    // Placed torches
    foreach (var (_, obj) in location.objects.Pairs)
    {
      if (obj is Fence fence && fence.heldObject.Value is { } held)
      {
        if (TorchData.TryGet(held.ItemId, out var fenceTorchSettings)
            && held.lightSource is { } fenceLightSource
            && Game1.currentLightSources.TryGetValue(fenceLightSource.Id, out var fenceLight)
        )
        {
          if (fenceLight.radius.Value != fenceTorchSettings.Radius)
            fenceLight.radius.Value = fenceTorchSettings.Radius;

          if (fenceLight.color.Value != fenceTorchSettings.InvertedColor)
            fenceLight.color.Value = fenceTorchSettings.InvertedColor;
        }

        continue;
      }

      // Sprinkler "holding" custom torches
      if (obj.IsSprinkler() && obj.SpecialVariable == 999999 && obj.modData.TryGetValue(ModEntry.MODDATA_KEY, out string? torchId))
      {
        if (TorchData.TryGet(torchId, out var sprinklerTorchSettings)
            && obj.lightSource is { } sprinklerLightSource  
            && Game1.currentLightSources.TryGetValue(sprinklerLightSource.Id, out var sprinklerLight)
        )
        {
          if (sprinklerLight.radius.Value != sprinklerTorchSettings.Radius)
            sprinklerLight.radius.Value = sprinklerTorchSettings.Radius;

          if (sprinklerLight.color.Value != sprinklerTorchSettings.InvertedColor)
            sprinklerLight.color.Value = sprinklerTorchSettings.InvertedColor;
        }

        continue;
      }

      // Standalone torch
      if (TorchData.TryGet(obj.ItemId, out var settings)
          && obj.lightSource is { } source
          && Game1.currentLightSources.TryGetValue(source.Id, out var light)
      )
      {
        if (light.radius.Value != settings.Radius)
          light.radius.Value = settings.Radius;

        if (light.color.Value != settings.InvertedColor)
          light.color.Value = settings.InvertedColor;
      }
    }

    // Player held torches
    foreach (var farmer in location.farmers)
    {
      if (farmer.CurrentItem is { } item
          && TorchData.TryGet(item.ItemId, out var settings)
          && TryGetPlayerLight(farmer.UniqueMultiplayerID, out var light)
      )
      {
        if (light.color.Value != settings.InvertedColor)
          light.color.Value = settings.InvertedColor;
      }
    }
  }

  public void OnWarped(object? sender, WarpedEventArgs e)
  {
    var player = e.Player;

    var toRemove = new List<string>();

    foreach (var (key, light) in e.OldLocation.sharedLights.Pairs)
    {
      if (light.PlayerID == player.UniqueMultiplayerID)
        toRemove.Add(key);
    }

    int removed = 0;

    foreach (var id in toRemove)
    {
      e.OldLocation.sharedLights.Remove(id);
      removed++;
    }

    if (removed > 0)
      ModEntry.Log($"Warp cleanup: removed {removed} light(s) for player {player.UniqueMultiplayerID}");

    PlayerLightCache.Remove(player.UniqueMultiplayerID);
  }

  /* Helper */
  private bool TryGetPlayerLight(long playerId, out LightSource light)
  {
    if (PlayerLightCache.TryGetValue(playerId, out var id))
    {
      if (Game1.currentLightSources.TryGetValue(id, out var found))
      {
        light = found!;
        return true;
      }
    }

    foreach (var (_, lightSource) in Game1.currentLightSources)
    {
      if (lightSource.PlayerID == playerId)
      {
        PlayerLightCache[playerId] = lightSource.Id;
        light = lightSource;
        return true;
      }
    }

    light = null!;
    return false;
  }

  private static bool TryParseColor(string value, out Color color)
  {
    color = Color.White;

    var parts = value.Split(',');
    if (parts.Length != 3)
      return false;

    if (int.TryParse(parts[0].Trim(), out int r) &&
        int.TryParse(parts[1].Trim(), out int g) &&
        int.TryParse(parts[2].Trim(), out int b))
    {
      color = new Color(
          Math.Clamp(r, 0, 255),
          Math.Clamp(g, 0, 255),
          Math.Clamp(b, 0, 255)
      );
      return true;
    }

    return false;
  }

  private static Color InvertColor(Color c, float alpha)
  {
    return new Color(
      255 - c.R,
      255 - c.G,
      255 - c.B,
      (int)(alpha * 255f)
    );
  }
}
