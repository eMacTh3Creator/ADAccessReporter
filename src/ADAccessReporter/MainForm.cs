using System.Data;
using System.Drawing;

namespace ADAccessReporter;

public sealed class MainForm : Form
{
    private static readonly Color PageColor = Color.FromArgb(226, 232, 240);
    private static readonly Color HeaderColor = Color.FromArgb(17, 24, 39);
    private static readonly Color InkColor = Color.FromArgb(23, 32, 51);
    private static readonly Color MutedColor = Color.FromArgb(91, 102, 122);
    private static readonly Color LineColor = Color.FromArgb(207, 217, 230);
    private static readonly Color BlueColor = Color.FromArgb(37, 99, 235);
    private static readonly Color TealColor = Color.FromArgb(20, 184, 166);
    private static readonly Color AmberColor = Color.FromArgb(217, 119, 6);

    private readonly ActiveDirectoryService _adService = new();
    private readonly FolderAclService _folderAclService = new();

    private readonly Panel _contentHost = new();
    private readonly Button _groupsNavButton = new();
    private readonly Button _folderNavButton = new();
    private readonly Button _logNavButton = new();
    private readonly List<Button> _mainNavButtons = new();
    private Control? _groupsView;
    private Control? _folderView;
    private Control? _logView;

    private readonly TextBox _groupInput = new();
    private readonly TextBox _domainInput = new();
    private readonly CheckBox _recursiveCheck = new();
    private readonly CheckBox _includeDisabledCheck = new();
    private readonly Button _loadGroupsButton = new();
    private readonly Button _exportMembersButton = new();
    private readonly Button _exportComparisonButton = new();
    private readonly Label _groupStatus = new();
    private readonly Button _membersResultButton = new();
    private readonly Button _comparisonResultButton = new();
    private readonly Panel _membersGridPanel = new();
    private readonly Panel _comparisonGridPanel = new();
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
        MinimumSize = new Size(1180, 760);
        Size = new Size(1280, 820);
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        BackColor = PageColor;
        ShowIcon = true;

        var appIcon = LoadAppIcon();
        if (appIcon is not null)
        {
            Icon = appIcon;
        }

        BuildInterface();
        SetIdleState();
        AppendLog("Ready. Use your current Windows credentials, or enter a domain/controller when needed.");
    }

    private void BuildInterface()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            BackColor = PageColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _groupsView = BuildGroupsView();
        _folderView = BuildFolderView();
        _logView = BuildLogView();

        _contentHost.Dock = DockStyle.Fill;
        _contentHost.BackColor = PageColor;
        _contentHost.Controls.Add(_groupsView);
        _contentHost.Controls.Add(_folderView);
        _contentHost.Controls.Add(_logView);

        _statusStrip.Items.Add(_statusLabel);
        _statusStrip.SizingGrip = false;
        _statusStrip.BackColor = Color.White;
        _statusLabel.Text = "Ready";

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildMainNavigation(), 0, 1);
        root.Controls.Add(_contentHost, 0, 2);
        Controls.Add(root);
        Controls.Add(_statusStrip);

        ShowMainView(_groupsView, _groupsNavButton);
    }

    private Control BuildGroupsView()
    {
        var page = new Panel { Dock = DockStyle.Fill, BackColor = PageColor, Padding = new Padding(44, 18, 44, 28) };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.White
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 220));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var top = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.White
        };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 350));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));

        var groupPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            Padding = new Padding(0, 0, 26, 18),
            BackColor = Color.White
        };
        groupPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        groupPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        groupPanel.Controls.Add(MakeLabel("Group names, one per line or semicolon separated"), 0, 0);
        _groupInput.Multiline = true;
        _groupInput.ScrollBars = ScrollBars.None;
        _groupInput.Dock = DockStyle.Fill;
        _groupInput.PlaceholderText = "Domain Admins\r\nVPN Users\r\nDOMAIN\\Finance Share Access";
        _groupInput.Font = new Font("Consolas", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        ConfigureTextBox(_groupInput);
        groupPanel.Controls.Add(_groupInput, 0, 1);

        var optionPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 5,
            Padding = new Padding(0, 0, 26, 18),
            BackColor = Color.White
        };
        optionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        optionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        optionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        optionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        optionPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        optionPanel.Controls.Add(MakeLabel("Domain or controller (optional)"), 0, 0);
        _domainInput.Dock = DockStyle.Top;
        _domainInput.PlaceholderText = "corp.example.com or DC01";
        ConfigureTextBox(_domainInput);
        optionPanel.Controls.Add(_domainInput, 0, 1);
        _recursiveCheck.Text = "Include nested group members";
        _recursiveCheck.Checked = true;
        _recursiveCheck.Dock = DockStyle.Fill;
        ConfigureCheckBox(_recursiveCheck);
        optionPanel.Controls.Add(_recursiveCheck, 0, 2);
        _includeDisabledCheck.Text = "Include disabled user accounts";
        _includeDisabledCheck.Dock = DockStyle.Fill;
        ConfigureCheckBox(_includeDisabledCheck);
        optionPanel.Controls.Add(_includeDisabledCheck, 0, 3);
        _groupStatus.Dock = DockStyle.Fill;
        _groupStatus.ForeColor = MutedColor;
        _groupStatus.TextAlign = ContentAlignment.BottomLeft;
        optionPanel.Controls.Add(_groupStatus, 0, 4);

        var buttonPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 5,
            Padding = new Padding(0, 24, 0, 18),
            BackColor = Color.White
        };
        for (var i = 0; i < 4; i++)
        {
            buttonPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        }
        buttonPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        ConfigureButton(_loadGroupsButton, "Load Groups", LoadGroupsButton_Click, BlueColor, Color.White);
        ConfigureButton(_exportMembersButton, "Export Members CSV", ExportMembersButton_Click, TealColor, Color.White);
        ConfigureButton(_exportComparisonButton, "Export Comparison CSV", ExportComparisonButton_Click, Color.White, InkColor);
        buttonPanel.Controls.Add(_loadGroupsButton, 0, 0);
        buttonPanel.Controls.Add(_exportMembersButton, 0, 1);
        buttonPanel.Controls.Add(_exportComparisonButton, 0, 2);

        top.Controls.Add(groupPanel, 0, 0);
        top.Controls.Add(optionPanel, 1, 0);
        top.Controls.Add(buttonPanel, 2, 0);

        var resultArea = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            BackColor = Color.White
        };
        resultArea.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        resultArea.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var resultNav = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 0, 0, 0)
        };

        ConfigureSegmentButton(_membersResultButton, "Members", (_, _) => ShowGroupResultView(true));
        ConfigureSegmentButton(_comparisonResultButton, "Comparison", (_, _) => ShowGroupResultView(false));
        resultNav.Controls.Add(_membersResultButton);
        resultNav.Controls.Add(_comparisonResultButton);

        var resultHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(0, 4, 0, 0)
        };
        ConfigureGrid(_membersGrid);
        ConfigureGrid(_comparisonGrid);
        _comparisonGrid.CellFormatting += ComparisonGrid_CellFormatting;
        _membersGridPanel.Dock = DockStyle.Fill;
        _membersGridPanel.BackColor = Color.White;
        _comparisonGridPanel.Dock = DockStyle.Fill;
        _comparisonGridPanel.BackColor = Color.White;
        _membersGridPanel.Controls.Add(_membersGrid);
        _comparisonGridPanel.Controls.Add(_comparisonGrid);
        resultHost.Controls.Add(_comparisonGridPanel);
        resultHost.Controls.Add(_membersGridPanel);
        resultArea.Controls.Add(resultNav, 0, 0);
        resultArea.Controls.Add(resultHost, 0, 1);

        layout.Controls.Add(top, 0, 0);
        layout.Controls.Add(resultArea, 0, 1);
        page.Controls.Add(CreateSurface(layout));
        ShowGroupResultView(true);
        return page;
    }

    private Control BuildFolderView()
    {
        var page = new Panel { Dock = DockStyle.Fill, BackColor = PageColor, Padding = new Padding(44, 18, 44, 28) };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.White
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 220));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var top = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.White
        };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));

        var pathPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            Padding = new Padding(0, 0, 26, 18),
            BackColor = Color.White
        };
        pathPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        pathPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        pathPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        pathPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        pathPanel.Controls.Add(MakeLabel("Folder or file path"), 0, 0);
        _folderPathInput.Dock = DockStyle.Fill;
        _folderPathInput.PlaceholderText = @"\\server\share\folder";
        ConfigureTextBox(_folderPathInput);
        pathPanel.Controls.Add(_folderPathInput, 0, 1);
        var note = MakeLabel("Reports NTFS permissions visible from this path. Share-level permissions are separate.");
        note.ForeColor = MutedColor;
        pathPanel.Controls.Add(note, 0, 2);
        _folderStatus.Dock = DockStyle.Fill;
        _folderStatus.ForeColor = MutedColor;
        _folderStatus.TextAlign = ContentAlignment.BottomLeft;
        pathPanel.Controls.Add(_folderStatus, 0, 3);

        var optionPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 7,
            Padding = new Padding(0, 0, 26, 18),
            BackColor = Color.White
        };
        optionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        optionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        for (var i = 0; i < 4; i++)
        {
            optionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        }
        optionPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        optionPanel.Controls.Add(MakeLabel("Domain or controller (optional)"), 0, 0);
        _folderDomainInput.Dock = DockStyle.Top;
        _folderDomainInput.PlaceholderText = "corp.example.com or DC01";
        ConfigureTextBox(_folderDomainInput);
        optionPanel.Controls.Add(_folderDomainInput, 0, 1);
        _expandAclGroupsCheck.Text = "Expand AD groups in ACL";
        _expandAclGroupsCheck.Checked = true;
        _includeInheritedCheck.Text = "Include inherited entries";
        _includeInheritedCheck.Checked = true;
        _includeDenyCheck.Text = "Include deny entries";
        _includeDenyCheck.Checked = true;
        _includeDisabledAclUsersCheck.Text = "Include disabled expanded users";
        ConfigureCheckBox(_expandAclGroupsCheck);
        ConfigureCheckBox(_includeInheritedCheck);
        ConfigureCheckBox(_includeDenyCheck);
        ConfigureCheckBox(_includeDisabledAclUsersCheck);
        optionPanel.Controls.Add(_expandAclGroupsCheck, 0, 2);
        optionPanel.Controls.Add(_includeInheritedCheck, 0, 3);
        optionPanel.Controls.Add(_includeDenyCheck, 0, 4);
        optionPanel.Controls.Add(_includeDisabledAclUsersCheck, 0, 5);

        var buttonPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            Padding = new Padding(0, 24, 0, 18),
            BackColor = Color.White
        };
        for (var i = 0; i < 3; i++)
        {
            buttonPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        }
        buttonPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        ConfigureButton(_browseFolderButton, "Browse", BrowseFolderButton_Click, Color.White, InkColor);
        ConfigureButton(_scanFolderButton, "Scan Rights", ScanFolderButton_Click, BlueColor, Color.White);
        ConfigureButton(_exportFolderButton, "Export Rights CSV", ExportFolderButton_Click, TealColor, Color.White);
        buttonPanel.Controls.Add(_browseFolderButton, 0, 0);
        buttonPanel.Controls.Add(_scanFolderButton, 0, 1);
        buttonPanel.Controls.Add(_exportFolderButton, 0, 2);

        top.Controls.Add(pathPanel, 0, 0);
        top.Controls.Add(optionPanel, 1, 0);
        top.Controls.Add(buttonPanel, 2, 0);

        ConfigureGrid(_folderGrid);
        layout.Controls.Add(top, 0, 0);
        layout.Controls.Add(_folderGrid, 0, 1);
        page.Controls.Add(CreateSurface(layout));
        return page;
    }

    private Control BuildLogView()
    {
        var page = new Panel { Dock = DockStyle.Fill, BackColor = PageColor, Padding = new Padding(44, 18, 44, 28) };
        _logTextBox.Dock = DockStyle.Fill;
        _logTextBox.Multiline = true;
        _logTextBox.ReadOnly = true;
        _logTextBox.ScrollBars = ScrollBars.Both;
        _logTextBox.WordWrap = false;
        _logTextBox.BackColor = Color.White;
        _logTextBox.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point);
        ConfigureTextBox(_logTextBox);
        page.Controls.Add(CreateSurface(_logTextBox));
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
                DomainOrServer = _domainInput.Text.Trim(),
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
                DomainOrServer = _folderDomainInput.Text.Trim(),
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
        grid.BorderStyle = BorderStyle.None;
        grid.RowHeadersVisible = false;
        grid.EnableHeadersVisualStyles = false;
        grid.GridColor = LineColor;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        grid.ColumnHeadersHeight = 42;
        grid.RowTemplate.Height = 42;
        grid.DefaultCellStyle.BackColor = Color.White;
        grid.DefaultCellStyle.ForeColor = Color.FromArgb(70, 90, 126);
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
        grid.DefaultCellStyle.SelectionForeColor = InkColor;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = InkColor;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.White;
    }

    private static void ConfigureButton(Button button, string text, EventHandler handler, Color backColor, Color foreColor)
    {
        button.Text = text;
        button.Dock = DockStyle.Fill;
        button.Height = 48;
        button.Margin = new Padding(0, 0, 0, 14);
        button.FlatStyle = FlatStyle.Flat;
        button.BackColor = backColor;
        button.ForeColor = foreColor;
        button.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        button.FlatAppearance.BorderColor = backColor == Color.White ? LineColor : backColor;
        button.FlatAppearance.BorderSize = 1;
        button.UseVisualStyleBackColor = false;
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
            ForeColor = InkColor
        };
    }

    private Panel BuildHeader()
    {
        var header = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = HeaderColor,
            Padding = new Padding(44, 0, 44, 0)
        };

        var logo = new PictureBox
        {
            Width = 42,
            Height = 42,
            SizeMode = PictureBoxSizeMode.StretchImage,
            Left = 44,
            Top = 23,
            Image = Icon?.ToBitmap()
        };

        var title = new Label
        {
            Text = "AD Access Reporter",
            AutoSize = true,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 20F, FontStyle.Bold, GraphicsUnit.Point),
            Left = 96,
            Top = 23
        };

        header.Controls.Add(logo);
        header.Controls.Add(title);
        return header;
    }

    private Panel BuildMainNavigation()
    {
        var nav = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = PageColor,
            Padding = new Padding(44, 0, 44, 0)
        };

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = PageColor,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 0, 0, 0)
        };

        ConfigureNavButton(_groupsNavButton, "AD Groups", () => ShowMainView(_groupsView, _groupsNavButton));
        ConfigureNavButton(_folderNavButton, "Folder Rights", () => ShowMainView(_folderView, _folderNavButton));
        ConfigureNavButton(_logNavButton, "Activity Log", () => ShowMainView(_logView, _logNavButton));
        _mainNavButtons.AddRange(new[] { _groupsNavButton, _folderNavButton, _logNavButton });

        flow.Controls.Add(_groupsNavButton);
        flow.Controls.Add(_folderNavButton);
        flow.Controls.Add(_logNavButton);
        nav.Controls.Add(flow);
        return nav;
    }

    private static Panel CreateSurface(Control content)
    {
        var surface = new SurfacePanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(32)
        };

        content.Dock = DockStyle.Fill;
        surface.Controls.Add(content);
        return surface;
    }

    private void ShowMainView(Control? view, Button activeButton)
    {
        if (view is null)
        {
            return;
        }

        foreach (Control control in _contentHost.Controls)
        {
            control.Visible = ReferenceEquals(control, view);
        }

        view.BringToFront();

        foreach (var button in _mainNavButtons)
        {
            var active = ReferenceEquals(button, activeButton);
            button.ForeColor = active ? Color.White : Color.FromArgb(61, 88, 132);
            button.BackColor = PageColor;
            button.FlatAppearance.BorderColor = active ? Color.White : PageColor;
        }
    }

    private void ShowGroupResultView(bool showMembers)
    {
        _membersGridPanel.Visible = showMembers;
        _comparisonGridPanel.Visible = !showMembers;

        if (showMembers)
        {
            _membersGridPanel.BringToFront();
        }
        else
        {
            _comparisonGridPanel.BringToFront();
        }

        SetSegmentState(_membersResultButton, showMembers);
        SetSegmentState(_comparisonResultButton, !showMembers);
    }

    private static void ConfigureTextBox(TextBox textBox)
    {
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.BackColor = Color.White;
        textBox.ForeColor = InkColor;
    }

    private static void ConfigureCheckBox(CheckBox checkBox)
    {
        checkBox.AutoSize = true;
        checkBox.AutoEllipsis = false;
        checkBox.CheckAlign = ContentAlignment.MiddleLeft;
        checkBox.TextAlign = ContentAlignment.MiddleLeft;
        checkBox.Dock = DockStyle.Left;
        checkBox.MinimumSize = new Size(0, 28);
        checkBox.Margin = new Padding(0, 2, 0, 0);
        checkBox.ForeColor = MutedColor;
        checkBox.BackColor = Color.White;
        checkBox.FlatStyle = FlatStyle.System;
    }

    private static void ConfigureNavButton(Button button, string text, Action action)
    {
        button.Text = text;
        button.Width = 150;
        button.Height = 42;
        button.Margin = new Padding(0, 6, 10, 6);
        button.FlatStyle = FlatStyle.Flat;
        button.BackColor = PageColor;
        button.ForeColor = Color.FromArgb(61, 88, 132);
        button.Font = new Font("Segoe UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point);
        button.TextAlign = ContentAlignment.MiddleLeft;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = PageColor;
        button.UseVisualStyleBackColor = false;
        button.Click += (_, _) => action();
    }

    private static void ConfigureSegmentButton(Button button, string text, EventHandler handler)
    {
        button.Text = text;
        button.Width = 132;
        button.Height = 36;
        button.Margin = new Padding(0, 0, 8, 0);
        button.FlatStyle = FlatStyle.Flat;
        button.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        button.TextAlign = ContentAlignment.MiddleLeft;
        button.UseVisualStyleBackColor = false;
        button.Click += handler;
        SetSegmentState(button, false);
    }

    private static void SetSegmentState(Button button, bool active)
    {
        button.BackColor = Color.White;
        button.ForeColor = active ? InkColor : MutedColor;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = active ? LineColor : Color.White;
    }

    private static Icon? LoadAppIcon()
    {
        try
        {
            return Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        }
        catch
        {
            return null;
        }
    }

    private void ComparisonGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.Value is not string text || e.RowIndex < 0)
        {
            return;
        }

        var cellStyle = e.CellStyle;
        if (cellStyle is null)
        {
            return;
        }

        var columnName = _comparisonGrid.Columns[e.ColumnIndex].HeaderText;
        if (string.Equals(columnName, "Status", StringComparison.OrdinalIgnoreCase))
        {
            if (text.Contains("Common", StringComparison.OrdinalIgnoreCase))
            {
                cellStyle.ForeColor = TealColor;
            }
            else if (text.Contains("Only", StringComparison.OrdinalIgnoreCase))
            {
                cellStyle.ForeColor = AmberColor;
            }
            else
            {
                cellStyle.ForeColor = BlueColor;
            }
        }
        else if (string.Equals(text, "Yes", StringComparison.OrdinalIgnoreCase))
        {
            cellStyle.ForeColor = Color.FromArgb(70, 90, 126);
        }
    }

    private sealed class SurfacePanel : Panel
    {
        public SurfacePanel()
        {
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var pen = new Pen(LineColor);
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }
    }
}
