using System.Data;
using System.Drawing;

namespace ADAccessReporter;

public sealed class MainForm : Form
{
    private readonly ActiveDirectoryService _adService = new();
    private readonly FolderAclService _folderAclService = new();

    private readonly TextBox _groupInput = new();
    private readonly TextBox _domainInput = new();
    private readonly CheckBox _recursiveCheck = new();
    private readonly CheckBox _includeDisabledCheck = new();
    private readonly Button _loadGroupsButton = new();
    private readonly Button _exportMembersButton = new();
    private readonly Button _exportComparisonButton = new();
    private readonly Label _groupStatus = new();
    private readonly DataGridView _membersGrid = new();
    private readonly DataGridView _comparisonGrid = new();

    private readonly TextBox _folderPathInput = new();
    private readonly TextBox _folderDomainInput = new();
    private readonly CheckBox _expandAclGroupsCheck = new();
    private readonly CheckBox _includeInheritedCheck = new();
    private readonly CheckBox _includeDenyCheck = new();
    private readonly CheckBox _includeDisabledAclUsersCheck = new();
    private readonly Button _browseFolderButton = new();
    private readonly Button _scanFolderButton = new();
    private readonly Button _exportFolderButton = new();
    private readonly Label _folderStatus = new();
    private readonly DataGridView _folderGrid = new();

    private readonly TextBox _logTextBox = new();
    private readonly StatusStrip _statusStrip = new();
    private readonly ToolStripStatusLabel _statusLabel = new();

    private DataTable _membersTable = new();
    private DataTable _comparisonTable = new();
    private DataTable _folderTable = new();

    public MainForm()
    {
        Text = "AD Access Reporter";
        MinimumSize = new Size(1060, 720);
        Size = new Size(1280, 820);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        BackColor = Color.FromArgb(246, 248, 250);

        BuildInterface();
        SetIdleState();
        AppendLog("Ready. Use your current Windows credentials, or enter a domain/controller when needed.");
    }

    private void BuildInterface()
    {
        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Padding = new Point(12, 6)
        };

        tabs.TabPages.Add(BuildGroupsTab());
        tabs.TabPages.Add(BuildFolderTab());
        tabs.TabPages.Add(BuildLogTab());

        _statusStrip.Items.Add(_statusLabel);
        _statusStrip.SizingGrip = false;
        _statusLabel.Text = "Ready";

        Controls.Add(tabs);
        Controls.Add(_statusStrip);
    }

    private TabPage BuildGroupsTab()
    {
        var page = new TabPage("AD Groups") { BackColor = BackColor };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 190));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var top = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1
        };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));

        var groupPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            Padding = new Padding(0, 0, 12, 8)
        };
        groupPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        groupPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        groupPanel.Controls.Add(MakeLabel("Group names, one per line or semicolon separated"), 0, 0);
        _groupInput.Multiline = true;
        _groupInput.ScrollBars = ScrollBars.Vertical;
        _groupInput.Dock = DockStyle.Fill;
        _groupInput.PlaceholderText = "Domain Admins\r\nVPN Users\r\nDOMAIN\\Finance Share Access";
        groupPanel.Controls.Add(_groupInput, 0, 1);

        var optionPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 5,
            Padding = new Padding(0, 0, 12, 8)
        };
        optionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        optionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        optionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        optionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        optionPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        optionPanel.Controls.Add(MakeLabel("Domain or controller (optional)"), 0, 0);
        _domainInput.Dock = DockStyle.Top;
        _domainInput.PlaceholderText = "corp.example.com or DC01";
        optionPanel.Controls.Add(_domainInput, 0, 1);
        _recursiveCheck.Text = "Include nested group members";
        _recursiveCheck.Checked = true;
        _recursiveCheck.Dock = DockStyle.Fill;
        optionPanel.Controls.Add(_recursiveCheck, 0, 2);
        _includeDisabledCheck.Text = "Include disabled user accounts";
        _includeDisabledCheck.Dock = DockStyle.Fill;
        optionPanel.Controls.Add(_includeDisabledCheck, 0, 3);
        _groupStatus.Dock = DockStyle.Fill;
        _groupStatus.ForeColor = Color.FromArgb(83, 92, 104);
        _groupStatus.TextAlign = ContentAlignment.BottomLeft;
        optionPanel.Controls.Add(_groupStatus, 0, 4);

        var buttonPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 5,
            Padding = new Padding(0, 24, 0, 8)
        };
        for (var i = 0; i < 4; i++)
        {
            buttonPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        }
        buttonPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        ConfigureButton(_loadGroupsButton, "Load Groups", LoadGroupsButton_Click);
        ConfigureButton(_exportMembersButton, "Export Members CSV", ExportMembersButton_Click);
        ConfigureButton(_exportComparisonButton, "Export Comparison CSV", ExportComparisonButton_Click);
        buttonPanel.Controls.Add(_loadGroupsButton, 0, 0);
        buttonPanel.Controls.Add(_exportMembersButton, 0, 1);
        buttonPanel.Controls.Add(_exportComparisonButton, 0, 2);

        top.Controls.Add(groupPanel, 0, 0);
        top.Controls.Add(optionPanel, 1, 0);
        top.Controls.Add(buttonPanel, 2, 0);

        var resultTabs = new TabControl { Dock = DockStyle.Fill };
        var membersPage = new TabPage("Members") { BackColor = BackColor };
        var comparisonPage = new TabPage("Comparison") { BackColor = BackColor };
        ConfigureGrid(_membersGrid);
        ConfigureGrid(_comparisonGrid);
        membersPage.Controls.Add(_membersGrid);
        comparisonPage.Controls.Add(_comparisonGrid);
        resultTabs.TabPages.Add(membersPage);
        resultTabs.TabPages.Add(comparisonPage);

        layout.Controls.Add(top, 0, 0);
        layout.Controls.Add(resultTabs, 0, 1);
        page.Controls.Add(layout);
        return page;
    }

    private TabPage BuildFolderTab()
    {
        var page = new TabPage("Folder Rights") { BackColor = BackColor };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 174));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var top = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1
        };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));

        var pathPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            Padding = new Padding(0, 0, 12, 8)
        };
        pathPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        pathPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        pathPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        pathPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        pathPanel.Controls.Add(MakeLabel("Folder or file path"), 0, 0);
        _folderPathInput.Dock = DockStyle.Fill;
        _folderPathInput.PlaceholderText = @"\\server\share\folder";
        pathPanel.Controls.Add(_folderPathInput, 0, 1);
        var note = MakeLabel("Reports NTFS permissions visible from this path. Share-level permissions are separate.");
        note.ForeColor = Color.FromArgb(83, 92, 104);
        pathPanel.Controls.Add(note, 0, 2);
        _folderStatus.Dock = DockStyle.Fill;
        _folderStatus.ForeColor = Color.FromArgb(83, 92, 104);
        _folderStatus.TextAlign = ContentAlignment.BottomLeft;
        pathPanel.Controls.Add(_folderStatus, 0, 3);

        var optionPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 7,
            Padding = new Padding(0, 0, 12, 8)
        };
        optionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        optionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        for (var i = 0; i < 4; i++)
        {
            optionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        }
        optionPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        optionPanel.Controls.Add(MakeLabel("Domain or controller (optional)"), 0, 0);
        _folderDomainInput.Dock = DockStyle.Top;
        _folderDomainInput.PlaceholderText = "corp.example.com or DC01";
        optionPanel.Controls.Add(_folderDomainInput, 0, 1);
        _expandAclGroupsCheck.Text = "Expand AD groups in ACL";
        _expandAclGroupsCheck.Checked = true;
        _includeInheritedCheck.Text = "Include inherited entries";
        _includeInheritedCheck.Checked = true;
        _includeDenyCheck.Text = "Include deny entries";
        _includeDenyCheck.Checked = true;
        _includeDisabledAclUsersCheck.Text = "Include disabled expanded users";
        optionPanel.Controls.Add(_expandAclGroupsCheck, 0, 2);
        optionPanel.Controls.Add(_includeInheritedCheck, 0, 3);
        optionPanel.Controls.Add(_includeDenyCheck, 0, 4);
        optionPanel.Controls.Add(_includeDisabledAclUsersCheck, 0, 5);

        var buttonPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            Padding = new Padding(0, 24, 0, 8)
        };
        for (var i = 0; i < 3; i++)
        {
            buttonPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        }
        buttonPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        ConfigureButton(_browseFolderButton, "Browse", BrowseFolderButton_Click);
        ConfigureButton(_scanFolderButton, "Scan Rights", ScanFolderButton_Click);
        ConfigureButton(_exportFolderButton, "Export Rights CSV", ExportFolderButton_Click);
        buttonPanel.Controls.Add(_browseFolderButton, 0, 0);
        buttonPanel.Controls.Add(_scanFolderButton, 0, 1);
        buttonPanel.Controls.Add(_exportFolderButton, 0, 2);

        top.Controls.Add(pathPanel, 0, 0);
        top.Controls.Add(optionPanel, 1, 0);
        top.Controls.Add(buttonPanel, 2, 0);

        ConfigureGrid(_folderGrid);
        layout.Controls.Add(top, 0, 0);
        layout.Controls.Add(_folderGrid, 0, 1);
        page.Controls.Add(layout);
        return page;
    }

    private TabPage BuildLogTab()
    {
        var page = new TabPage("Activity Log") { BackColor = BackColor };
        _logTextBox.Dock = DockStyle.Fill;
        _logTextBox.Multiline = true;
        _logTextBox.ReadOnly = true;
        _logTextBox.ScrollBars = ScrollBars.Both;
        _logTextBox.WordWrap = false;
        _logTextBox.BackColor = Color.White;
        _logTextBox.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point);
        page.Padding = new Padding(12);
        page.Controls.Add(_logTextBox);
        return page;
    }

    private async void LoadGroupsButton_Click(object? sender, EventArgs e)
    {
        var groups = ParseGroupInputs(_groupInput.Text);
        if (groups.Count == 0)
        {
            MessageBox.Show(this, "Enter at least one AD group name.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        SetBusyState("Loading AD groups...");
        try
        {
            var options = new GroupLookupOptions
            {
                DomainOrServer = _folderDomainInput.Text.Trim(),
                Recursive = _recursiveCheck.Checked,
                IncludeDisabled = _includeDisabledCheck.Checked
            };

            var progress = new Progress<string>(AppendLog);
            var records = await _adService.GetGroupMembersAsync(groups, options, progress, CancellationToken.None);
            _membersTable = BuildMembersTable(records);
            _comparisonTable = BuildComparisonTable(records, groups);
            _membersGrid.DataSource = _membersTable;
            _comparisonGrid.DataSource = _comparisonTable;

            _groupStatus.Text = $"{records.Count:N0} member row(s), {_comparisonTable.Rows.Count:N0} distinct user(s)";
            AppendLog($"AD group load complete: {records.Count:N0} member row(s).");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            AppendLog($"AD group load failed: {ex.Message}");
        }
        finally
        {
            SetIdleState();
        }
    }

    private async void ScanFolderButton_Click(object? sender, EventArgs e)
    {
        var path = _folderPathInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(path))
        {
            MessageBox.Show(this, "Enter a folder, file, or UNC path.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        SetBusyState("Scanning folder rights...");
        try
        {
            var options = new FolderRightsOptions
            {
                DomainOrServer = _domainInput.Text.Trim(),
                ExpandAdGroups = _expandAclGroupsCheck.Checked,
                IncludeInherited = _includeInheritedCheck.Checked,
                IncludeDenyEntries = _includeDenyCheck.Checked,
                IncludeDisabledUsers = _includeDisabledAclUsersCheck.Checked
            };

            var progress = new Progress<string>(AppendLog);
            var records = await _folderAclService.GetRightsAsync(path, options, progress, CancellationToken.None);
            _folderTable = BuildFolderTable(records);
            _folderGrid.DataSource = _folderTable;
            _folderStatus.Text = $"{records.Count:N0} permission row(s)";
            AppendLog($"Folder rights scan complete: {records.Count:N0} row(s).");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            AppendLog($"Folder rights scan failed: {ex.Message}");
        }
        finally
        {
            SetIdleState();
        }
    }

    private void BrowseFolderButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Choose a folder to inspect",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _folderPathInput.Text = dialog.SelectedPath;
        }
    }

    private void ExportMembersButton_Click(object? sender, EventArgs e)
    {
        ExportTable(_membersTable, "ad-group-members.csv");
    }

    private void ExportComparisonButton_Click(object? sender, EventArgs e)
    {
        ExportTable(_comparisonTable, "ad-group-comparison.csv");
    }

    private void ExportFolderButton_Click(object? sender, EventArgs e)
    {
        ExportTable(_folderTable, "folder-rights.csv");
    }

    private void ExportTable(DataTable table, string defaultFileName)
    {
        if (table.Rows.Count == 0)
        {
            MessageBox.Show(this, "There are no rows to export yet.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            FileName = defaultFileName,
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            AddExtension = true,
            DefaultExt = "csv",
            OverwritePrompt = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        CsvExporter.WriteDataTable(table, dialog.FileName);
        AppendLog($"Exported {table.Rows.Count:N0} row(s) to {dialog.FileName}");
        MessageBox.Show(this, "CSV export complete.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void SetBusyState(string status)
    {
        UseWaitCursor = true;
        _loadGroupsButton.Enabled = false;
        _scanFolderButton.Enabled = false;
        _browseFolderButton.Enabled = false;
        _exportMembersButton.Enabled = false;
        _exportComparisonButton.Enabled = false;
        _exportFolderButton.Enabled = false;
        _statusLabel.Text = status;
        AppendLog(status);
    }

    private void SetIdleState()
    {
        UseWaitCursor = false;
        _loadGroupsButton.Enabled = true;
        _scanFolderButton.Enabled = true;
        _browseFolderButton.Enabled = true;
        _exportMembersButton.Enabled = _membersTable.Rows.Count > 0;
        _exportComparisonButton.Enabled = _comparisonTable.Rows.Count > 0;
        _exportFolderButton.Enabled = _folderTable.Rows.Count > 0;
        _statusLabel.Text = "Ready";
    }

    private void AppendLog(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action<string>(AppendLog), message);
            return;
        }

        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
        _logTextBox.AppendText(line);
    }

    private static DataTable BuildMembersTable(IEnumerable<AdGroupMemberRecord> records)
    {
        var table = new DataTable("AD group members");
        AddColumns(table, "Input Group", "Resolved Group", "Name", "SAM Account", "Display Name", "UPN", "Email", "Enabled", "Distinguished Name", "SID");

        foreach (var record in records.OrderBy(r => r.InputGroup).ThenBy(r => r.DisplayName).ThenBy(r => r.SamAccountName))
        {
            table.Rows.Add(
                record.InputGroup,
                record.GroupName,
                record.Name,
                record.SamAccountName,
                record.DisplayName,
                record.UserPrincipalName,
                record.Email,
                record.Enabled,
                record.DistinguishedName,
                record.Sid);
        }

        return table;
    }

    private static DataTable BuildComparisonTable(IEnumerable<AdGroupMemberRecord> records, IReadOnlyList<string> requestedGroups)
    {
        var recordList = records.ToList();
        var groups = requestedGroups.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var table = new DataTable("AD group comparison");
        AddColumns(table, "User Key", "Display Name", "SAM Account", "UPN", "Email");

        foreach (var group in groups)
        {
            table.Columns.Add(group);
        }

        AddColumns(table, "Present In", "Missing From", "Status");

        var users = recordList
            .GroupBy(r => r.ComparisonKey, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => ActiveDirectoryService.FirstNonEmpty(g.First().DisplayName, g.First().SamAccountName, g.Key));

        foreach (var userGroup in users)
        {
            var first = userGroup.First();
            var presentGroups = groups
                .Where(group => userGroup.Any(record => string.Equals(record.InputGroup, group, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            var missingGroups = groups.Except(presentGroups, StringComparer.OrdinalIgnoreCase).ToList();
            var values = new List<object?>
            {
                first.ComparisonKey,
                first.DisplayName,
                first.SamAccountName,
                first.UserPrincipalName,
                first.Email
            };

            values.AddRange(groups.Select(group => presentGroups.Contains(group, StringComparer.OrdinalIgnoreCase) ? "Yes" : string.Empty));
            values.Add(string.Join("; ", presentGroups));
            values.Add(string.Join("; ", missingGroups));
            values.Add(GetComparisonStatus(presentGroups.Count, groups.Count, presentGroups));
            table.Rows.Add(values.ToArray());
        }

        return table;
    }

    private static DataTable BuildFolderTable(IEnumerable<FolderRightsRecord> records)
    {
        var table = new DataTable("Folder rights");
        AddColumns(
            table,
            "Path",
            "Entry Kind",
            "Identity",
            "Principal Type",
            "Access Type",
            "Rights",
            "Is Inherited",
            "Inheritance",
            "Propagation",
            "Expanded User",
            "Expanded SAM",
            "Expanded Display Name",
            "Expanded Email",
            "Expanded UPN",
            "Expanded SID",
            "Notes");

        foreach (var record in records.OrderBy(r => r.Identity).ThenBy(r => r.EntryKind).ThenBy(r => r.ExpandedDisplayName))
        {
            table.Rows.Add(
                record.Path,
                record.EntryKind,
                record.Identity,
                record.PrincipalType,
                record.AccessType,
                record.Rights,
                record.IsInherited,
                record.Inheritance,
                record.Propagation,
                record.ExpandedUserName,
                record.ExpandedSamAccountName,
                record.ExpandedDisplayName,
                record.ExpandedEmail,
                record.ExpandedUserPrincipalName,
                record.ExpandedSid,
                record.Notes);
        }

        return table;
    }

    private static string GetComparisonStatus(int presentCount, int groupCount, IReadOnlyCollection<string> presentGroups)
    {
        if (groupCount == 0 || presentCount == 0)
        {
            return "Not present";
        }

        if (presentCount == groupCount)
        {
            return "Common to all";
        }

        if (presentCount == 1)
        {
            return $"Only in {presentGroups.First()}";
        }

        return $"Shared by {presentCount} groups";
    }

    private static IReadOnlyList<string> ParseGroupInputs(string input)
    {
        return input
            .Split(new[] { '\r', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void AddColumns(DataTable table, params string[] names)
    {
        foreach (var name in names)
        {
            table.Columns.Add(name);
        }
    }

    private static void ConfigureGrid(DataGridView grid)
    {
        grid.Dock = DockStyle.Fill;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.ReadOnly = true;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = true;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
        grid.BackgroundColor = Color.White;
        grid.BorderStyle = BorderStyle.FixedSingle;
        grid.RowHeadersVisible = false;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(230, 236, 242);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(20, 34, 51);
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 252, 253);
    }

    private static void ConfigureButton(Button button, string text, EventHandler handler)
    {
        button.Text = text;
        button.Dock = DockStyle.Fill;
        button.Height = 32;
        button.Margin = new Padding(0, 0, 0, 8);
        button.FlatStyle = FlatStyle.System;
        button.Click += handler;
    }

    private static Label MakeLabel(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(20, 34, 51)
        };
    }
}
