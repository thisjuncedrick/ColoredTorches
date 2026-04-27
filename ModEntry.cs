using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI;
using StardewModdingAPI.Events;

using StardewValley;
using ColoredTorches.Integrations;
using ColoredTorches.Patches;

namespace ColoredTorches;

public class ModEntry : Mod
{

#if DEBUG
  private const LogLevel logLevel = LogLevel.Debug;
#else
  private const LogLevel logLevel = LogLevel.Trace;
#endif

  private ModConfig Config = null!;
  private TorchManager Manager = null!;
  private static IMonitor ModMonitor = null!;

  internal const string MODDATA_KEY = "ceddieeee.ColoredTorches/TorchId";
  internal const string CONTENT_PACK_ID = "ceddieeee.ColoredTorches_CP";

  internal static Texture2D MouseCursors = null!;


  public override void Entry(IModHelper helper)
  {
    I18n.Init(helper.Translation);

    MouseCursors = Helper.ModContent.Load<Texture2D>("assets/Cursors.png");

    Config = helper.ReadConfig<ModConfig>();
    ModMonitor = Monitor;

    Manager = new TorchManager(Config); 
    
    var harmony = new Harmony(ModManifest.UniqueID);
    VisualPatches.Apply(harmony);
    FencePatches.Apply(harmony);

    helper.Events.GameLoop.GameLaunched    += OnGameLaunched;
    helper.Events.GameLoop.SaveLoaded      += OnSaveLoaded;
    helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;

  }

  private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
  {
    GMCM.Register(
      helper: Helper,
      manifest: ModManifest,
      getConfig: () => Config,
      reset: () => {
        Config = new ModConfig();
        Manager.RebuildCache(Config);
      },
      save: () => {
        Helper.WriteConfig(Config);
        Manager.RebuildCache(Config);
      }
    );
  }


  private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
  {
    Helper.Events.GameLoop.UpdateTicked += Manager.OnTick;
    Helper.Events.Player.Warped += Manager.OnWarped;
  }

  private void OnReturnedToTitle(object? s, ReturnedToTitleEventArgs e)
  {
    Helper.Events.GameLoop.UpdateTicked -= Manager.OnTick;
    Helper.Events.Player.Warped -= Manager.OnWarped;
  }

  internal static void Log(string message, LogLevel level = logLevel)
  {
    ModMonitor.Log(message, level);
  }
}
