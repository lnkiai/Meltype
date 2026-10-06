// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 lnkiai

using Meltype.Composition;
using Meltype.Config;
using Meltype.Input;

namespace Meltype.Tests;

/// <summary>英語のユーザー辞書 (自分の ID などを、打ったとおりの英字で入力する)。</summary>
internal static class UserEnglishWordsTests
{
    private static string TempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "meltype-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static string Composed(CompositionDetector detector, string text)
    {
        var session = new MeltypeSession(detector, new CompositionTests.FakeConverter(), new CompositionOptions(), () => new Settings());
        SessionResult? last = null;
        foreach (var c in text) last = session.HandleKey(char.ToUpperInvariant(c), c, false, false, false, false);
        return last?.View?.Text ?? "";
    }

    [Test]
    public static void RegisteredWord_StaysEnglish_AfterReload()
    {
        var directory = TempDirectory();
        try
        {
            var detector = CompositionDetector.CreateDefault(directory);
            Assert.True(!Composed(detector, "watashihadenodesu").Contains("deno"), "登録する前は、ローマ字としてかなになる");

            var words = UserEnglishWords.Load(directory);
            Assert.Equal(null, words.Add("deno"));
            detector.ReloadUserWords();
            Assert.Equal("deno", Composed(detector, "deno"));
            Assert.Equal("わたしはdenoです", Composed(detector, "watashihadenodesu"));

            words.Remove("DENO");
            detector.ReloadUserWords();
            Assert.True(!Composed(detector, "watashihadenodesu").Contains("deno"), "消したら元に戻る");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Test]
    public static void File_KeepsHandWrittenLinesAndCase()
    {
        var directory = TempDirectory();
        try
        {
            var path = Path.Combine(directory, UserEnglishWords.FileName);
            File.WriteAllLines(path, ["# 手で書いたコメント", "GitHub iPhone # 行末のコメント"]);
            var words = UserEnglishWords.Load(directory);
            Assert.Equal(null, words.Add("Hono"));
            Assert.Equal(null, words.Add("hono"));  // 同じ語は形だけ新しい方に
            words.Remove("iphone");
            var lines = File.ReadAllLines(path);
            Assert.Equal("# 手で書いたコメント", lines[0]);
            Assert.Equal("GitHub # 行末のコメント", lines[1]);
            Assert.Equal("hono", lines[^1]);
            Assert.Equal(2, UserEnglishWords.Load(directory).Words.Count);
            Assert.True(words.Add("hono js") is not null, "空白は使えない");
            Assert.True(words.Add("a") is not null, "1 文字は登録しない");
            Assert.True(words.Add("user123") is not null, "数字の入った語は判定に効かないので登録しない");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Test]
    public static void HandEdits_WhileRunning_AreKept()
    {
        var directory = TempDirectory();
        try
        {
            var words = UserEnglishWords.Load(directory);
            Assert.Equal(null, words.Add("Hono"));
            // Meltype が動いている間に、手でファイルに書き足した
            File.AppendAllLines(Path.Combine(directory, UserEnglishWords.FileName), ["Bun"]);
            Assert.Equal(null, words.Add("deno"));
            var saved = UserEnglishWords.Load(directory).Words;
            Assert.True(saved.Contains("Bun") && saved.Contains("Hono") && saved.Contains("deno"), "手で書き足した語も残る");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Test]
    public static void HandWrittenProperNouns_AreNotForcedEnglish()
    {
        // 前から手で書いていた固有名詞の辞書は、大文字小文字を直すだけ (英語に決める語にはしない)
        var directory = TempDirectory();
        try
        {
            var path = Path.Combine(directory, "propernouns.txt");
            File.WriteAllLines(path, ["Deno"]);
            var detector = CompositionDetector.CreateDefault(directory);
            Assert.True(!detector.IsUserEnglish("deno"), "英語に決める語にはしない");
            Assert.Equal("Deno", detector.ProperNouns.Canonical("deno"));
            Assert.Equal(0, UserEnglishWords.Load(directory).Words.Count);
            Assert.True(File.Exists(path), "手で書いたファイルはそのまま");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Test]
    public static void RegisteredCase_WinsOverBuiltIn()
    {
        var directory = TempDirectory();
        try
        {
            var detector = CompositionDetector.CreateDefault(directory);
            Assert.Equal("GitHub", detector.ProperNouns.Canonical("github"));
            UserEnglishWords.Load(directory).Add("Github");
            detector.ReloadUserWords();
            Assert.Equal("Github", detector.ProperNouns.Canonical("github"));
            Assert.True(detector.IsUserEnglish("github"), "登録した語として扱う");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
