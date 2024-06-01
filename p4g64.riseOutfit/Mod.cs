using p4g64.riseOutfit.Configuration;
using p4g64.riseOutfit.Menu;
using p4g64.riseOutfit.Native;
using p4g64.riseOutfit.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using static p4g64.riseOutfit.Native.Party;
using static p4g64.riseOutfit.Utils;
using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace p4g64.riseOutfit;
/// <summary>
/// Your mod logic goes here.
/// </summary>
public unsafe class Mod : ModBase // <= Do not Remove.
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

    private IAsmHook _aoaOutfitHook;
    private IAsmHook _assistOutfitHook;
    private IAsmHook _equipMenuHook;

    private IHook<SetupEquipMenuInfoDelegate> _setupEquipMenuHook;

    private PartyInfo** _partyInfoPtr;
    private PartyInfo* _partyInfo => *_partyInfoPtr;

    private byte** _itemsPtr;
    private byte* _items => *_itemsPtr;

    private FacilityFtds** _facilityFtdsPtr;
    private FacilityFtds* _facilityFtds => *_facilityFtdsPtr;

    private IReverseWrapper<GetRiseOutfitDelegate> _getRiseOutfitReverseWrapper;

    private CostumeShop _costumeShop;

    public Mod(ModContext context)
    {
        //Debugger.Launch();
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _logger = context.Logger;
        _owner = context.Owner;
        _configuration = context.Configuration;
        _modConfig = context.ModConfig;

        Initialise(_logger, _configuration, _modLoader);
        Party.Initialise(_hooks!);
        Flags.Initialise(_hooks!);

        _costumeShop = new CostumeShop(_hooks!);

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

        SigScan("E8 ?? ?? ?? ?? 48 8D 8D ?? ?? ?? ?? 48 C7 C0 FF FF FF FF 0F 1F 44 ?? 00", "Rise AoA Outfit", address =>
        {
            _aoaOutfitHook = _hooks.CreateAsmHook(function, address, AsmHookBehaviour.ExecuteFirst).Activate();
        });

        SigScan("E8 ?? ?? ?? ?? 48 8D 8D ?? ?? ?? ?? 48 C7 C0 FF FF FF FF 0F 1F 40 00", "Rise Assist Outfit", address =>
        {
            _assistOutfitHook = _hooks.CreateAsmHook(function, address, AsmHookBehaviour.ExecuteFirst).Activate();
        });

        SigScan("66 83 FB 05 74 ?? 0F B7 CB E8 ?? ?? ?? ?? 85 C0 74 ?? 49 0F BF C7", "Setup Equip Menu", address =>
        {
            // We're removing the cmp and jz, that movzx is some original code
            var function = new string[]
            {
                "use64",
                "movzx ecx, bx"
            };
            _equipMenuHook = _hooks.CreateAsmHook(function, address, AsmHookBehaviour.DoNotExecuteOriginal).Activate();
        });

        SigScan("48 8B 0D ?? ?? ?? ?? 4C 8D 67 ??", "PartyInfoPtr", address =>
        {
            _partyInfoPtr = (PartyInfo**)GetGlobalAddress(address + 3);
        });

        SigScan("48 8B 1D ?? ?? ?? ?? 45 33 C0 80 BB ?? ?? ?? ?? 01", "ItemsPtr", address =>
        {
            _itemsPtr = (byte**)GetGlobalAddress(address + 3);
        });

        SigScan("48 89 05 ?? ?? ?? ?? 44 8B 42 ?? 48 8B 52 ?? E8 ?? ?? ?? ?? 48 8B 1D ?? ?? ?? ?? 48 8B 0B 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B CB E8 ?? ?? ?? ?? 33 D2", "FacilityFtdsPtr", address =>
        {
            _facilityFtdsPtr = (FacilityFtds**)GetGlobalAddress(address + 3);
        });

        SigScan("48 89 5C 24 ?? 55 56 57 41 54 41 55 41 56 41 57 48 8D 6C 24 ?? 48 81 EC 90 00 00 00 4C 8B E1", "SetupEquipMenuInfo", address =>
        {
            _setupEquipMenuHook = _hooks.CreateHook<SetupEquipMenuInfoDelegate>(SetupEquipMenuInfo, address).Activate();
        });

    }

    private nuint SetupEquipMenuInfo(nuint param1, nuint param2, nuint param3)
    {
        // Setup default items for Rise
        GiveDefaultEquipment();
        GiveSetOutfits();

        return _setupEquipMenuHook.OriginalFunction(param1, param2, param3);
    }

    private string GetRiseOutfit(string currentStr)
    {
        if (_partyInfo->RiseCostume == Item.DefaultClothing) return currentStr;

        int bedIndex = currentStr.IndexOf("ASSIST_RISE");
        if (bedIndex == -1) return currentStr;

        int outfitId = ((int)_partyInfo->RiseCostume - 1792) / 8;
        var newBed = currentStr.Substring(0, bedIndex) + $"ASSIST_RISE_{outfitId:X}.BED";
        Log($"Switched outfit bed from {currentStr} to {newBed} (outfit {_partyInfo->RiseCostume})");
        return newBed;
    }

    /// <summary>
    /// Give Rise the default outfits that you get automatically like when Winter starts and stuff.
    /// Also gives the default Armor, Weapon, and Accessory
    /// </summary>
    private void GiveDefaultEquipment()
    {

        foreach (var item in _defaultItems)
        {
            if (_items[(int)item.Key] > 0 && _items[(int)item.Value] == 0 && _partyInfo->RiseCostume != item.Value)
            {
                Log($"Giving outfit {item.Value}");
                _items[(int)item.Value] = 1;
            }
        }

        if(Flags.CheckFlag(Flags.Flag.GoldenEnding) && _items[(int)Item.RiseEpilogue] == 0 && _partyInfo->RiseCostume != Item.RiseEpilogue)
        {
            Log($"Giving outfit {Item.RiseEpilogue}");
            _items[(int)Item.RiseEpilogue] = 1;
        }

        if (_partyInfo->RiseWeapon == Item.GolfClub)
            _partyInfo->RiseWeapon = Item.BareHand;

        if (_partyInfo->RiseArmor == Item.TShirt)
            _partyInfo->RiseArmor = Item.LaceCamisole;

        if (_partyInfo->RiseAccessory == Item.Wristwatch)
            _partyInfo->RiseAccessory = Item.Ribbon;
    }

    // Go through all of the outfit sets and give any items that Rise should have
    private void GiveSetOutfits()
    {
        var costumeSetFtd = GetCostumeSetFtd();

        for (int i = 0; i < costumeSetFtd->NumEntries; i++)
        {
            var set = &((CostumeSet*)&costumeSetFtd->Entries)[i];
            if (_items[(int)set->Item] > 0 && TryFindPartySetItem(set, PartyMember.Rise, out var riseOutfit) && _items[(int)riseOutfit] == 0 && _partyInfo->RiseCostume != riseOutfit)
            {
                for (int j = 0; j < 8; j++)
                {
                    var setItem = (&set->Items)[j];
                    if (_items[(int)setItem.Item] > 0)
                    {
                        // Someone has an item in the set so everyone must, give Rise the outfit
                        Log($"Giving outfit {riseOutfit}");
                        _items[(int)riseOutfit] = 1;
                        break;
                    }
                }
            }
        }

    }

    /// <summary>
    /// Tries to find an outft for the specified party member in a costume set
    /// </summary>
    /// <param name="set">The costume set to search</param>
    /// <param name="member">The party member to find the outfit for</param>
    /// <param name="item">The found outfit</param>
    /// <returns>True if the party member had an outfit in the set, false otherwise</returns>
    private bool TryFindPartySetItem(CostumeSet* set, PartyMember member, out Item item)
    {
        for (int i = 0; i < 8; i++)
        {
            var setItem = (&set->Items)[i];
            if (setItem.Member == member)
            {
                item = setItem.Item;
                return true;
            }
        }
        item = 0;
        return false;
    }

    private Ftd* GetCostumeSetFtd()
    {
        Ftd* ftd = &_facilityFtds->FtdFiles;

        for (int i = 0; i < _facilityFtds->NumFtds; i++)
        {
            if (new string((sbyte*)ftd->Name).Equals("fclCostumeSetTable.ftd"))
            {
                return ftd;
            }

            ftd = (Ftd*)((byte*)ftd + 0x24 + ftd->AlignedSize);
        }
        return null;
    }

    private delegate string GetRiseOutfitDelegate(string currentStr);
    private delegate nuint SetupEquipMenuInfoDelegate(nuint param1, nuint param2, nuint param3);

    [StructLayout(LayoutKind.Explicit)]
    private struct PartyInfo
    {
        [FieldOffset(0x25C)]
        internal Item RiseWeapon;

        [FieldOffset(0x25E)]
        internal Item RiseArmor;

        [FieldOffset(0x260)]
        internal Item RiseAccessory;

        [FieldOffset(0x262)]
        internal Item RiseCostume;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FacilityFtds
    {
        internal int NumFtds;
        internal Ftd FtdFiles; // This is an array
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct Ftd
    {
        [FieldOffset(0)]
        internal fixed byte Name[0x20];

        [FieldOffset(0x20)]
        internal int AlignedSize;

        [FieldOffset(0x24)]
        internal int Size;

        [FieldOffset(0x28)]
        internal int NumEntries;

        [FieldOffset(0x34)]
        internal byte Entries;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x22)]
    private struct CostumeSet
    {
        [FieldOffset(0)]
        internal Item Item;

        // Array of 8 items
        [FieldOffset(2)]
        internal SetItem Items;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SetItem
    {
        internal PartyMember Member;
        internal Item Item;
    }

    private enum Item : short
    {
        GolfClub = 1,
        TShirt = 257,
        LaceCamisole = 264,
        Wristwatch = 513,
        Ribbon = 520,
        DefaultClothing = 1792,
        YuWinterYaso = 1793,
        RiseWinterYaso = 1797,
        YuSummerYaso = 1801,
        RiseSummerYaso = 1805,
        YuSummerClothes = 1809,
        RiseSummerClothes = 1813,
        YuWinterClothes = 1817,
        RiseWinterClothes = 1824,
        YuBathTowel = 1881,
        RiseBathTowel = 1887,
        YuMidwinterOutfit = 1937,
        RiseMidwinterOutfit = 1941,
        YuMidwinterYaso = 1977,
        RiseMidwinterYaso = 1981,
        BareHand = 2559,
        RiseEpilogue = 1997,
    }

    private Dictionary<Item, Item> _defaultItems = new()
    {
        { Item.YuWinterYaso, Item.RiseWinterYaso },
        { Item.YuSummerYaso, Item.RiseSummerYaso },
        { Item.YuWinterClothes, Item.RiseWinterClothes },
        { Item.YuSummerClothes, Item.RiseSummerClothes },
        { Item.YuMidwinterYaso, Item.RiseMidwinterYaso },
        { Item.YuMidwinterOutfit, Item.RiseMidwinterOutfit },
        { Item.YuBathTowel, Item.RiseBathTowel }
    };

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