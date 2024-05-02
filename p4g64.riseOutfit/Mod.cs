using p4g64.riseOutfit.Configuration;
using p4g64.riseOutfit.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using System.IO;
using static p4g64.riseOutfit.Utils;
using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace p4g64.riseOutfit;
/// <summary>
/// Your mod logic goes here.
/// </summary>
public class Mod : ModBase // <= Do not Remove.
{
    /// <summary>
    /// Provides access to the mod loader API.
    /// </summary>
    private readonly IModLoader _modLoader;

    /// <summary>
    /// Provides access to the Reloaded.Hooks API.
    /// </summary>
    /// <remarks>This is null if you remove dependency on Reloaded.SharedLib.Hooks in your mod.</remarks>
    private readonly IReloadedHooks? _hooks;

    /// <summary>
    /// Provides access to the Reloaded logger.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Entry point into the mod, instance that created this class.
    /// </summary>
    private readonly IMod _owner;

    /// <summary>
    /// Provides access to this mod's configuration.
    /// </summary>
    private Config _configuration;

    /// <summary>
    /// The configuration of the currently executing mod.
    /// </summary>
    private readonly IModConfig _modConfig;

    private IAsmHook _outfitHook;

    private IReverseWrapper<GetRiseOutfitDelegate> _getRiseOutfitReverseWrapper;

    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _logger = context.Logger;
        _owner = context.Owner;
        _configuration = context.Configuration;
        _modConfig = context.ModConfig;

        Initialise(_logger, _configuration, _modLoader);

        SigScan("E8 ?? ?? ?? ?? 48 8D 8D ?? ?? ?? ?? 48 C7 C0 FF FF FF FF 0F 1F 44 ?? 00", "Rise Outfit", address =>
        {
            string[] function =
            {
                "use64",
                "sub rsp, 40",
                "push rcx\npush r8\npush r9\npush r10\npush r11",
                "mov rcx, rdx",
                _hooks!.Utilities.GetAbsoluteCallMnemonics(GetRiseOutfit, out _getRiseOutfitReverseWrapper),
                "mov rdx, rax",
                "pop r11\npop r10\npop r9\npop r8\npop rcx",
                "add rsp, 40"
            };

            _outfitHook = _hooks.CreateAsmHook(function, address, AsmHookBehaviour.ExecuteFirst).Activate();
        });
    }

    private string GetRiseOutfit(string currentStr)
    {
        int bedIndex = currentStr.IndexOf("ASSIST_RISE");
        if (bedIndex == -1) return currentStr;

        var newBed = currentStr.Substring(0, bedIndex) + _configuration.RiseBed;
        Log($"Switched outfit bed from {currentStr} to {newBed}");
        return newBed;
    }

    private delegate string GetRiseOutfitDelegate(string currentStr);

    #region Standard Overrides
    public override void ConfigurationUpdated(Config configuration)
    {
        // Apply settings from configuration.
        // ... your code here.
        _configuration = configuration;
        _logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");
    }
    #endregion

    #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod() { }
#pragma warning restore CS8618
    #endregion
}