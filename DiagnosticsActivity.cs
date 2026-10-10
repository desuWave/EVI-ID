
using System;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using EVI_ID.Shell;

namespace EVI_ID;

[Activity(
    Label = "EVI-ID Diagnostics",
    Exported = false)]
public class DiagnosticsActivity : Activity
{
    private const int VisibleLogLimit = 30;

    private LinearLayout _logContainer = null!;
    private LinearLayout _projectsContainer = null!;
    private EditText _projectNameInput = null!;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        Logger.Initialize(FilesDir!.AbsolutePath);
        StorageManager.Initialize(FilesDir!.AbsolutePath);

        Logger.Info("Diagnostics activity opened.");

        var root = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical
        };
        root.SetPadding(20, 20, 20, 12);
        root.SetBackgroundColor(Color.White);

        var title = new TextView(this)
        {
            Text = "EVI-ID Diagnostics",
            TextSize = 24
        };
        title.SetTextColor(Color.Black);

        var content = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical
        };

        var scroll = new ScrollView(this);
        scroll.AddView(content);

        _projectsContainer = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical
        };

        var projectsTitle = new TextView(this)
        {
            Text = "Project storage test",
            TextSize = 20
        };
        projectsTitle.SetTextColor(Color.Black);

        _projectNameInput = new EditText(this)
        {
            Hint = "Enter test project name"
        };
        _projectNameInput.SetSingleLine(true);

        var createProjectButton = new Button(this)
        {
            Text = "Create project"
        };

        var testButton = new Button(this)
        {
            Text = "Write test log entry"
        };

        var copyLogButton = new Button(this)
        {
            Text = "Copy log"
        };

        var copyRecentButton = new Button(this)
        {
            Text = "Copy recent log"
        };

        var clearButton = new Button(this)
        {
            Text = "Clear screen"
        };

        var operationButton = new Button(this)
        {
            Text = "Test shell operation"
        };

        var diagnosticCommandButton = new Button(this)
        {
            Text = "Run diagnostic command"
        };

        var executableInput = new EditText(this)
        {
            Hint = "Executable path",
            Text = "/system/bin/id"
        };
        executableInput.SetSingleLine(true);

        var argumentsInput = new EditText(this)
        {
            Hint = "Arguments (optional)"
        };
        argumentsInput.SetSingleLine(true);

        var runCommandButton = new Button(this)
        {
            Text = "Run command"
        };

        var logTitle = new TextView(this)
        {
            Text = "Diagnostics log",
            TextSize = 20
        };
        logTitle.SetTextColor(Color.Black);

        _logContainer = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical
        };

        content.AddView(projectsTitle);
        content.AddView(_projectsContainer);
        content.AddView(_projectNameInput);
        content.AddView(createProjectButton);
        content.AddView(testButton);
        content.AddView(copyLogButton);
        content.AddView(copyRecentButton);
        content.AddView(clearButton);
        content.AddView(operationButton);
        content.AddView(diagnosticCommandButton);
        content.AddView(executableInput);
        content.AddView(argumentsInput);
        content.AddView(runCommandButton);
        content.AddView(logTitle);
        content.AddView(_logContainer);

        createProjectButton.Click += (_, _) =>
        {
            string name = _projectNameInput.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(name))
            {
                Logger.Warning(
                    "Project creation rejected: empty name.");
                RefreshLogs();
                return;
            }

            try
            {
                string path =
                    StorageManager.CreateProjectDirectory(name);

                Logger.Info(
                    $"Project created: {name}; Path={path}");

                _projectNameInput.Text = "";
                RefreshProjects();
            }
            catch (Exception ex)
            {
                Logger.Error(
                    $"Project creation failed: {name}; Error={ex.Message}");
            }

            RefreshLogs();
        };

        testButton.Click += (_, _) =>
        {
            Logger.Info("Test log entry created.");
            RefreshLogs();
        };

        copyLogButton.Click += (_, _) => CopyLog(false);
        copyRecentButton.Click += (_, _) => CopyLog(true);

        clearButton.Click += (_, _) =>
        {
            ClearLogScreen();
        };

        operationButton.Click += (_, _) =>
        {
            var operation =
                ProcessManager.StartOperation("Diagnostic test");

            ProcessManager.CompleteOperation(
                operation.Id,
                "Diagnostic operation completed successfully.");

            RefreshLogs();
        };

        diagnosticCommandButton.Click += async (_, _) =>
        {
            diagnosticCommandButton.Enabled = false;

            try
            {
                await CommandRunner.RunIdentityCheckAsync();
            }
            finally
            {
                diagnosticCommandButton.Enabled = true;
                RefreshLogs();
            }
        };

        runCommandButton.Click += async (_, _) =>
        {
            string executable =
                executableInput.Text?.Trim() ?? "";

            string argumentText =
                argumentsInput.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(executable))
            {
                Logger.Warning(
                    "Command rejected: executable path is empty.");
                RefreshLogs();
                return;
            }

            string[] arguments =
                string.IsNullOrWhiteSpace(argumentText)
                    ? Array.Empty<string>()
                    : argumentText.Split(
                        ' ',
                        StringSplitOptions.RemoveEmptyEntries);

            runCommandButton.Enabled = false;

            try
            {
                await CommandRunner.RunAsync(
                    executable, arguments);
            }
            finally
            {
                runCommandButton.Enabled = true;
                RefreshLogs();
            }
        };

        root.AddView(title);
        root.AddView(scroll, new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            0,
            1));

        SetContentView(root);
        root.SetFitsSystemWindows(true);

        RefreshProjects();
        RefreshLogs();
    }

    private void RefreshProjects()
    {
        _projectsContainer.RemoveAllViews();

        var projects = StorageManager.GetProjectDirectories();

        if (projects.Count == 0)
        {
            var empty = new TextView(this)
            {
                Text = "No projects yet",
                TextSize = 16
            };
            empty.SetTextColor(Color.DarkGray);
            _projectsContainer.AddView(empty);
            return;
        }

        foreach (string path in projects)
        {
            var item = new TextView(this)
            {
                Text = "📁 " + System.IO.Path.GetFileName(path),
                TextSize = 16
            };
            item.SetTextColor(Color.Black);
            _projectsContainer.AddView(item);
        }
    }

    private void RefreshLogs()
    {
        _logContainer.RemoveAllViews();

        var entries = Logger.Entries;
        int start = Math.Max(0, entries.Count - VisibleLogLimit);

        for (int i = start; i < entries.Count; i++)
        {
            var line = new TextView(this)
            {
                Text = entries[i],
                TextSize = 14
            };
            line.SetTextColor(Color.Black);
            _logContainer.AddView(line);
        }
    }

    private void ClearLogScreen()
    {
        _logContainer.RemoveAllViews();
    }

    private void CopyLog(bool recentOnly)
    {
        var entries = Logger.Entries;
        int start = recentOnly
            ? Math.Max(0, entries.Count - VisibleLogLimit)
            : 0;

        string text = string.Join(
            System.Environment.NewLine,
            entries.Skip(start));

        var clipboard = (ClipboardManager)GetSystemService(
            ClipboardService)!;

        clipboard.PrimaryClip = ClipData.NewPlainText(
            recentOnly ? "EVI-ID Recent Log" : "EVI-ID Log",
            text);

        Logger.Info(
            $"{(recentOnly ? "Recent log" : "Log")} copied; " +
            $"entries={entries.Count - start}");

        RefreshLogs();
    }
}
