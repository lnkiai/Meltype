// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 lnkiai

using System.Text;

namespace Meltype.Composition;

/// <summary>
/// ユーザーが登録した英語の語 (自分の ID・社名・製品名など)。打ったとおりの英字で入力し、ローマ字として読めてもかなにしない
/// (deno → deno。登録しないと「での」になる)。大文字小文字は登録したとおりの形を候補に出す (hono → Hono)。
/// 保存先は %LOCALAPPDATA%\Meltype\dictionaries\userenglish.txt (手で書いた行・コメントはそのまま残す)。
/// 固有名詞の辞書 (propernouns.txt。大文字小文字を直すだけで、英語に決めはしない) とは別のファイルにする。
/// トレイの「ユーザー辞書...」の「英語」から登録・削除する。
/// </summary>
public sealed class UserEnglishWords
{
    public const int MinLength = 2;
    public const int MaxLength = 40;

    private readonly string? _path;
    private readonly List<string> _lines = [];

    public UserEnglishWords(string? path)
    {
        _path = path;
        Refresh();
    }

    /// <summary>
    /// ファイルを読み直す。登録・削除の前にも読み直すので、Meltype が動いている間に手で書き換えた内容も消さない。
    /// </summary>
    public void Refresh()
    {
        if (_path is null) return;
        try
        {
            var lines = File.Exists(_path) ? File.ReadAllLines(_path, Encoding.UTF8) : [];
            _lines.Clear();
            _lines.AddRange(lines);
        }
        catch (Exception ex)
        {
            Diagnostics.Log.Warn($"英語のユーザー辞書を読めませんでした: {ex.Message}");
        }
    }

    public const string FileName = "userenglish.txt";

    private const string Header = "# Meltype の英語のユーザー辞書 (1 行に 1 語。打ったとおりの英字で入力する語。トレイの「ユーザー辞書...」の「英語」で編集できます)";

    /// <summary>既定の場所 (dictionaries\userenglish.txt) のもの。</summary>
    public static UserEnglishWords Load(string? userDictionaryDirectory) =>
        new(userDictionaryDirectory is null ? null : Path.Combine(userDictionaryDirectory, FileName));

    /// <summary>登録した語 (登録した形のまま。ファイルの順)。</summary>
    public IReadOnlyList<string> Words => _lines.SelectMany(Tokens).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    /// <summary>小文字にした語の集合 (判定に使う)。</summary>
    public IReadOnlySet<string> LowercaseWords => Words.Select(w => w.ToLowerInvariant()).ToHashSet(StringComparer.Ordinal);

    /// <summary>登録する。登録できなければ理由を返す。同じ語 (大文字小文字は問わない) があれば、形だけ新しい方にする。</summary>
    public string? Add(string word)
    {
        word = word.Trim();
        if (word.Length < MinLength) return $"{MinLength} 文字以上の英字にしてください。";
        if (word.Length > MaxLength) return $"{MaxLength} 文字までにしてください。";
        // 判定は英字だけの語を見るので、数字・空白・記号の入った語は登録しても効かない
        if (!word.All(char.IsAsciiLetter)) return "英字だけで入力してください (数字・空白・記号は使えません)。";
        Refresh();
        RemoveToken(word);
        _lines.Add(word);
        Save();
        return null;
    }

    /// <summary>削除する (大文字小文字は問わない)。</summary>
    public void Remove(string word)
    {
        Refresh();
        if (!RemoveToken(word)) return;
        Save();
    }

    private bool RemoveToken(string word)
    {
        var removed = false;
        for (var i = _lines.Count - 1; i >= 0; i--)
        {
            var (body, comment) = Split(_lines[i]);
            var tokens = SplitTokens(body);
            var kept = tokens.Where(t => !t.Equals(word, StringComparison.OrdinalIgnoreCase)).ToList();
            if (kept.Count == tokens.Count) continue;
            removed = true;
            if (kept.Count == 0 && comment.Length == 0) _lines.RemoveAt(i);
            else _lines[i] = string.Join(' ', kept) + (comment.Length > 0 ? (kept.Count > 0 ? " " : "") + comment : "");
        }
        return removed;
    }

    private void Save()
    {
        if (_path is null) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            if (!File.Exists(_path) && !_lines.Any(l => l.StartsWith('#')))
            {
                _lines.Insert(0, Header);
            }
            File.WriteAllLines(_path, _lines, new UTF8Encoding(false));
        }
        catch (Exception ex)
        {
            Diagnostics.Log.Warn($"英語のユーザー辞書を保存できませんでした: {ex.Message}");
        }
    }

    private static (string Body, string Comment) Split(string line)
    {
        var hash = line.IndexOf('#');
        return hash < 0 ? (line, "") : (line[..hash], line[hash..]);
    }

    private static List<string> SplitTokens(string body) => body.Split([' ', '\t', '\r', ','], StringSplitOptions.RemoveEmptyEntries).ToList();

    /// <summary>ProperNouns と同じ読み方 (英数字だけの語)。</summary>
    private static IEnumerable<string> Tokens(string line) => SplitTokens(Split(line).Body).Where(t => t.All(char.IsAsciiLetterOrDigit));
}
