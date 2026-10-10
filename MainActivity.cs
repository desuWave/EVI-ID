
using System;
using System.IO;
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
    Label = "EVI-ID",
    MainLauncher = true,
    Exported = true)]
public class MainActivity : Activity
{
    private LinearLayout _screenHost = null!;
    private LinearLayout _consolePanel = null!;
    private LinearLayout _guiPanel = null!;
    private LinearLayout _projectsPanel = null!;
    private LinearLayout _projectsContainer = null!;

    private EditText _projectNameInput = null!;
    private EditText _executableInput = null!;
    private EditText _argumentsInput = null!;
    private TextView _consoleOutput = null!;

    private Button _consoleTab = null!;
    private Button _guiTab = null!;
    private Button _projectsTab = null!;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        Logger.Initialize(FilesDir!.AbsolutePath);
        StorageManager.Initialize(FilesDir!.AbsolutePath);

        Logger.Info("EVI-ID main interface started.");

        var root = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical
        };
        root.SetPadding(20, 20, 20, 12);
        root.SetBackgroundColor(Color.White);

        var header = new LinearLayout(this)
        {
            Orientation = Orientation.Horizontal
        };

        header.SetGravity(GravityFlags.CenterVertical);

        var title = new TextView(this)
        {
            Text = "EVI Shell",
            TextSize = 26,
            Gravity = GravityFlags.CenterVertical
        };
        title.SetTextColor(Color.Black);

        var diagnosticsButton = new Button(this)
        {
            Text = "Diagnostics"
        };

        header.AddView(title, new LinearLayout.LayoutParams(
            0, ViewGroup.LayoutParams.WrapContent, 1));

        header.AddView(diagnosticsButton);

        _screenHost = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical
        };

        var screenScroll = new ScrollView(this);
        screenScroll.AddView(_screenHost);

        BuildConsolePanel();
        BuildGuiPanel();
        BuildProjectsPanel();

        _screenHost.AddView(_consolePanel);
        _screenHost.AddView(_guiPanel);
        _screenHost.AddView(_projectsPanel);

        var navigation = new LinearLayout(this)
        {
            Orientation = Orientation.Horizontal
        };

        _consoleTab = new Button(this) { Text = "Console" };
        _guiTab = new Button(this) { Text = "GUI" };
        _projectsTab = new Button(this) { Text = "Projects" };

        navigation.AddView(_consoleTab,
            new LinearLayout.LayoutParams(
                0, ViewGroup.LayoutParams.WrapContent, 1));

        navigation.AddView(_guiTab,
            new LinearLayout.LayoutParams(
                0, ViewGroup.LayoutParams.WrapContent, 1));

        navigation.AddView(_projectsTab,
            new LinearLayout.LayoutParams(
                0, ViewGroup.LayoutParams.WrapContent, 1));

        _consoleTab.Click += (_, _) => ShowPanel("console");
        _guiTab.Click += (_, _) => ShowPanel("gui");
        _projectsTab.Click += (_, _) => ShowPanel("projects");

        diagnosticsButton.Click += (_, _) =>
        {
            StartActivity(new Intent(
                this, typeof(DiagnosticsActivity)));
        };

        root.AddView(header);

        root.AddView(screenScroll,
            new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                0, 1));

        root.AddView(navigation);

        ActionBar?.Hide();

        SetContentView(root);

        HideSystemUi();

        ShowPanel("console");
        RefreshProjects();
    }

private void HideSystemUi()
{
    Window!.DecorView.SystemUiVisibility =
        (StatusBarVisibility)(
            SystemUiFlags.Fullscreen |
            SystemUiFlags.HideNavigation |
            SystemUiFlags.ImmersiveSticky);
}

public override void OnWindowFocusChanged(bool hasFocus)
{
    base.OnWindowFocusChanged(hasFocus);

    if (hasFocus)
        HideSystemUi();
}

    private void BuildConsolePanel()
    {
        _consolePanel = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical
        };

        var heading = new TextView(this)
        {
            Text = "Console",
            TextSize = 22
        };
        heading.SetTextColor(Color.Black);

        var description = new TextView(this)
        {
             Text = "Enter an executable path and optional arguments.",
             TextSize = 14
        };

        description.SetPadding(0, 8, 0, 12);
        
        description.SetTextColor(Color.DarkGray);

        _executableInput = new EditText(this)
        {
            Hint = "Executable path",
            Text = "/system/bin/id"
        };
        _executableInput.SetSingleLine(true);

        _argumentsInput = new EditText(this)
        {
            Hint = "Arguments (optional)"
        };
        _argumentsInput.SetSingleLine(true);

        var runButton = new Button(this)
        {
            Text = "Run command"
        };

        _consoleOutput = new TextView(this)
        {
            Text = "EVI Shell ready.",
            TextSize = 14
        };
        _consoleOutput.SetTextColor(Color.Black);
        _consoleOutput.SetTextIsSelectable(true);

        runButton.Click += async (_, _) =>
        {
            string executable =
                _executableInput.Text?.Trim() ?? "";

            string argumentText =
                _argumentsInput.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(executable))
            {
                _consoleOutput.Text =
                    "Error: executable path is empty.";
                return;
            }

            string[] arguments =
                string.IsNullOrWhiteSpace(argumentText)
                    ? Array.Empty<string>()
                    : argumentText.Split(
                        ' ',
                        StringSplitOptions.RemoveEmptyEntries);

            runButton.Enabled = false;
            _consoleOutput.Text = "Running...";

            try
            {
                var result = await CommandRunner.RunAsync(
                    executable, arguments);

                string output = result.StandardOutput.Trim();
                string error = result.StandardError.Trim();

                _consoleOutput.Text =
                    $"Exit code: {result.ExitCode}\n\n" +
                    (output.Length > 0
                        ? $"stdout:\n{output}\n\n"
                        : "") +
                    (error.Length > 0
                        ? $"stderr:\n{error}"
                        : "");

                if (string.IsNullOrWhiteSpace(output) &&
                    string.IsNullOrWhiteSpace(error))
                {
                    _consoleOutput.Text =
                        $"Command finished. Exit code: {result.ExitCode}";
                }
            }
            catch (Exception ex)
            {
                _consoleOutput.Text =
                    $"Console error: {ex.GetType().Name}: {ex.Message}";
            }
            finally
            {
                runButton.Enabled = true;
            }
        };

        _consolePanel.AddView(heading);
        _consolePanel.AddView(description);
        _consolePanel.AddView(_executableInput);
        _consolePanel.AddView(_argumentsInput);
        _consolePanel.AddView(runButton);
        _consolePanel.AddView(_consoleOutput);
    }

    private void BuildGuiPanel()
    {
        _guiPanel = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical,
            Visibility = ViewStates.Gone
        };

        var heading = new TextView(this)
        {
            Text = "EVI Shell — GUI",
            TextSize = 22
        };
        heading.SetTextColor(Color.Black);

        var message = new TextView(this)
        {
            Text =
                "The graphical workspace is not implemented yet.\n\n" +
                "This tab is reserved for EVI Shell's future " +
                "graphical tools and workspace."
        };
        message.SetTextColor(Color.DarkGray);

        _guiPanel.AddView(heading);
        _guiPanel.AddView(message);
    }

    private void BuildProjectsPanel()
    {
        _projectsPanel = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical,
            Visibility = ViewStates.Gone
        };

        var heading = new TextView(this)
        {
            Text = "Projects",
            TextSize = 22
        };
        heading.SetTextColor(Color.Black);

        _projectNameInput = new EditText(this)
        {
            Hint = "Enter project name"
        };
        _projectNameInput.SetSingleLine(true);

        var createButton = new Button(this)
        {
            Text = "Create project"
        };

        _projectsContainer = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical
        };

        createButton.Click += (_, _) =>
        {
            string name = _projectNameInput.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(name))
            {
                Logger.Warning(
                    "Project creation rejected: empty name.");
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

                new AlertDialog.Builder(this)
                    .SetTitle("Project creation failed")
                    .SetMessage(ex.Message)
                    .SetPositiveButton("OK", (_, _) => { })
                    .Show();
            }
        };

        _projectsPanel.AddView(heading);
        _projectsPanel.AddView(_projectNameInput);
        _projectsPanel.AddView(createButton);
        _projectsPanel.AddView(_projectsContainer);
    }

    private void ShowPanel(string name)
    {
        _consolePanel.Visibility =
            name == "console" ? ViewStates.Visible : ViewStates.Gone;

        _guiPanel.Visibility =
            name == "gui" ? ViewStates.Visible : ViewStates.Gone;

        _projectsPanel.Visibility =
            name == "projects" ? ViewStates.Visible : ViewStates.Gone;

        _consoleTab.Enabled = name != "console";
        _guiTab.Enabled = name != "gui";
        _projectsTab.Enabled = name != "projects";

        if (name == "projects")
            RefreshProjects();
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
            string name = System.IO.Path.GetFileName(path);

            var item = new TextView(this)
            {
                Text = "📁 " + name,
                TextSize = 16
            };
            item.SetTextColor(Color.Black);
            _projectsContainer.AddView(item);
        }
    }
}
