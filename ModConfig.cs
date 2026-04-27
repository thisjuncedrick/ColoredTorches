namespace ColoredTorches;

internal sealed class TorchColorConfig
{
  public string RGB { get; set; } = "255, 255, 255";
  public float Alpha { get; set; } = 1f;
  public float Radius { get; set; } = 1.25f;
}

internal sealed class ModConfig
{
  public TorchColorConfig Red { get; set; } = new() { RGB = "255, 0, 100" };
  public TorchColorConfig Orange { get; set; } = new() { RGB = "255, 79, 0" };
  public TorchColorConfig Yellow { get; set; } = new() { RGB = "255, 255, 0" };
  public TorchColorConfig Green { get; set; } = new() { RGB = "15, 255, 80" };
  public TorchColorConfig Blue { get; set; } = new() { RGB = "0, 191, 255" };
  public TorchColorConfig Purple { get; set; } = new() { RGB = "255, 0, 255" };
}