// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Yukishiro
// Modified by lnkiai (2026): Meltype IME (TSF) を足すための変更

using Meltype.Composition;
using Meltype.Input;

namespace Meltype.UI;

/// <summary>
/// ユーザー辞書の登録・削除。
/// 「日本語」: 読みと単語。読みはローマ字で打ってもよい (kigoutou → きごうとう)。読みを打つと変換候補がプルダウンに出るので、
/// そこから選ぶか、単語欄に直接打って登録する (変換ボックスの方式なら、この画面でも Meltype キーボードが使える。Meltype IME は Meltype 自身の画面では何もしないので、読みはローマ字で打つ)。
/// 「英語」: 打ったとおりの英字で入力する語 (自分の ID・社名など。deno が「での」にならないように)。
/// </summary>
internal sealed class UserDictionaryForm : Form
{
    private readonly CompositionService _service;
    private readonly TextBox _reading = new() { Width = 220 };
    private readonly Label _readingPreview = new() { AutoSize = true, ForeColor = SystemColors.GrayText, Padding = new Padding(0, 6, 0, 0) };
    private readonly ComboBox _word = new() { Width = 220, DropDownStyle = ComboBoxStyle.DropDown };
    private readonly DataGridView _grid = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        RowHeadersVisible = false,
        BackgroundColor = SystemColors.Window,
    };
    private readonly Label _message = new() { AutoSize = true, ForeColor = Color.Firebrick, Padding = new Padding(0, 6, 0, 0) };
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };
    private readonly TabPage _japaneseTab = new("日本語 (読み → 単語)");
    private readonly TabPage _englishTab = new("英語 (そのまま英字)");
    private readonly TextBox _english = new() { Width = 220 };
    private readonly Label _englishMessage = new() { AutoSize = true, ForeColor = Color.Firebrick, Padding = new Padding(0, 6, 0, 0) };
    private readonly Button _addEnglish = new() { Text = "登録", AutoSize = true };
    private readonly ListBox _englishList = new() { Dock = DockStyle.Fill, IntegralHeight = false, SelectionMode = SelectionMode.MultiExtended };
    private readonly System.Windows.Forms.Timer _suggestTimer = new() { Interval = 300 };

    public UserDictionaryForm(CompositionService service)
    {
        _service = service;
        Text = "Meltype ユーザー辞書";
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(560, 520);
        MinimumSize = new Size(460, 360);
        Font = new Font("Yu Gothic UI", 9.5F);

        var add = new Button { Text = "登録", AutoSize = true };
        add.Click += (_, _) => Register();
        var entry = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 3, Padding = new Padding(8) };
        entry.Controls.Add(new Label { Text = "読み (ローマ字・ひらがな)", AutoSize = true, Padding = new Padding(0, 6, 8, 0) }, 0, 0);
        entry.Controls.Add(_reading, 1, 0);
        entry.Controls.Add(_readingPreview, 2, 0);
        entry.Controls.Add(new Label { Text = "単語 (候補から選ぶか直接入力)", AutoSize = true, Padding = new Padding(0, 6, 8, 0) }, 0, 1);
        entry.Controls.Add(_word, 1, 1);
        entry.Controls.Add(add, 2, 1);
        entry.Controls.Add(_message, 1, 2);
        entry.SetColumnSpan(_message, 2);

        var remove = new Button { Text = "選んだ語を削除", AutoSize = true };
        remove.Click += (_, _) => RemoveSelected();
        var close = new Button { Text = "閉じる", AutoSize = true, DialogResult = DialogResult.Cancel };
        close.Click += (_, _) => Close();
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(6) };
        // ほかの日本語入力 (Microsoft IME・Google 日本語入力) の辞書の取り込みと、Microsoft IME の形式での書き出し
        var import = new Button { Text = "取り込む...", AutoSize = true };
        import.Click += (_, _) => Import();
        var export = new Button { Text = "書き出す...", AutoSize = true };
        export.Click += (_, _) => Export();
        buttons.Controls.AddRange([close, remove, export, import]);

        _grid.Columns.Add("reading", "読み");
        _grid.Columns.Add("word", "単語");
        var gridPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 8, 0) };
        gridPanel.Controls.Add(_grid);
        _japaneseTab.Controls.Add(gridPanel);
        _japaneseTab.Controls.Add(entry);

        // 英語: 打ったとおりの英字で入力する語
        _addEnglish.Click += (_, _) => RegisterEnglish();
        var englishEntry = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 3, Padding = new Padding(8) };
        englishEntry.Controls.Add(new Label { Text = "英語の語 (英字)", AutoSize = true, Padding = new Padding(0, 6, 8, 0) }, 0, 0);
        englishEntry.Controls.Add(_english, 1, 0);
        englishEntry.Controls.Add(_addEnglish, 2, 0);
        englishEntry.Controls.Add(_englishMessage, 1, 1);
        englishEntry.SetColumnSpan(_englishMessage, 2);
        var englishHelp = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            MaximumSize = new Size(520, 0),
            ForeColor = SystemColors.GrayText,
            Padding = new Padding(10, 0, 10, 6),
            Text = "自分の ID・社名・製品名など、ローマ字として読めてもかなにしたくない語を登録します (deno → 「での」にならない)。" +
                   "大文字小文字は登録したとおりの形で候補に出ます (Hono)。",
        };
        var englishPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 8, 0) };
        englishPanel.Controls.Add(_englishList);
        _englishTab.Controls.Add(englishPanel);
        _englishTab.Controls.Add(englishHelp);
        _englishTab.Controls.Add(englishEntry);
        _englishList.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Delete) RemoveSelected();
        };

        _tabs.TabPages.AddRange([_japaneseTab, _englishTab]);
        _tabs.SelectedIndexChanged += (_, _) =>
        {
            // 取り込み・書き出しは日本語の辞書だけ (Microsoft IME などの形式)
            var japanese = _tabs.SelectedTab == _japaneseTab;
            import.Visible = japanese;
            export.Visible = japanese;
            AcceptButton = japanese ? add : _addEnglish;
            (japanese ? (Control)_reading : _english).Focus();
        };

        Controls.Add(_tabs);
        Controls.Add(buttons);
        AcceptButton = add;
        CancelButton = close;

        _reading.TextChanged += (_, _) =>
        {
            _readingPreview.Text = _reading.Text.Any(char.IsAsciiLetter) ? "→ " + _service.ToReading(_reading.Text) : "";
            _suggestTimer.Stop();
            _suggestTimer.Start();
        };
        _suggestTimer.Tick += (_, _) =>
        {
            _suggestTimer.Stop();
            Suggest();
        };
        _grid.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Delete) RemoveSelected();
        };
        Reload();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ForegroundTracker.TypingAllowedWindows[Handle] = true;
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        ForegroundTracker.TypingAllowedWindows.TryRemove(Handle, out _);
        base.OnHandleDestroyed(e);
    }

    private void Suggest()
    {
        var reading = _service.ToReading(_reading.Text);
        var typed = _word.Text;
        _word.Items.Clear();
        if (reading.Length == 0) return;
        foreach (var word in _service.SuggestWords(reading)) _word.Items.Add(word);
        if (typed.Length == 0 && _word.Items.Count > 0) _word.SelectedIndex = 0;
    }

    private void Register()
    {
        var reading = _service.ToReading(_reading.Text);
        var error = _service.UserDictionary.Add(reading, _word.Text);
        if (error is not null)
        {
            _message.ForeColor = Color.Firebrick;
            _message.Text = error;
            return;
        }
        _message.Text = "";
        Diagnostics.Log.Info($"ユーザー辞書に登録しました: {Diagnostics.Log.Text(reading)} → {Diagnostics.Log.Text(_word.Text.Trim())}");
        _reading.Clear();
        _word.Text = "";
        _word.Items.Clear();
        Reload();
        _reading.Focus();
    }

    private void RegisterEnglish()
    {
        var word = _english.Text.Trim();
        var error = _service.UserEnglish.Add(word);
        if (error is not null)
        {
            _englishMessage.ForeColor = Color.Firebrick;
            _englishMessage.Text = error;
            return;
        }
        _service.ReloadUserWords();
        _englishMessage.Text = "";
        Diagnostics.Log.Info($"英語のユーザー辞書に登録しました: {Diagnostics.Log.Text(word)}");
        _english.Clear();
        Reload();
        _english.Focus();
    }

    /// <summary>選んでいた語を入れて開く (Ctrl+F7)。読みは推測したもの (直して登録できる)。英字だけの語なら「英語」で開く。</summary>
    public void Prefill(string word, string reading)
    {
        if (word.Length >= UserEnglishWords.MinLength && word.All(char.IsAsciiLetter))
        {
            // 英字だけの語は「英語」のタブで開く。読みを付けて登録したいとき (えーぴーあい → API) のために、日本語のタブにも入れておく
            _word.Text = word;
            _reading.Text = reading;
            _message.Text = "";
            _tabs.SelectedTab = _englishTab;
            _english.Text = word;
            _englishMessage.Text = "「登録」を押すと、この語は打ったとおりの英字で入力されます。読みを付けて登録するなら「日本語」のタブへ。";
            _englishMessage.ForeColor = SystemColors.GrayText;
            Activate();
            _english.Focus();
            return;
        }
        _tabs.SelectedTab = _japaneseTab;
        _word.Text = word;
        _reading.Text = reading;
        _message.Text = reading.Length == 0 ? "読みを入力してください。" : "読みを確かめて「登録」を押してください。";
        _message.ForeColor = reading.Length == 0 ? Color.Firebrick : SystemColors.GrayText;
        Activate();
        _reading.Focus();
        _reading.SelectAll();
    }

    private void Import()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "ユーザー辞書を取り込む",
            Filter = "辞書のテキストファイル (*.txt)|*.txt|すべてのファイル (*.*)|*.*",
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            var result = UserDictionaryFile.Parse(File.ReadAllBytes(dialog.FileName));
            var added = _service.UserDictionary.AddRange(result.Words);
            Reload();
            Diagnostics.Log.Info($"ユーザー辞書を取り込みました: {added} 語 ({result.Encoding})");
            var skipped = result.Skipped > 0 ? $"\n読みがかなでない・短すぎるなどで飛ばした行: {result.Skipped}" : "";
            var duplicates = result.Words.Count - added;
            MessageBox.Show(this, $"{added} 語を登録しました。{(duplicates > 0 ? $"\n登録済みの語: {duplicates}" : "")}{skipped}", "ユーザー辞書の取り込み",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"取り込めませんでした。\n\n{ex.Message}", "ユーザー辞書の取り込み", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void Export()
    {
        using var dialog = new SaveFileDialog
        {
            Title = "ユーザー辞書を書き出す",
            Filter = "辞書のテキストファイル (*.txt)|*.txt",
            FileName = $"Meltype-ユーザー辞書-{DateTime.Now:yyyyMMdd}.txt",
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            File.WriteAllBytes(dialog.FileName, UserDictionaryFile.Export(_service.UserDictionary.Words));
            MessageBox.Show(this, $"{_service.UserDictionary.Count} 語を書き出しました。\nMicrosoft IME・Google 日本語入力・ATOK の辞書ツールで取り込めます。", "ユーザー辞書の書き出し",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"書き出せませんでした。\n\n{ex.Message}", "ユーザー辞書の書き出し", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void RemoveSelected()
    {
        if (_tabs.SelectedTab == _englishTab)
        {
            var words = _englishList.SelectedItems.Cast<string>().ToList();
            if (words.Count == 0) return;
            foreach (var word in words) _service.UserEnglish.Remove(word);
            _service.ReloadUserWords();
            Reload();
            return;
        }
        foreach (DataGridViewRow row in _grid.SelectedRows)
        {
            if (row.Tag is UserWord word) _service.UserDictionary.Remove(word);
        }
        Reload();
    }

    private void Reload()
    {
        _grid.Rows.Clear();
        foreach (var word in _service.UserDictionary.Words.Reverse())
        {
            var index = _grid.Rows.Add(word.Reading, word.Word);
            _grid.Rows[index].Tag = word;
        }
        _englishList.Items.Clear();
        // 手で書き換えたファイルも、一覧に出して判定にも効かせる
        _service.UserEnglish.Refresh();
        _service.ReloadUserWords();
        foreach (var word in _service.UserEnglish.Words.Reverse()) _englishList.Items.Add(word);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _suggestTimer.Dispose();
        base.Dispose(disposing);
    }
}
