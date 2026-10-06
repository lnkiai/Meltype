// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 lnkiai

using System.Runtime.InteropServices;

namespace Meltype.Tests;

/// <summary>
/// 調査用: このスレッドの TSF の状態 (フォーカスのある入力欄、キーを受け取る IME、入力欄の「IME に渡さない」印) を文字にする。
/// e2e テストで、キーが IME に届かないときに原因を見るのに使う。
/// </summary>
internal static class TsfProbe
{
    private static readonly Guid ClsidThreadMgr = new("529A9E6B-6587-4F23-AB9E-9C7D683E3C50");
    private static readonly Guid KeyboardDisabled = new("71a5b253-1951-466b-9fbc-9c8808fa84f2");
    private static readonly Guid EmptyContext = new("d7487dbf-804e-41c5-894d-ad96fd4eea13");
    private static readonly Guid KeyboardOpenClose = new("58273aad-01bb-4164-95c6-755ba0b5162d");

    [ComImport, Guid("aa80e801-2021-11d2-93e0-0060b067b86e"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ITfThreadMgr
    {
        void Activate();
        void Deactivate();
        void CreateDocumentMgr();
        void EnumDocumentMgrs();
        [PreserveSig] int GetFocus([MarshalAs(UnmanagedType.Interface)] out ITfDocumentMgr? documentMgr);
    }

    [ComImport, Guid("aa80e7f4-2021-11d2-93e0-0060b067b86e"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ITfDocumentMgr
    {
        void CreateContext();
        void Push();
        void Pop();
        [PreserveSig] int GetTop([MarshalAs(UnmanagedType.Interface)] out ITfContext? context);
        [PreserveSig] int GetBase([MarshalAs(UnmanagedType.Interface)] out ITfContext? context);
    }

    [ComImport, Guid("aa80e7fd-2021-11d2-93e0-0060b067b86e"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ITfContext
    {
    }

    [ComImport, Guid("7dcf57ac-18ad-438b-824d-979bffb74b7c"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ITfCompartmentMgr
    {
        [PreserveSig] int GetCompartment(ref Guid guid, [MarshalAs(UnmanagedType.Interface)] out ITfCompartment? compartment);
    }

    [ComImport, Guid("bb08f7a9-607a-4384-8623-056892b64371"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ITfCompartment
    {
        void SetValue();
        [PreserveSig] int GetValue(out object? value);
    }

    public static string Describe()
    {
        try
        {
            var threadMgr = (ITfThreadMgr)Activator.CreateInstance(Type.GetTypeFromCLSID(ClsidThreadMgr)!)!;
            var parts = new List<string> { $"スレッドの入力モード: {Read((ITfCompartmentMgr)threadMgr, KeyboardOpenClose)}", $"キーボードの配列: {GetKeyboardLayout(0):X8}", ActiveProfile() };
            if (threadMgr.GetFocus(out var documentMgr) != 0 || documentMgr is null) return string.Join(" / ", parts.Append("TSF のフォーカス: 無し"));
            foreach (var (name, top) in new[] { ("一番上の入力欄", true), ("元の入力欄", false) })
            {
                var hr = top ? documentMgr.GetTop(out var context) : documentMgr.GetBase(out context);
                if (hr != 0 || context is null)
                {
                    parts.Add($"{name}: 無し");
                    continue;
                }
                var compartments = (ITfCompartmentMgr)context;
                parts.Add($"{name}: IME に渡さない={Read(compartments, KeyboardDisabled)} 空の入力欄={Read(compartments, EmptyContext)}");
            }
            return string.Join(" / ", parts);
        }
        catch (Exception ex)
        {
            return $"TSF の状態を読めません: {ex.Message}";
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetKeyboardLayout(uint thread);

    [StructLayout(LayoutKind.Sequential)]
    private struct TF_INPUTPROCESSORPROFILE
    {
        public uint dwProfileType;
        public ushort langid;
        public Guid clsid;
        public Guid guidProfile;
        public Guid catid;
        public IntPtr hklSubstitute;
        public uint dwCaps;
        public IntPtr hkl;
        public uint dwFlags;
    }

    [ComImport, Guid("71c6e74c-0f28-11d8-a82a-00065b84435c"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ITfInputProcessorProfileMgr
    {
        void ActivateProfile();
        void DeactivateProfile();
        void GetProfile();
        void EnumProfiles();
        void ReleaseInputProcessor();
        void RegisterProfile();
        void UnregisterProfile();
        [PreserveSig] int GetActiveProfile(ref Guid catid, out TF_INPUTPROCESSORPROFILE profile);
    }

    private static string ActiveProfile()
    {
        var manager = (ITfInputProcessorProfileMgr)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("33C53A50-F456-4884-B049-85FD643ECFED"))!)!;
        var keyboard = new Guid("34745c63-b2f0-4784-8b67-5e12c8701a31");
        var hr = manager.GetActiveProfile(ref keyboard, out var profile);
        return hr == 0 ? $"有効な IME: {profile.clsid} (種類 {profile.dwProfileType}, 言語 {profile.langid:X4}, 配列 {profile.hkl:X8}, flags {profile.dwFlags:X})" : $"有効な IME: 0x{hr:X8}";
    }

    private static string Read(ITfCompartmentMgr compartments, Guid guid)
    {
        var hr = compartments.GetCompartment(ref guid, out var compartment);
        if (hr != 0 || compartment is null) return $"(取れない 0x{hr:X8})";
        hr = compartment.GetValue(out var value);
        // S_FALSE (1) は「値が設定されていない」
        return hr switch { 0 => value?.ToString() ?? "空", 1 => "未設定", _ => $"(読めない 0x{hr:X8})" };
    }
}
