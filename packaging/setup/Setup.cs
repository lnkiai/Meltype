// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 lnkiai
//
// Meltype のインストーラー (1 つの exe)。中に配布用の zip (Build-Package.ps1 で作ったもの) を持っていて、
// 一時フォルダーに展開して install.ps1 -Ask (Install.cmd と同じ) を実行する。
// Windows に入っている .NET Framework 4 の C# コンパイラーでビルドする (Build-Setup.ps1)。

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;

internal static class Setup
{
    private static int Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.Title = "Meltype のインストール";
        var work = Path.Combine(Path.GetTempPath(), "meltype-setup-" + Guid.NewGuid().ToString("N"));
        var code = 1;
        try
        {
            Console.WriteLine("Meltype をインストールします。少し待ってください...");
            Directory.CreateDirectory(work);
            var zip = Path.Combine(work, "Meltype.zip");
            using (var payload = Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip"))
            using (var file = File.Create(zip))
            {
                if (payload == null) throw new InvalidOperationException("インストーラーの中身が見つかりません。");
                payload.CopyTo(file);
            }
            var folder = Path.Combine(work, "Meltype");
            ZipFile.ExtractToDirectory(zip, folder);
            var script = Path.Combine(folder, "install.ps1");
            if (!File.Exists(script)) throw new InvalidOperationException("install.ps1 が見つかりません。");

            var powershell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"WindowsPowerShell\v1.0\powershell.exe");
            var start = new ProcessStartInfo(powershell, "-NoProfile -ExecutionPolicy Bypass -File \"" + script + "\" -Ask")
            {
                UseShellExecute = false,
                WorkingDirectory = folder,
            };
            using (var process = Process.Start(start))
            {
                process.WaitForExit();
                code = process.ExitCode;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("インストールできませんでした: " + ex.Message);
        }
        finally
        {
            try { Directory.Delete(work, true); } catch { }
        }
        Console.WriteLine();
        Console.WriteLine("Enter キーを押すと閉じます。");
        Console.ReadLine();
        return code;
    }
}
