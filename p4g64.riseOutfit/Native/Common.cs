using Reloaded.Hooks.ReloadedII.Interfaces;
using static p4g64.riseOutfit.Utils;

namespace p4g64.riseOutfit.Native;
internal unsafe class Common
{
    private static byte** _flags;

    private static short** _date;

    internal static short Date => **_date;

    internal static void Initialise(IReloadedHooks hooks)
    {
        SigScan("48 8B 0D ?? ?? ?? ?? F6 81 ?? ?? ?? ?? 40", "FlagsPtr", address =>
        {
            _flags = (byte**)GetGlobalAddress(address + 3);
        });

        SigScan("4C 8B 15 ?? ?? ?? ?? 8B 4C 24 ??", "DatePtr", address =>
        {
            _date = (short**)GetGlobalAddress(address + 3);
        });
    }

    internal static bool CheckFlag(int flag)
    {
        return ((*_flags)[flag / 8] >> (flag & 0x1f) & 1) != 0;
    }

    internal static bool CheckFlag(Flag flag)
    {
        return CheckFlag((int)flag);
    }

    internal enum Flag
    {
        NewGamePlus = 2048,
        GoldenEnding = 5187,
        StripteaseBonusBossAvailable = 3760, // On if you haven't beat the boss yet, off if you have
        StripteaseBonusBossItemAvailable = 3752, // On if you haven't collected the item yet, off otherwise (only applicable if you have actually beat the boss)
    }

}
