# SPDX-License-Identifier: GPL-3.0-or-later
# Copyright (C) 2026 lnkiai
#
# 配布用の zip (Build-Package.ps1 で作ったもの) から、ダブルクリックで入れられるインストーラー (1 つの exe) を作る。
#   powershell -ExecutionPolicy Bypass -File packaging\setup\Build-Setup.ps1 [-Zip <zip>] [-Out <exe>]
# Windows に入っている .NET Framework 4 の C# コンパイラー (csc.exe) でビルドする (追加で入れるものは無い)。

param(
    [string]$Zip,
    [string]$Out
)

$ErrorActionPreference = 'Stop'
$repo = Resolve-Path (Join-Path $PSScriptRoot '..\..')
if (-not $Zip) {
    $latest = Get-ChildItem (Join-Path $repo 'dist') -Filter 'Meltype-*.zip' -ErrorAction SilentlyContinue | Sort-Object LastWriteTime | Select-Object -Last 1
    if (-not $latest) { throw 'dist に zip がありません。先に Build-Package.ps1 を実行してください。' }
    $Zip = $latest.FullName
}
if (-not $Out) {
    $version = ([xml](Get-Content -LiteralPath (Join-Path $repo 'src\Meltype\Meltype.csproj') -Raw)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
    $Out = Join-Path $repo "dist\Meltype-IME-$version-setup.exe"
}
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $csc)) { $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (-not (Test-Path -LiteralPath $csc)) { throw 'C# コンパイラー (csc.exe) が見つかりません。' }
$icon = Join-Path $repo 'native\tip\meltype.ico'
$source = Join-Path $PSScriptRoot 'Setup.cs'
& $csc /nologo /target:exe /optimize+ "/out:$Out" "/win32icon:$icon" "/resource:$Zip,payload.zip" `
    /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll $source
if ($LASTEXITCODE -ne 0) { throw 'インストーラーをビルドできませんでした。' }
Write-Host "インストーラーを作りました: $Out"
