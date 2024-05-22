using p4g64.riseOutfit.Native;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using System.Runtime.InteropServices;
using static p4g64.riseOutfit.Native.Party;
using static p4g64.riseOutfit.Utils;

namespace p4g64.riseOutfit.Menu;
internal unsafe class CostumeShop
{

    private IAsmHook _memberSelectYOffset;

    private IHook<SetupPartyMemberOptionsDelegate> _setupPartyMembersHook;

    internal CostumeShop(IReloadedHooks hooks)
    {
        SigScan("F3 44 0F 10 0D ?? ?? ?? ?? 66 41 3B 34 24", "Costume Member Select Y Offset", address =>
        {
            string[] function =
            {
                "use64",
                "xorps xmm9, xmm9", // Set the y offset to 0
            };
            _memberSelectYOffset = hooks.CreateAsmHook(function, address, AsmHookBehaviour.DoNotExecuteOriginal).Activate();
        });

        SigScan("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 41 56 48 83 EC 20 48 8B F9 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8D 9F ?? ?? ?? ??", "Facility::SetupPartyMemberOptions", address =>
        {
            _setupPartyMembersHook = hooks.CreateHook<SetupPartyMemberOptionsDelegate>(SetupPartyMemberOptions, address).Activate();
        });
    }

    private void SetupPartyMemberOptions(FacilityMenuInfo* menu)
    {
        _setupPartyMembersHook.OriginalFunction(menu);

        // Only add Rise in Croco Fur        
        if (menu->Menus[menu->CurrentMenu] != 5)
            return;

        if(IsPartyMemberAvailable(PartyMember.Rise))
        {
            // Add Rise to the list of party members
            for(int i = menu->NumPartyMembers; i > FindRiseIndex(menu); i--)
            {
                menu->PartyMembers[i] = menu->PartyMembers[i - 1];
                menu->PartyMembers[i - 1] = (short)PartyMember.Rise;
            }

            menu->NumPartyMembers++;
        }
    }

    // Find the index that rise should be put into the menu at
    private int FindRiseIndex(FacilityMenuInfo* menu)
    {
        for(int i = 0; i < menu->NumPartyMembers; i++)
        {
            if (menu->PartyMembers[i] > (short)PartyMember.Rise)
                return i;
        }
        return menu->NumPartyMembers;
    }

    private delegate void SetupPartyMemberOptionsDelegate(FacilityMenuInfo* menu);

    [StructLayout(LayoutKind.Explicit)]
    private struct FacilityMenuInfo
    {
        [FieldOffset(0x4c8)]
        internal fixed short PartyMembers[10];

        [FieldOffset(0x4dc)]
        internal short NumPartyMembers;

        [FieldOffset(0x4de)]
        internal short Thing;

        [FieldOffset(0x340)]
        internal fixed short Menus[14];

        [FieldOffset(0x35c)]
        internal int CurrentMenu;
    }
}