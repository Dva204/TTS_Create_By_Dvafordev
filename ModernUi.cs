using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace TTS_Create_By_DvaForDev;

internal sealed class ModernTabControl : TabControl
{
    private const int TcmAdjustRect = 0x1328;
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == TcmAdjustRect && !DesignMode)
        {
            m.Result = (IntPtr)1;
            return;
        }
        base.WndProc(ref m);
    }
}

public partial class Form1
{
    private readonly Color uiPrimary = Color.FromArgb(27, 118, 91);
    private readonly Color uiBackground = Color.FromArgb(245, 247, 250);
    private readonly Color uiText = Color.FromArgb(37, 49, 65);
    private readonly Dictionary<TabPage, Button> navigationButtons = new();
    private Label? pageHeading;
    private Label? pageSubtitle;

    private void InitializeModernUi()
    {
        SuspendLayout();
        var pages = mainTabControl.TabPages.Cast<TabPage>().ToList();
        var active = mainTabControl.SelectedIndex;
        var tabHost = new ModernTabControl
        {
            Dock = DockStyle.Fill, Alignment = TabAlignment.Top,
            Appearance = TabAppearance.FlatButtons,
            SizeMode = TabSizeMode.Fixed, ItemSize = new Size(0, 1),
            Multiline = true, Margin = Padding.Empty,
            Padding = new Point(0, 0)
        };
        foreach (var page in pages)
        {
            mainTabControl.TabPages.Remove(page);
            tabHost.TabPages.Add(page);
        }
        Controls.Remove(mainTabControl);
        mainTabControl.Dispose();
        mainTabControl = tabHost;
        mainTabControl.SelectedIndex = Math.Clamp(active, 0, pages.Count - 1);

        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2,
            BackColor = uiBackground, Margin = Padding.Empty, Padding = Padding.Empty
        };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 218));
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var nav = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12, 9, 12, 12) };
        var brand = new Label
        {
            Text = "DvaForDev", Font = new Font("Segoe UI Semibold", 17, FontStyle.Bold),
            ForeColor = uiPrimary, Dock = DockStyle.Top, Height = 52,
            TextAlign = ContentAlignment.MiddleLeft
        };
        var menu = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false,
            AutoScroll = true, Padding = new Padding(0, 16, 0, 0)
        };
        nav.Controls.Add(menu);
        nav.Controls.Add(brand);
        var names = new[] { "TTS / Hàng loạt", "Thư viện giọng", "Biên tập video", "Cài đặt", "Whiteboard AI", "AI Video Studio" };
        var symbols = new[] { "◉", "♫", "▣", "⚙", "✎", "▶" };
        for (int i = 0; i < pages.Count; i++)
        {
            var page = pages[i];
            int index = i;
            var button = new Button
            {
                Text = (i < symbols.Length ? symbols[i] : "•") + "   " +
                       (i < names.Length ? names[i] : page.Text),
                Width = 186, Height = 48, Margin = new Padding(2, 3, 2, 3),
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                Padding = new Padding(12, 0, 0, 0)
            };
            button.FlatAppearance.BorderSize = 0;
            button.Click += (_, _) => mainTabControl.SelectedIndex = index;
            navigationButtons[page] = button;
            menu.Controls.Add(button);
        }
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, BackColor = Color.White, ColumnCount = 1, RowCount = 2,
            Padding = new Padding(23, 10, 20, 8)
        };
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        pageHeading = new Label
        {
            Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 16, FontStyle.Bold),
            ForeColor = uiText, TextAlign = ContentAlignment.MiddleLeft
        };
        pageSubtitle = new Label
        {
            Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(115, 125, 139),
            Text = "Không gian làm việc • Các công cụ hiện tại được giữ nguyên"
        };
        header.Controls.Add(pageHeading, 0, 0);
        header.Controls.Add(pageSubtitle, 0, 1);
        shell.Controls.Add(nav, 0, 0);
        shell.SetRowSpan(nav, 2);
        shell.Controls.Add(header, 1, 0);
        var workspace = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 10, 10, 10), BackColor = uiBackground };
        mainTabControl.Dock = DockStyle.Fill;
        workspace.Controls.Add(mainTabControl);
        shell.Controls.Add(workspace, 1, 1);
        Controls.Add(shell);
        ApplyModernControlStyles(mainTabControl);
        ApplyLegacyWorkspaceStyles();
        FixTtsBatchLayout();
        mainTabControl.SelectedIndexChanged += (_, _) => RefreshModernNavigation();
        RefreshModernNavigation();
        MinimumSize = new Size(1030, 680);
        ResumeLayout(true);
    }

    private void ApplyModernControlStyles(Control root)
    {
        foreach (Control child in root.Controls)
        {
            if (child is Button button && button.FlatStyle != FlatStyle.Flat)
            {
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderColor = Color.FromArgb(215, 223, 230);
                button.FlatAppearance.BorderSize = 1;
                if (button.BackColor == SystemColors.Control ||
                    button.BackColor == Color.Transparent)
                    button.BackColor = Color.White;
                button.ForeColor = uiText;
                button.Cursor = Cursors.Hand;
            }
            else if (child is DataGridView table)
            {
                table.BackgroundColor = Color.White;
                table.BorderStyle = BorderStyle.None;
                table.EnableHeadersVisualStyles = false;
                table.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(236, 242, 246);
                table.ColumnHeadersDefaultCellStyle.ForeColor = uiText;
                table.ColumnHeadersDefaultCellStyle.Font =
                    new Font("Segoe UI Semibold", 9, FontStyle.Bold);
                table.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 241, 234);
                table.DefaultCellStyle.SelectionForeColor = uiText;
                table.RowTemplate.Height = 29;
            }
            else if (child is TextBox input && !input.Multiline)
            {
                input.BorderStyle = BorderStyle.FixedSingle;
                input.BackColor = Color.White;
            }
            if (child.HasChildren) ApplyModernControlStyles(child);
        }
    }

    // Apply a consistent visual language without moving legacy controls or
    // disturbing the event bindings of the existing production workflows.
    private void ApplyLegacyWorkspaceStyles()
    {
        var tabs = mainTabControl.TabPages;
        for (int i = 0; i < Math.Min(4, tabs.Count); i++)
        {
            var page = tabs[i];
            page.BackColor = uiBackground;
            StyleLegacyChildren(page);
        }
    }

    private void StyleLegacyChildren(Control root)
    {
        foreach (Control child in root.Controls)
        {
            switch (child)
            {
                case GroupBox group:
                    group.ForeColor = uiText;
                    group.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
                    group.Padding = new Padding(9, 12, 9, 8);
                    break;
                case DataGridView table:
                    table.BackgroundColor = Color.White;
                    table.GridColor = Color.FromArgb(228, 233, 239);
                    table.BorderStyle = BorderStyle.None;
                    table.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
                    table.ColumnHeadersHeight = Math.Max(table.ColumnHeadersHeight, 36);
                    table.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
                    table.EnableHeadersVisualStyles = false;
                    table.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(231, 239, 246);
                    table.ColumnHeadersDefaultCellStyle.ForeColor = uiText;
                    table.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(231, 239, 246);
                    table.DefaultCellStyle.BackColor = Color.White;
                    table.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 251, 253);
                    table.DefaultCellStyle.SelectionBackColor = Color.FromArgb(215, 239, 229);
                    table.DefaultCellStyle.SelectionForeColor = uiText;
                    break;
                case Button button:
                    button.Cursor = Cursors.Hand;
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = Color.FromArgb(207, 219, 229);
                    button.FlatAppearance.BorderSize = 1;
                    if (button.BackColor == SystemColors.Control)
                        button.BackColor = Color.White;
                    // Preserve stronger existing semantics, e.g. green Run,
                    // red Stop and blue Save buttons.
                    if (button.BackColor == Color.White)
                        button.ForeColor = uiText;
                    break;
                case TextBox entry when !entry.Multiline:
                    entry.BorderStyle = BorderStyle.FixedSingle;
                    entry.BackColor = Color.White;
                    break;
                case ComboBox combo:
                    combo.FlatStyle = FlatStyle.Flat;
                    combo.BackColor = Color.White;
                    break;
            }
            if (child.HasChildren) StyleLegacyChildren(child);
        }
    }

    // The legacy voice options were positioned with fixed X/Y coordinates.
    // When the new sidebar reduced usable width they overlapped each other.
    // Reparent them into responsive cells rather than squeezing old positions.
    private void FixTtsBatchLayout()
    {
        ttsRootLayout.RowStyles[0].Height = 202;
        ttsRootLayout.RowStyles[1].Height = 153;
        ttsTopLayout.ColumnStyles[0].Width = 44;
        ttsTopLayout.ColumnStyles[1].Width = 36;
        ttsTopLayout.ColumnStyles[2].Width = 20;
        grpVoiceSettings.Padding = new Padding(8, 14, 8, 6);
        grpOptions.Padding = new Padding(8, 15, 8, 6);
        var voiceControls = new Control[] {
            chkVoiceSettings, lblSpeed, numSpeed, lblStability, numStability,
            lblSimilarity, numSimilarity, lblStyle, numStyle,
            chkSpeakerBoost, lblDenoise, numDenoise, btnResetVoiceSettings
        };
        foreach (var c in voiceControls) grpVoiceSettings.Controls.Remove(c);
        var settingsGrid = new TableLayoutPanel {
            Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 5,
            Padding = new Padding(2, 1, 2, 0), Margin = Padding.Empty
        };
        settingsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 29));
        settingsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 19));
        settingsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
        settingsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        for (int i = 0; i < 5; i++)
            settingsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
        void Place(Control c, int col, int row)
        {
            c.Dock = DockStyle.Fill;
            c.Margin = new Padding(2);
            if (c is Label lbl) lbl.TextAlign = ContentAlignment.MiddleLeft;
            settingsGrid.Controls.Add(c, col, row);
        }
        Place(chkVoiceSettings, 0, 0);
        settingsGrid.SetColumnSpan(chkVoiceSettings, 4);
        Place(lblSpeed, 0, 1); Place(numSpeed, 1, 1);
        Place(lblStyle, 2, 1); Place(numStyle, 3, 1);
        Place(lblStability, 0, 2); Place(numStability, 1, 2);
        Place(chkSpeakerBoost, 2, 2);
        settingsGrid.SetColumnSpan(chkSpeakerBoost, 2);
        Place(lblSimilarity, 0, 3); Place(numSimilarity, 1, 3);
        Place(lblDenoise, 2, 3); Place(numDenoise, 3, 3);
        Place(btnResetVoiceSettings, 2, 4);
        settingsGrid.SetColumnSpan(btnResetVoiceSettings, 2);
        grpVoiceSettings.Controls.Add(settingsGrid);

        var optionsControls = new Control[] {
            chkRetryFailed, chkAutoSplit, chkSkipExisting, chkAutoJoin,
            chkDeleteSegments, lblSplitChars, txtSplitChars, btnAdvancedSettings
        };
        foreach (var c in optionsControls) grpOptions.Controls.Remove(c);
        var optionsGrid = new TableLayoutPanel {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4,
            Margin = Padding.Empty, Padding = new Padding(3, 2, 3, 1)
        };
        optionsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        optionsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        for (int i = 0; i < 4; i++)
            optionsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
        void Opt(Control c, int col, int row)
        {
            c.Dock = DockStyle.Fill;
            c.Margin = new Padding(2);
            optionsGrid.Controls.Add(c, col, row);
        }
        Opt(chkRetryFailed, 0, 0); Opt(chkAutoSplit, 1, 0);
        Opt(chkSkipExisting, 0, 1); Opt(chkAutoJoin, 1, 1);
        Opt(chkDeleteSegments, 0, 2);
        var splitRow = new TableLayoutPanel { Dock = DockStyle.Fill,
            ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        splitRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        splitRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        lblSplitChars.Dock = DockStyle.Fill;
        lblSplitChars.TextAlign = ContentAlignment.MiddleLeft;
        txtSplitChars.Dock = DockStyle.Fill;
        splitRow.Controls.Add(lblSplitChars, 0, 0);
        splitRow.Controls.Add(txtSplitChars, 1, 0);
        Opt(splitRow, 1, 2);
        Opt(btnAdvancedSettings, 0, 3);
        optionsGrid.SetColumnSpan(btnAdvancedSettings, 2);
        grpOptions.Controls.Add(optionsGrid);
    }

    private void RefreshModernNavigation()
    {
        foreach (var (page, button) in navigationButtons)
        {
            bool selected = mainTabControl.SelectedTab == page;
            button.BackColor = selected ? Color.FromArgb(224, 244, 237) : Color.White;
            button.ForeColor = selected ? uiPrimary : uiText;
            button.Font = new Font("Segoe UI", 10,
                selected ? FontStyle.Bold : FontStyle.Regular);
        }
        if (pageHeading != null)
            pageHeading.Text = navigationButtons.TryGetValue(mainTabControl.SelectedTab!, out var selectedButton)
                ? selectedButton.Text[4..].Trim() : "DvaForDev Studio";
    }
}
