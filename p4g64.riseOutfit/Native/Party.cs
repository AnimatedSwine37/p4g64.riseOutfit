using Reloaded.Hooks.ReloadedII.Interfaces;

using static p4g64.riseOutfit.Utils;

namespace p4g64.riseOutfit.Native;
internal class Party
{
    internal static IsPartyMemberAvailableDelegate IsPartyMemberAvailable;

    internal static void Initialise(IReloadedHooks hooks)
    {
        SigScan("40 53 48 83 EC 20 0F BF D9 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 33 D2", "IsPartyMemberAvailable", address =>
        {
            IsPartyMemberAvailable = hooks.CreateWrapper<IsPartyMemberAvailableDelegate>(address, out _);
        });
    }

    internal delegate bool IsPartyMemberAvailableDelegate(PartyMember member);

    internal enum PartyMember : short
    {
        None,
        Protagonist,
        Yosuke,
        Chie,
        Yukiko,
        Rise,
        Kanji,
        Naoto,
        Teddie
    }

}
