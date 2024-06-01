using Reloaded.Hooks.ReloadedII.Interfaces;
using static p4g64.riseOutfit.Utils;

namespace p4g64.riseOutfit.Native;
internal unsafe class Flags
{
    private static byte** _flags;

    internal static void Initialise(IReloadedHooks hooks)
    {
        SigScan("48 8B 0D ?? ?? ?? ?? F6 81 ?? ?? ?? ?? 40", "FlagsPtr", address =>
        {
            _flags = (byte**)GetGlobalAddress(address + 3);
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
        GoldenEnding = 5187,
    }

}
