using Microsoft.Xna.Framework;

namespace ColoredTorches;

internal sealed class TorchSettings
{
  public Color Color { get; init; }
  public Color InvertedColor { get; init; }
  public float Radius { get; init; }
}
