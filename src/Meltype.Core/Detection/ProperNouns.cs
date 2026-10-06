// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Yukishiro

namespace Meltype.Detection;

/// <summary>
/// 英語の固有名詞 (dictionaries/propernouns.txt)。小文字で引き、正しい大文字小文字の形 (GitHub, iPhone) を返す。
/// ローマ字としても読めてしまう固有名詞 (amazon, adobe) を英語として扱うのに使う。
/// </summary>
public sealed class ProperNouns
{
    private readonly Dictionary<string, string> _canonical = new(StringComparer.Ordinal);
    private readonly WordList _words = new();

    public static ProperNouns Load(string? userDirectory)
    {
        var nouns = new ProperNouns();
        // ユーザーの辞書を先に読む (同じ語なら、ユーザーが登録した大文字小文字の形を使う: Github と登録すれば Github)。
        // 英語のユーザー辞書 (Composition.UserEnglishWords) の語も、登録した形で出すので固有名詞として読む
        if (userDirectory is not null)
        {
            foreach (var name in new[] { Composition.UserEnglishWords.FileName, "propernouns.txt" })
            {
                var path = Path.Combine(userDirectory, name);
                try
                {
                    if (File.Exists(path)) nouns.AddText(File.ReadAllText(path));
                }
                catch (Exception ex)
                {
                    Diagnostics.Log.Warn($"ユーザーの固有名詞辞書を読めませんでした: {ex.Message}");
                }
            }
        }
        nouns.AddText(DictionarySource.ReadEmbedded("propernouns.txt"));
        return nouns;
    }

    public void AddText(string text)
    {
        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine;
            var hash = line.IndexOf('#');
            if (hash >= 0) line = line[..hash];
            foreach (var word in line.Split([' ', '\t', '\r', ','], StringSplitOptions.RemoveEmptyEntries))
            {
                if (!word.All(char.IsAsciiLetterOrDigit)) continue;
                var lower = word.ToLowerInvariant();
                _canonical.TryAdd(lower, word);
                _words.Add(lower);
            }
        }
    }

    /// <summary>小文字の綴り (英語辞書に足す用)。</summary>
    public IEnumerable<string> LowercaseWords => _canonical.Keys;

    public bool Contains(string lower) => _canonical.ContainsKey(lower);

    public bool HasPrefix(string lower) => _words.HasPrefix(lower);

    /// <summary>正しい大文字小文字の形。固有名詞でなければ null。</summary>
    public string? Canonical(string lower) => _canonical.TryGetValue(lower, out var word) ? word : null;
}
