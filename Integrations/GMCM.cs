using StardewModdingAPI;

namespace ColoredTorches.Integrations;

internal static class GMCM
{
  private static readonly (Func<string> Title, Func<ModConfig, TorchColorConfig> Get)[] Entries =
    new (Func<string> Title, Func<ModConfig, TorchColorConfig> Get)[]
    {
      (I18n.Config_Color_Red_Title,    c => c.Red),
      (I18n.Config_Color_Orange_Title, c => c.Orange),
      (I18n.Config_Color_Yellow_Title, c => c.Yellow),
      (I18n.Config_Color_Green_Title,  c => c.Green),
      (I18n.Config_Color_Blue_Title,   c => c.Blue),
      (I18n.Config_Color_Purple_Title, c => c.Purple),
    };

  public static void Register(
    IModHelper helper, 
    IManifest manifest, 
    Func<ModConfig> getConfig, 
    Action reset, 
    Action save
  )
  {
    var gmcm = helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");

    if (gmcm is null)
      return;

    gmcm.Register(manifest, reset, save);

    foreach (var (title, get) in Entries)
    {
      Create( 
        api:       gmcm, 
        mod:       manifest, 
        title:     title,
        getRGB:    () => get(getConfig()).RGB, 
        setRGB:    v => get(getConfig()).RGB = v,
        getAlpha:  () => get(getConfig()).Alpha, 
        setAlpha:  v => get(getConfig()).Alpha = v,
        getRadius: () => get(getConfig()).Radius, 
        setRadius: v => get(getConfig()).Radius = v);
    }
  }

  private static void Create(
    IGenericModConfigMenuApi api,
    IManifest mod,
    Func<string> title,
    Func<string> getRGB,
    Action<string> setRGB,
    Func<float> getAlpha,
    Action<float> setAlpha,
    Func<float> getRadius,
    Action<float> setRadius
  )
  {
    api.AddSectionTitle(
      mod:  mod, 
      text: title
    );

    api.AddTextOption(
      mod:      mod, 
      getValue: getRGB, 
      setValue: setRGB,
      name:     () => I18n.Config_Option_RGB_Name(),
      tooltip:  () => I18n.Config_Option_RGB_Tooltip()
    );

    api.AddNumberOption(
      mod:         mod, 
      getValue:    getAlpha, 
      setValue:    setAlpha,
      min:         0f, 
      max:         1f, 
      interval:    0.01f,
      formatValue: v => $"{v * 100:0}%",
      name:        () => I18n.Config_Option_Intensity_Name(),
      tooltip:     () => I18n.Config_Option_Intensity_Tooltip()
    );

    api.AddNumberOption(
      mod:      mod, 
      getValue: getRadius, 
      setValue: setRadius,
      min:      0.1f, 
      max:      5f, 
      interval: 0.05f,
      name:     () => I18n.Config_Option_Radius_Name(),
      tooltip:  () => I18n.Config_Option_Radius_Tooltip()
    );
  }
}
