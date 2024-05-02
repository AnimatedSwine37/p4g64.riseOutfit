using p4g64.riseOutfit.Template.Configuration;
using System.ComponentModel;

namespace p4g64.riseOutfit.Configuration;
public class Config : Configurable<Config>
{
    [DisplayName("Rise Bed File")]
    [Description("The name of the bed file to use for Rise's assist.")]
    [DefaultValue("ASSIST_RISE_S.BED")]
    public string RiseBed { get; set; } = "ASSIST_RISE_S.BED";

    [DisplayName("Debug Mode")]
    [Description("Logs additional information to the console that is useful for debugging.")]
    [DefaultValue(false)]
    public bool DebugEnabled { get; set; } = false;
}

/// <summary>
/// Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
/// Override elements in <see cref="ConfiguratorMixinBase"/> for finer control.
/// </summary>
public class ConfiguratorMixin : ConfiguratorMixinBase
{
    // 
}