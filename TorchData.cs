using System.Diagnostics.CodeAnalysis;

namespace ColoredTorches;

internal static class TorchData
{
  private static readonly Dictionary<string, TorchSettings> Map = new();

  public static void Set(string id, TorchSettings settings)
  {
    Map[id] = settings;
  }

  public static void Clear()
  {
    Map.Clear();
  }

  public static bool TryGet(string id, [NotNullWhen(true)] out TorchSettings? settings)
    => Map.TryGetValue(id, out settings);
}
