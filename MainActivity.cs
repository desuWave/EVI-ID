using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Provider;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;

using EVI_ID.Shell;
using EVI_ID.Testing;
using EVI_Refactor;

using EVI_ID.Mdl;
using EVI_ID.Mdl.Editor;
using EVI_ID.Mdl.Git;
using EVI_ID.Mdl.Build;

namespace EVI_ID;

[Activity(
    Label = "EVI-ID",
    MainLauncher = true,
    Exported = true)]
public class MainActivity : Activity
{
    private const int OpenTreeRequest = 1001;

    private LinearLayout? _root;
    private LinearLayout? _screenHost;
    private LinearLayout? _consolePanel;
    private LinearLayout? _guiPanel;
    private LinearLayout? _projectsPanel;
    private LinearLayout? _projectsContainer;

    private TextView? _consoleOutput;
    private EditText? _consoleInput;
    
    private ScrollView? _consoleScrollView;
    private OutputNavigator?
_outputNavigator;
    
    private Button? _executeButton;
    private TextView? _workingDirectoryLabel;

    private Button? _consoleTab;
    private Button? _guiTab;
    private Button? _projectsTab;

    private Button? _connectFolderButton;
    private LinearLayout? _externalFoldersContainer;

    private LinearLayout? _externalBrowserPanel;
    private LinearLayout? _externalBrowserContainer;
    private TextView? _externalBrowserTitle;
    private Button? _externalBrowserBackButton;

    private EditText? _projectNameInput;

    private string _currentDirectory = "";

    private string? _activeTreeUri;
    private string? _activeDocumentId;

    private EVIFileSystem? _fileSystem;
    private ShellSession? _shellSession;

    private readonly List<string> _history = new();

    private readonly List<
        (string DocumentId, string Name)> _externalFolderNavigation = new();

    protected override void OnCreate(
        Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        string appDataDirectory =
            FilesDir?.AbsolutePath
            ?? throw new InvalidOperationException(
                "EVI-ID application data directory is unavailable.");

        StartupDiagnostics.Initialize(
            appDataDirectory);

        Logger.Initialize(
    System.IO.Path.Combine(
        appDataDirectory,
        "EVI-ID",
        "Logs"));

        Logger.Info(
            "EVI-ID MainActivity started.");

        StorageManager.Initialize(
            appDataDirectory);

        _currentDirectory =
            StorageManager.RootDirectory;

        _shellSession =
            new ShellSession(
                _currentDirectory);

        _fileSystem =
            new EVIFileSystem(
                this,
                _currentDirectory);

        BuildMainInterface();

        LoadPersistedExternalFolder();

        Logger.Info(
            "EVI-ID MainActivity initialized.");
    }

    private void BuildMainInterface()
{
    var root =
        new LinearLayout(this)
        {
            Orientation =
                Orientation.Vertical
        };

    root.SetPadding(
        20,
        12,
        20,
        8);

    root.SetBackgroundColor(
        Color.White);

    _root = root;

    // Header
    var header =
        new LinearLayout(this)
        {
            Orientation =
                Orientation.Horizontal
        };

    header.SetGravity(
        GravityFlags.CenterVertical);

    var title =
        new TextView(this)
        {
            Text = "EVI Shell",
            TextSize = 26
        };

    title.SetTextColor(
        Color.Black);

    var diagnosticsButton =
        new Button(this)
        {
            Text = "Diagnostics"
        };

    diagnosticsButton.Click +=
        (_, _) => OpenDiagnostics();

    header.AddView(
        title,
        new LinearLayout.LayoutParams(
            0,
            ViewGroup.LayoutParams.WrapContent,
            1));

    header.AddView(
        diagnosticsButton,
        new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.WrapContent,
            ViewGroup.LayoutParams.WrapContent));

    root.AddView(
        header,
        new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent));

    // One central panel host.
    _screenHost =
        new LinearLayout(this)
        {
            Orientation =
                Orientation.Vertical
        };

    root.AddView(
        _screenHost,
        new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            0,
            1));

    // Build the actual panels.
    BuildConsolePanel();
    BuildGuiPanel();
    BuildProjectsPanel();

    if (_consolePanel is null ||
        _guiPanel is null ||
        _projectsPanel is null)
    {
        throw new InvalidOperationException(
            "Failed to initialize EVI Shell panels.");
    }

    _screenHost.AddView(
        _consolePanel,
        new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent));

    _screenHost.AddView(
        _guiPanel,
        new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent));

    _screenHost.AddView(
        _projectsPanel,
        new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent));

    // Bottom navigation.
    var navigation =
        new LinearLayout(this)
        {
            Orientation =
                Orientation.Horizontal
        };

    _consoleTab =
        new Button(this)
        {
            Text = "Console"
        };

    _guiTab =
        new Button(this)
        {
            Text = "GUI"
        };

    _projectsTab =
        new Button(this)
        {
            Text = "Projects"
        };

    _consoleTab.Click +=
        (_, _) =>
            ShowPanel("console");

    _guiTab.Click +=
        (_, _) =>
            ShowPanel("gui");

    _projectsTab.Click +=
        (_, _) =>
        {
            ShowPanel("projects");
            OpenProjects();
        };

    navigation.AddView(
        _consoleTab,
        new LinearLayout.LayoutParams(
            0,
            ViewGroup.LayoutParams.WrapContent,
            1));

    navigation.AddView(
        _guiTab,
        new LinearLayout.LayoutParams(
            0,
            ViewGroup.LayoutParams.WrapContent,
            1));

    navigation.AddView(
        _projectsTab,
        new LinearLayout.LayoutParams(
            0,
            ViewGroup.LayoutParams.WrapContent,
            1));

    root.AddView(
        navigation,
        new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent));

    ActionBar?.Hide();

    SetContentView(root);

    root.SetOnApplyWindowInsetsListener(
        new RootInsetsListener());

    root.RequestApplyInsets();

    ShowPanel("console");
}

    private void BuildConsolePanel()
    {
        _consolePanel =
            new LinearLayout(this)
            {
                Orientation =
                    Orientation.Vertical
            };

        var heading =
            new TextView(this)
            {
                Text = "EVI Shell",
                TextSize = 22
            };

        heading.SetTextColor(Color.Black);

        _workingDirectoryLabel =
            new TextView(this)
            {
                Text =
                    _shellSession?.CurrentDirectory
                    ?? _currentDirectory,
                TextSize = 12
            };

        _workingDirectoryLabel.SetTextColor(
            Color.DarkGray);

        _consoleInput =
            new EditText(this)
            {
                Hint = "Enter command"
            };

        _consoleInput.InputType =
            Android.Text.InputTypes.ClassText |
            Android.Text.InputTypes.TextFlagNoSuggestions;

        _consoleInput.SetSingleLine(true);

        _executeButton =
            new Button(this)
            {
                Text = "Execute"
            };

        _consoleOutput =
            new TextView(this)
            {
                Text = "EVI Shell ready.\n",
                TextSize = 14
            };

        _consoleOutput.SetTextColor(Color.Black);
        _consoleOutput.SetTextIsSelectable(true);

        _consoleScrollView =
            new ScrollView(this);

        _consoleScrollView.AddView(
            _consoleOutput,
            new ScrollView.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent));

        _outputNavigator =
            new OutputNavigator(
                _consoleOutput,
                _consoleScrollView);

        _executeButton.Click +=
            async (_, _) =>
            {
                await ExecuteCommandAsync();
            };

        _consoleInput.EditorAction +=
            async (_, e) =>
            {
                if (e.ActionId ==
                    Android.Views.InputMethods.ImeAction.Done)
                {
                    e.Handled = true;
                    await ExecuteCommandAsync();
                }
            };

        _consolePanel.AddView(
            heading);

        _consolePanel.AddView(
            _workingDirectoryLabel);

                _consolePanel.AddView(
            _consoleScrollView,
            new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                0,
                1));

        _consolePanel.AddView(
            _consoleInput);

        _consolePanel.AddView(
            _executeButton);
    }

    private void BuildGuiPanel()
    {
        _guiPanel =
            new LinearLayout(this)
            {
                Orientation =
                    Orientation.Vertical,
                Visibility =
                    ViewStates.Gone
            };

        var heading =
            new TextView(this)
            {
                Text = "EVI Shell — GUI",
                TextSize = 22
            };

        heading.SetTextColor(Color.Black);

        var message =
            new TextView(this)
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
    _projectsPanel =
        new LinearLayout(this)
        {
            Orientation =
                Orientation.Vertical,
            Visibility =
                ViewStates.Gone
        };

    var heading =
        new TextView(this)
        {
            Text = "Projects",
            TextSize = 22
        };

    heading.SetTextColor(
        Color.Black);

    _projectNameInput =
        new EditText(this)
        {
            Hint = "Enter project name"
        };

    _projectNameInput.SetSingleLine(true);

    var createButton =
        new Button(this)
        {
            Text = "Create project"
        };

    _connectFolderButton =
        new Button(this)
        {
            Text = "Connect existing folder"
        };

    _projectsContainer =
        new LinearLayout(this)
        {
            Orientation =
                Orientation.Vertical
        };

    _externalFoldersContainer =
        new LinearLayout(this)
        {
            Orientation =
                Orientation.Vertical
        };

    _externalBrowserPanel =
        new LinearLayout(this)
        {
            Orientation =
                Orientation.Vertical,
            Visibility =
                ViewStates.Gone
        };

    _externalBrowserBackButton =
        new Button(this)
        {
            Text = "← Back"
        };

    _externalBrowserTitle =
        new TextView(this)
        {
            Text = "Selected folder",
            TextSize = 16
        };

    _externalBrowserTitle.SetTextColor(
        Color.Black);

    _externalBrowserContainer =
        new LinearLayout(this)
        {
            Orientation =
                Orientation.Vertical
        };

    _externalBrowserBackButton.Click +=
        (_, _) =>
            GoToExternalFolderParent();

    _externalBrowserPanel.AddView(
        _externalBrowserBackButton);

    _externalBrowserPanel.AddView(
        _externalBrowserTitle);

    _externalBrowserPanel.AddView(
        _externalBrowserContainer);

    _connectFolderButton.Click +=
        (_, _) =>
        {
            OpenExternalFolder();
        };

    createButton.Click +=
        (_, _) =>
        {
            string name =
                _projectNameInput.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(name))
            {
                AppendConsole(
                    "Project name is empty.\n");

                return;
            }

            try
            {
                string path =
                    StorageManager.CreateProjectDirectory(
                        name);

                Logger.Info(
                    $"Project created: {name}; Path={path}");

                _projectNameInput.Text = "";

                RefreshProjects();
            }
            catch (Exception ex)
            {
                Logger.Error(
                    $"Project creation failed: " +
                    $"{name}; Error={ex.Message}");

                new AlertDialog.Builder(this)
                    .SetTitle(
                        "Project creation failed")
                    .SetMessage(
                        ex.Message)
                    .SetPositiveButton(
                        "OK",
                        (_, _) => { })
                    .Show();
            }
        };

    // Fixed upper controls.
    _projectsPanel.AddView(
        heading);

    _projectsPanel.AddView(
        _projectNameInput);

    _projectsPanel.AddView(
        createButton);

    _projectsPanel.AddView(
        _connectFolderButton);

    // Scrollable Projects content.
    var projectsScrollView =
        new ScrollView(this);

    var projectsContent =
        new LinearLayout(this)
        {
            Orientation =
                Orientation.Vertical
        };

    projectsContent.AddView(
        _projectsContainer);

    projectsContent.AddView(
        _externalFoldersContainer);

    projectsContent.AddView(
        _externalBrowserPanel);

    projectsScrollView.AddView(
        projectsContent,
        new ScrollView.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent));

    _projectsPanel.AddView(
        projectsScrollView,
        new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            0,
            1));

    RefreshProjects();
    RefreshExternalFolders();
}
    
    private void ShowPanel(
        string name)
    {
        if (_consolePanel is null ||
            _guiPanel is null ||
            _projectsPanel is null ||
            _consoleTab is null ||
            _guiTab is null ||
            _projectsTab is null)
        {
            return;
        }

        _consolePanel.Visibility =
            name == "console"
                ? ViewStates.Visible
                : ViewStates.Gone;

        _guiPanel.Visibility =
            name == "gui"
                ? ViewStates.Visible
                : ViewStates.Gone;

        _projectsPanel.Visibility =
            name == "projects"
                ? ViewStates.Visible
                : ViewStates.Gone;

        _consoleTab.Enabled =
            name != "console";

        _guiTab.Enabled =
            name != "gui";

        _projectsTab.Enabled =
            name != "projects";

        if (name == "projects")
            RefreshProjects();
    }

    private void RefreshProjects()
    {
        if (_projectsContainer is null)
            return;

        _projectsContainer.RemoveAllViews();

        var projects =
            StorageManager.GetProjectDirectories();

        if (projects.Count == 0)
        {
            var empty =
                new TextView(this)
                {
                    Text = "No projects yet",
                    TextSize = 16
                };

            empty.SetTextColor(
                Color.DarkGray);

            _projectsContainer.AddView(
                empty);

            return;
        }

        foreach (string path in projects)
        {
            string name =
                System.IO.Path.GetFileName(path);

            var item =
                new TextView(this)
                {
                    Text = "📁 " + name,
                    TextSize = 16
                };

            item.SetTextColor(
                Color.Black);

            _projectsContainer.AddView(
                item);
        }
    }

    private void RefreshExternalFolders()
    {
        if (_externalFoldersContainer is null)
            return;

        _externalFoldersContainer.RemoveAllViews();

        var folders =
            ExternalFolderManager.GetFolders(
                this);

        if (folders.Count == 0)
        {
            var empty =
                new TextView(this)
                {
                    Text =
                        "No external folders connected",
                    TextSize = 14
                };

            empty.SetTextColor(
                Color.DarkGray);

            _externalFoldersContainer.AddView(
                empty);

            return;
        }

        foreach (var folder in folders)
        {
            bool hasPermission =
                ExternalFolderManager.HasPersistedPermission(
                    this,
                    folder.UriString);

            var item =
                new TextView(this)
                {
                    Text =
                        (hasPermission
                            ? "📁 "
                            : "⚠️ ") +
                        folder.Name +
                        "\n" +
                        folder.UriString,
                    TextSize = 12
                };

            item.SetTextColor(
                Color.Black);

            item.SetTextIsSelectable(
                true);

            item.Click +=
                (_, _) =>
                {
                    OpenExternalFolder(
                        folder.UriString);
                };

            _externalFoldersContainer.AddView(
                item);
        }
    }

    private void OpenExternalFolder(
        string uriString)
    {
        try
        {
            _activeTreeUri =
                uriString;

            var treeUri =
                Android.Net.Uri.Parse(
                    uriString)
                ?? throw new InvalidOperationException(
                    "Invalid external folder URI.");

            _activeDocumentId =
                DocumentsContract.GetTreeDocumentId(
                    treeUri)
                ?? throw new InvalidOperationException(
                    "Could not obtain external folder DocumentId.");

_externalFolderNavigation.Clear();

            _fileSystem!.ConfigureExternal(
                _activeTreeUri,
                _activeDocumentId);

            _fileSystem.UseExternal();

            _currentDirectory =
    _fileSystem.GetCurrentRoot();

_externalBrowserPanel!.Visibility =
    ViewStates.Visible;

RefreshExternalFolderContents();

AppendConsole(
    $"External folder selected:\n" +
    $"{uriString}\n");
        }
        catch (Exception ex)
        {
            Logger.Error(
                $"External folder opening failed: {ex}");

            AppendConsole(
                $"{ex.GetType().Name}: " +
                $"{ex.Message}\n");
        }
    }

    private void RefreshExternalFolderContents()
{
    Logger.Info(
        "RefreshExternalFolderContents() started.");

    if (string.IsNullOrWhiteSpace(
            _activeTreeUri) ||
        string.IsNullOrWhiteSpace(
            _activeDocumentId))
    {
        return;
    }

    if (_externalBrowserContainer is null)
    {
        Logger.Error(
            "RefreshExternalFolderContents(): " +
            "_externalBrowserContainer is NULL.");

        AppendConsole(
            "External browser container is not initialized.\n");

        return;
    }

    try
    {
        var entries =
            SafFileSystem.ListDirectory(
                this,
                _activeTreeUri,
                _activeDocumentId);

        _externalBrowserContainer.RemoveAllViews();

        if (_externalBrowserTitle is not null)
        {
            _externalBrowserTitle.Text =
                $"External folder\n" +
                $"{_activeDocumentId}";
        }

        Logger.Info(
            $"SAF: {_activeDocumentId}; " +
            $"children={entries.Count}");

        if (entries.Count == 0)
        {
            var empty =
                new TextView(this)
                {
                    Text = "Folder is empty.",
                    TextSize = 14
                };

            empty.SetTextColor(
                Color.DarkGray);

            _externalBrowserContainer.AddView(
                empty);

            return;
        }

        foreach (var entry in entries)
        {
            Logger.Info(
                $"SAF ENTRY: " +
                $"name='{entry.Name}', " +
                $"id='{entry.DocumentId}', " +
                $"mime='{entry.MimeType}', " +
                $"dir={entry.IsDirectory}");

            var item =
                new TextView(this)
                {
                    Text =
                        (entry.IsDirectory
                            ? "📁 "
                            : "📄 ") +
                        entry.Name,
                    TextSize = 16
                };

            item.SetTextColor(
                Color.Black);

            item.SetPadding(
                16,
                16,
                16,
                16);

            item.SetTextIsSelectable(
                true);

            if (entry.IsDirectory)
{
    item.Click +=
        (_, _) =>
        {
            _externalFolderNavigation.Add(
                (
                    entry.DocumentId,
                    entry.Name));

            _activeDocumentId =
                entry.DocumentId;

            RefreshExternalFolderContents();
        };
}

            _externalBrowserContainer.AddView(
                item);
        }

        _externalBrowserPanel!.Visibility =
            ViewStates.Visible;
    }
    catch (Exception ex)
    {
        Logger.Error(
            $"Failed to refresh external folder: {ex}");

        AppendConsole(
            $"Failed to refresh external folder: " +
            $"{ex.Message}\n");
    }
}

    private void GoToExternalFolderParent()
{
    if (string.IsNullOrWhiteSpace(
            _activeTreeUri) ||
        string.IsNullOrWhiteSpace(
            _activeDocumentId))
    {
        return;
    }

    if (_externalFolderNavigation.Count == 0)
    {
        return;
    }

    _externalFolderNavigation.RemoveAt(
        _externalFolderNavigation.Count - 1);

    if (_externalFolderNavigation.Count == 0)
    {
        try
        {
            var treeUri =
                Android.Net.Uri.Parse(
                    _activeTreeUri)
                ?? throw new InvalidOperationException(
                    "Invalid external folder URI.");

            _activeDocumentId =
                DocumentsContract.GetTreeDocumentId(
                    treeUri)
                ?? throw new InvalidOperationException(
                    "Could not obtain root DocumentId.");

            RefreshExternalFolderContents();
        }
        catch (Exception ex)
        {
            Logger.Error(
                $"Failed to return to external root: {ex}");

            AppendConsole(
                $"Failed to return to external root: " +
                $"{ex.Message}\n");
        }

        return;
    }

    _activeDocumentId =
        _externalFolderNavigation[
            _externalFolderNavigation.Count - 1]
        .DocumentId;

    RefreshExternalFolderContents();
}

    private async Task ExecuteCommandAsync()
    {
        if (_consoleInput is null ||
            _consoleOutput is null ||
            _executeButton is null)
        {
            return;
        }

        string input =
            _consoleInput.Text?.Trim() ?? "";

        _consoleInput.Text = "";

        if (string.IsNullOrWhiteSpace(input))
            return;

        _history.Add(
            input);

        Logger.Info(
            $"Command started: {input}");

        AppendConsole(
            $"\n{GetPrompt()} > {input}\n");

        _executeButton.Enabled = false;
        _consoleInput.Enabled = false;

        try
        {
            string[] parts =
                input.Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
                return;

            string command =
                parts[0].ToLowerInvariant();

            string[] arguments =
                parts.Length > 1
                    ? parts[1..]
                    : Array.Empty<string>();

            bool handled =
                await ExecuteBuiltInCommandAsync(
                    command,
                    arguments);

            if (!handled)
            {
                await ExecuteExternalCommandAsync(
                    command,
                    arguments);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(
                $"Command execution failed: {ex}");

            AppendConsole(
                $"{ex.GetType().Name}: " +
                $"{ex.Message}\n");
        }
        finally
        {
            _executeButton.Enabled = true;
            _consoleInput.Enabled = true;
            _consoleInput.RequestFocus();
        }
    }

    private async Task<bool> ExecuteBuiltInCommandAsync(
        string command,
        string[] arguments)
    {
        switch (command)
        {
            case "help":
    if (arguments.Length == 0)
    {
        ShowHelp();
    }
    else
    {
        ShowCommandHelp(
            arguments[0]);
    }

    return true;

            case "pwd":
                AppendConsole(
                    GetPrompt() + "\n");
                return true;

            case "space":
                ExecuteSpaceCommand(
                    arguments);
                return true;

            case "ls":
                ExecuteLsCommand(
                    arguments);
                return true;

            case "cd":
                ExecuteCdCommand(
                    arguments);
                return true;

case "type":
    ExecuteTypeCommand(arguments);
    return true;

            case "cat":
                ExecuteCatCommand(
                    arguments);
                return true;

case "build":
    await ExecuteBuildCommandAsync(arguments);
    return true;

case "git":
    await ExecuteGitCommandAsync(arguments);
    return true;

            case "refactor":
     ExecuteRefactorCommand(arguments);
                return true;

            case "write":
                ExecuteWriteCommand(
                    arguments);
                return true;

            case "append":
                ExecuteAppendCommand(
                    arguments);
                return true;

            case "touch":
                ExecuteTouchCommand(
                    arguments);
                return true;

            case "mkdir":
                ExecuteMkdirCommand(
                    arguments);
                return true;

            case "rename":
                ExecuteRenameCommand(
                    arguments);
                return true;

            case "rm":
                ExecuteRemoveCommand(
                    arguments);
                return true;

            case "saf-test":
                TestSafRead();

                AppendConsole(
                    "SAF test started. " +
                    "Check Diagnostics → Logs.\n");

                return true;

case "editor":
    if (arguments.Length == 1 &&
        string.Equals(
            arguments[0],
            "jit-test",
            StringComparison.OrdinalIgnoreCase))
    {
        string result =
            EditorJitTest.Run();

        AppendConsole(
            result + "\n");

        return true;
    }

    AppendConsole(
        "Usage: editor jit-test\n");

    return true;

            case "clear":
            case "cls":
                _consoleOutput!.Text =
                    "EVI Shell ready.\n";
                return true;
                
case "head":
    _outputNavigator?.Head();
    return true;

case "tail":
    _outputNavigator?.Tail();
    return true;

case "lets":
    if (arguments.Length != 2 ||
        !string.Equals(
            arguments[0],
            "go",
            StringComparison.OrdinalIgnoreCase) ||
        !int.TryParse(
            arguments[1],
            out int line))
    {
        AppendConsole(
            "Usage: lets go <line>\n");

        return true;
    }

    if (_outputNavigator is null ||
        !_outputNavigator.Goto(line))
    {
        AppendConsole(
            "Invalid line.\n");
    }

    return true;

case "page":
    if (arguments.Length < 1)
    {
        AppendConsole(
            "Usage: page up|down\n");

        return true;
    }

    switch (arguments[0].ToLowerInvariant())
    {
        case "up":
            _outputNavigator?.PageUp();
            break;

        case "down":
            _outputNavigator?.PageDown();
            break;

        default:
            AppendConsole(
                "Usage: page up|down\n");
            break;
    }

    return true;

            case "refdiag":
                AppendConsole(
                    RuntimeReferenceDiagnostics.Run() + "\n" +
                    SafReferenceDiagnostics.Run(this) + "\n");

                return true;

                        case "utst":
            case "unit_test":
                AppendConsole(
                    UnitTestRunner.RunAll(this) + "\n");

                return true;

            default:
                return false;
        }
    }

private void ShowCommandHelp(
    string command)
{
    switch (command.ToLowerInvariant())
    {
        case "help":
            AppendConsole(
                "help\n" +
                "  Show available commands.\n" +
                "  Usage: help\n" +
                "  Usage: help <command>\n");
            break;

        case "pwd":
            AppendConsole(
                "pwd\n" +
                "  Show the current working directory.\n" +
                "  Usage: pwd\n");
            break;

        case "space":
            AppendConsole(
                "space\n" +
                "  Show or change the active storage space.\n" +
                "  Usage: space\n" +
                "  Usage: space internal\n" +
                "  Usage: space external\n");
            break;

        case "ls":
            AppendConsole(
                "ls\n" +
                "  List files and directories.\n" +
                "  Usage: ls\n");
            break;

        case "cd":
            AppendConsole(
                "cd\n" +
                "  Change the current working directory.\n" +
                "  Usage: cd <path>\n");
            break;

        case "type":
            AppendConsole(
                "type\n" +
                "  Display the contents of a file.\n" +
                "  Usage: type <file>\n");
            break;

        case "cat":
            AppendConsole(
                "cat\n" +
                "  Display the contents of a file.\n" +
                "  Usage: cat <file>\n");
            break;

        case "build":
            AppendConsole(
                "build\n" +
                "  Build a C# project or source file.\n" +
                "  Usage: build <file>\n");
            break;

        case "git":
            AppendConsole(
                "git\n" +
                "  Git integration commands.\n" +
                "  Usage: git version\n" +
                "  Usage: git clone <repository-url>\n");
            break;

        case "refactor":
            AppendConsole(
                "refactor\n" +
                "  Run the EVI-Refactor external module.\n" +
                "  Usage: refactor <file>\n");
            break;

        case "write":
            AppendConsole(
                "write\n" +
                "  Write text to a file.\n" +
                "  Usage: write <file> <text>\n");
            break;

        case "append":
            AppendConsole(
                "append\n" +
                "  Append text to a file.\n" +
                "  Usage: append <file> <text>\n");
            break;

        case "touch":
            AppendConsole(
                "touch\n" +
                "  Create an empty file.\n" +
                "  Usage: touch <file>\n");
            break;

        case "mkdir":
            AppendConsole(
                "mkdir\n" +
                "  Create a directory.\n" +
                "  Usage: mkdir <directory>\n");
            break;

        case "rename":
            AppendConsole(
                "rename\n" +
                "  Rename a file or directory.\n" +
                "  Usage: rename <path> <new-name>\n");
            break;

        case "rm":
            AppendConsole(
                "rm\n" +
                "  Remove a file or directory.\n" +
                "  Usage: rm <path>\n");
            break;

        case "saf-test":
            AppendConsole(
                "saf-test\n" +
                "  Test Android Storage Access Framework integration.\n" +
                "  Usage: saf-test\n");
            break;

        case "editor":
            AppendConsole(
                "editor\n" +
                "  Embedded source editor commands.\n" +
                "  Usage: editor jit-test\n");
            break;

        case "clear":
        case "cls":
            AppendConsole(
                "clear / cls\n" +
                "  Clear the console output.\n" +
                "  Usage: clear\n" +
                "  Usage: cls\n");
            break;

        case "head":
            AppendConsole(
                "head\n" +
                "  Move output view to the beginning.\n" +
                "  Usage: head\n");
            break;

        case "tail":
            AppendConsole(
                "tail\n" +
                "  Move output view to the end.\n" +
                "  Usage: tail\n");
            break;

        case "lets":
            AppendConsole(
                "lets go\n" +
                "  Move output view to a specific line.\n" +
                "  Usage: lets go <line>\n");
            break;

        case "page":
            AppendConsole(
                "page\n" +
                "  Move output view by one screen.\n" +
                "  Usage: page up\n" +
                "  Usage: page down\n");
            break;

        case "refdiag":
            AppendConsole(
                "refdiag\n" +
                "  Inspect runtime metadata references.\n" +
                "  Usage: refdiag\n");
            break;

        case "utst":
        case "unit_test":
            AppendConsole(
                "utst / unit_test\n" +
                "  Run unit tests.\n" +
                "  Usage: utst\n" +
                "  Usage: unit_test\n");
            break;

        default:
            AppendConsole(
                $"Unknown command: {command}\n" +
                "Use 'help' to list available commands.\n");
            break;
    }
}

    private void ShowHelp()
    {
        AppendConsole(
            "Commands:\n" +
            "  help\n" +
            "  pwd\n" +
            "  space\n" +
            "  space internal\n" +
            "  space external\n" +
            "  ls\n" +
            "  cd <path>\n" +
            "  type <file>\n" +
            "  cat <file>\n" +
            "  build <file>\n" +
            "  git version\n" +
            "  git clone <repository-url>\n" +
            "  refactor <file>\n" +
            "  write <file> <text>\n" +
            "  append <file> <text>\n" +
            "  touch <file>\n" +
            "  mkdir <directory>\n" +
            "  rename <path> <new-name>\n" +
            "  rm <path>\n" +
            "  saf-test\n" +
            "  editor jit-test\n" +
            "  clear\n" +
            "  cls\n" +
            "  head\n" +
            "  tail\n" +
            "  lets go <line>\n" +
            "  page up\n" +
            "  page down\n" +
            "  utst\n" +
            "  unit_test\n");
    }

    private void ExecuteSpaceCommand(
        string[] arguments)
    {
        if (arguments.Length == 0)
        {
            AppendConsole(
                $"Current space: " +
                $"{_fileSystem!.Space}\n");

            return;
        }

        string requested =
            arguments[0].ToLowerInvariant();

        if (requested == "internal")
        {
            _fileSystem!.UseInternal();

            _currentDirectory =
                _fileSystem.GetCurrentRoot();

            _shellSession =
                new ShellSession(
                    _currentDirectory);

            UpdateWorkingDirectoryLabel();

            AppendConsole(
                "Current space: Internal\n");

            return;
        }

        if (requested == "external")
        {
            if (!_fileSystem!.IsExternalConfigured())
            {
                AppendConsole(
                    "External SAF space is not connected.\n");

                return;
            }

            _fileSystem.UseExternal();

            _currentDirectory =
                _fileSystem.GetCurrentRoot();

            UpdateWorkingDirectoryLabel();

            AppendConsole(
                "Current space: External\n");

            return;
        }

        AppendConsole(
            "Usage: space [internal|external]\n");
    }

    private void ExecuteLsCommand(
        string[] arguments)
    {
        if (_fileSystem!.Space ==
            EVIFileSpace.Internal)
        {
            string directory =
                ResolveInternalConsolePath(
                    arguments.Length > 0
                        ? arguments[0]
                        : ".");

            if (!Directory.Exists(
                    directory))
            {
                AppendConsole(
                    $"Directory not found: " +
                    $"{directory}\n");

                return;
            }

            foreach (string dir in
                Directory
                    .GetDirectories(directory)
                    .OrderBy(
                        x => System.IO.Path.GetFileName(x),
                        StringComparer.OrdinalIgnoreCase))
            {
                AppendConsole(
                    $"[DIR]  " +
                    $"{System.IO.Path.GetFileName(dir)}\n");
            }

            foreach (string file in
                Directory
                    .GetFiles(directory)
                    .OrderBy(
                        x => System.IO.Path.GetFileName(x),
                        StringComparer.OrdinalIgnoreCase))
            {
                AppendConsole(
                    $"[FILE] " +
                    $"{System.IO.Path.GetFileName(file)}\n");
            }

            return;
        }

        try
        {
            if (arguments.Length == 0 ||
                arguments[0] == ".")
            {
                var entries =
                    _fileSystem.ListCurrentDirectory();

                foreach (var entry in entries)
                {
                    AppendConsole(
                        entry.IsDirectory
                            ? $"[DIR]  {entry.Name}\n"
                            : $"[FILE] {entry.Name}\n");
                }

                return;
            }

            string directoryId =
                ResolveExternalDirectoryForConsole(
                    arguments[0]);

            var externalEntries =
                _fileSystem.ListExternal(
                    directoryId);

            foreach (var entry in externalEntries)
            {
                AppendConsole(
                    entry.IsDirectory
                        ? $"[DIR]  {entry.Name}\n"
                        : $"[FILE] {entry.Name}\n");
            }
        }
        catch (Exception ex)
        {
            AppendConsole(
                $"{ex.GetType().Name}: " +
                $"{ex.Message}\n");
        }
    }

    private void ExecuteCdCommand(
        string[] arguments)
    {
        if (arguments.Length == 0)
        {
            AppendConsole(
                $"Current directory: " +
                $"{GetPrompt()}\n");

            return;
        }

        string target =
            arguments[0];

        if (_fileSystem!.Space ==
            EVIFileSpace.Internal)
        {
            string directory =
                ResolveInternalConsolePath(
                    target);

            if (!Directory.Exists(
                    directory))
            {
                AppendConsole(
                    $"Directory not found: " +
                    $"{target}\n");

                return;
            }

            _currentDirectory =
                directory;

            _shellSession =
                new ShellSession(
                    _currentDirectory);

            UpdateWorkingDirectoryLabel();

            AppendConsole(
                $"{GetPrompt()}\n");

            return;
        }

        if (_fileSystem.ChangeExternalDirectory(
                target,
                out string message))
        {
            _currentDirectory =
                _fileSystem.GetExternalCurrentDocumentId();

            UpdateWorkingDirectoryLabel();

            AppendConsole(
                $"{GetPrompt()}\n");
        }
        else
        {
            AppendConsole(
                message + "\n");
        }
    }

private void ExecuteTypeCommand(
    string[] arguments)
{
    if (arguments.Length < 1)
    {
        AppendConsole(
            "Usage: type [--tail N] <file>\n");
        return;
    }

    try
    {
        int startIndex = 0;
        int tailLines = -1;

        if (arguments.Length >= 2 &&
            arguments[0].Equals(
                "--tail",
                StringComparison.OrdinalIgnoreCase))
        {
            if (!int.TryParse(
                    arguments[1],
                    out tailLines) ||
                tailLines < 1)
            {
                AppendConsole(
                    "Usage: type [--tail N] <file>\n");
                return;
            }

            startIndex = 2;
        }

        if (arguments.Length <= startIndex)
        {
            AppendConsole(
                "Usage: type [--tail N] <file>\n");
            return;
        }

        string path =
            ResolveInternalConsolePath(
                arguments[startIndex]);

        if (!File.Exists(path))
        {
            AppendConsole(
                $"FileNotFoundException: Could not find file '{path}'.\n");
            return;
        }

        if (tailLines > 0)
        {
            string[] lines =
                File.ReadAllLines(path);

            int firstLine =
                Math.Max(
                    0,
                    lines.Length - tailLines);

            AppendConsole(
                $"Showing lines {firstLine + 1}-{lines.Length} " +
                $"of {lines.Length}\n");

            for (int i = firstLine; i < lines.Length; i++)
            {
                AppendConsole(
                    lines[i] +
                    "\n");
            }

            return;
        }

        string content =
            File.ReadAllText(path);

        AppendConsole(content);

        if (!content.EndsWith("\n"))
            AppendConsole("\n");
    }
    catch (Exception ex)
    {
        AppendConsole(
            $"{ex.GetType().Name}: " +
            $"{ex.Message}\n");
    }
}

    private void ExecuteCatCommand(
    string[] arguments)
{
    if (arguments.Length < 1)
    {
        AppendConsole(
            "Usage: cat <file>\n");
        return;
    }

    try
    {
        string path =
            ResolveInternalConsolePath(
                arguments[0]);

        if (!File.Exists(path))
        {
            AppendConsole(
                $"FileNotFoundException: Could not find file '{path}'.\n");
            return;
        }

        string content =
            File.ReadAllText(path);

        AppendConsole(
            content);

        if (!content.EndsWith("\n"))
            AppendConsole("\n");
    }
    catch (Exception ex)
    {
        AppendConsole(
            $"{ex.GetType().Name}: " +
            $"{ex.Message}\n");
    }
}

private async Task ExecuteBuildCommandAsync(
    string[] arguments)
{
    if (arguments.Length < 1)
    {
        AppendConsole(
            "Usage: build <project-directory>\n");

        return;
    }

    try
    {
        string path =
            arguments[0];

        BuildModule buildModule =
            new();

        AppendConsole(
            $"Building:\n{path}\n\n");

        var result =
            await buildModule.BuildAsync(
                path);

        AppendConsole(
            result.Output);

        AppendConsole(
            $"Exit code: {result.ExitCode}\n");
    }
    catch (Exception ex)
    {
        AppendConsole(
            $"{ex.GetType().Name}: " +
            $"{ex.Message}\n");
    }
}

private async Task ExecuteGitCommandAsync(
    string[] arguments)
{
    if (arguments.Length == 0)
    {
        AppendConsole(
            "Usage:\n" +
            "  git version\n" +
            "  git clone <repository-url>\n");

        return;
    }

    string gitRoot =
        System.IO.Path.Combine(
            StorageManager.RootDirectory,
            "repo");

    var git =
        new GitModule(gitRoot);

    string subcommand =
        arguments[0].ToLowerInvariant();

    switch (subcommand)
    {
        case "version":
        {
            AppendConsole(
                "Checking Git...\n");

            var result =
                await git.VersionAsync();

            AppendConsole(
                result.Output);

            if (!result.Output.EndsWith("\n"))
                AppendConsole("\n");

            AppendConsole(
                $"Exit code: {result.ExitCode}\n");

            return;
        }

        case "clone":
        {
            if (arguments.Length < 2)
            {
                AppendConsole(
                    "Usage: git clone <repository-url>\n");

                return;
            }

            string url =
                arguments[1];

            AppendConsole(
                $"Repository: {url}\n" +
                $"Destination: {gitRoot}\n" +
                "Cloning...\n");

            try
            {
                var result =
                    await git.CloneAsync(url);

                AppendConsole(
                    result.Output);

                if (!result.Output.EndsWith("\n"))
                    AppendConsole("\n");

                AppendConsole(
                    $"Exit code: {result.ExitCode}\n");
            }
            catch (Exception ex)
            {
                Logger.Error(
                    $"Git clone failed: {ex}");

                AppendConsole(
                    $"{ex.GetType().Name}: " +
                    $"{ex.Message}\n");
            }

            return;
        }

        default:
            AppendConsole(
                $"Unknown git command: {subcommand}\n" +
                "Usage:\n" +
                "  git version\n" +
                "  git clone <repository-url>\n");

            return;
    }
}

private void ExecuteRefactorCommand(
    string[] arguments)
{
    if (arguments.Length < 1)
    {
        AppendConsole(
            "Usage: refactor <file-or-directory>\n");
        return;
    }

    try
    {
        if (_fileSystem is null)
        {
            throw new InvalidOperationException(
                "EVI file system is not initialized.");
        }

        var options = RefactorCommandOptions.Parse(arguments);
        string path = options.Path;

        if (_fileSystem.Space == EVIFileSpace.Internal)
        {
            string fullPath =
                ResolveInternalConsolePath(path);

            path = System.IO.Path.GetRelativePath(
                StorageManager.RootDirectory,
                fullPath);
        }

        var sources =
            _fileSystem.ReadCSharpSources(
                path,
                out bool isDirectory);

        IEnumerable<Microsoft.CodeAnalysis.MetadataReference>? references = null;

        if (options.AssemblyNames.Count > 0)
        {
            var directory =
                SafReferenceDirectoryResolver.Resolve(this);

            references = SafMetadataReferenceLoader.Load(
                this,
                directory.TreeUriString,
                directory.DocumentId,
                options.AssemblyNames);
        }

        RefactorEngine engine = new();

        string report =
            engine.AnalyzeSources(
                path,
                sources,
                isDirectory,
                500,
                200,
                10,
                references);

        AppendConsole(report);
        AppendConsole("Refactor exit code: 0\n");
    }
    catch (Exception ex)
    {
        Logger.Error(
            $"Refactor command failed: {ex}");
        AppendConsole(
            $"{ex.GetType().Name}: {ex.Message}\n");
        AppendConsole("Refactor exit code: 1\n");
    }
}

    private void ExecuteWriteCommand(
        string[] arguments)
    {
        if (arguments.Length < 2)
        {
            AppendConsole(
                "Usage: write <file> <text>\n");
            return;
        }

        try
        {
            _fileSystem!.WriteText(
                arguments[0],
                string.Join(
                    " ",
                    arguments.Skip(1)));

            AppendConsole(
                "Write OK.\n");
        }
        catch (Exception ex)
        {
            AppendConsole(
                $"{ex.GetType().Name}: " +
                $"{ex.Message}\n");
        }
    }

    private void ExecuteAppendCommand(
        string[] arguments)
    {
        if (arguments.Length < 2)
        {
            AppendConsole(
                "Usage: append <file> <text>\n");
            return;
        }

        try
        {
            _fileSystem!.AppendText(
                arguments[0],
                string.Join(
                    " ",
                    arguments.Skip(1)));

            AppendConsole(
                "Append OK.\n");
        }
        catch (Exception ex)
        {
            AppendConsole(
                $"{ex.GetType().Name}: " +
                $"{ex.Message}\n");
        }
    }

    private void ExecuteTouchCommand(
        string[] arguments)
    {
        if (arguments.Length < 1)
        {
            AppendConsole(
                "Usage: touch <file>\n");
            return;
        }

        try
        {
            _fileSystem!.CreateFile(
                arguments[0]);

            AppendConsole(
                "Touch OK.\n");
        }
        catch (Exception ex)
        {
            AppendConsole(
                $"{ex.GetType().Name}: " +
                $"{ex.Message}\n");
        }
    }

    private void ExecuteMkdirCommand(
        string[] arguments)
    {
        if (arguments.Length < 1)
        {
            AppendConsole(
                "Usage: mkdir <directory>\n");
            return;
        }

        try
        {
            _fileSystem!.CreateDirectory(
                arguments[0]);

            AppendConsole(
                "Mkdir OK.\n");
        }
        catch (Exception ex)
        {
            AppendConsole(
                $"{ex.GetType().Name}: " +
                $"{ex.Message}\n");
        }
    }

    private void ExecuteRenameCommand(
        string[] arguments)
    {
        if (arguments.Length < 2)
        {
            AppendConsole(
                "Usage: rename <path> <new-name>\n");
            return;
        }

        try
        {
            _fileSystem!.Rename(
                arguments[0],
                arguments[1]);

            AppendConsole(
                "Rename OK.\n");
        }
        catch (Exception ex)
        {
            AppendConsole(
                $"{ex.GetType().Name}: " +
                $"{ex.Message}\n");
        }
    }

    private void ExecuteRemoveCommand(
        string[] arguments)
    {
        if (arguments.Length < 1)
        {
            AppendConsole(
                "Usage: rm <path>\n");
            return;
        }

        try
        {
            _fileSystem!.Delete(
                arguments[0]);

            AppendConsole(
                "Remove OK.\n");
        }
        catch (Exception ex)
        {
            AppendConsole(
                $"{ex.GetType().Name}: " +
                $"{ex.Message}\n");
        }
    }

    private string ResolveInternalConsolePath(
        string path)
    {
        if (string.IsNullOrWhiteSpace(path) ||
            path == ".")
        {
            return _currentDirectory;
        }

        string root =
            System.IO.Path.GetFullPath(
                StorageManager.RootDirectory)
            .TrimEnd(
                System.IO.Path.DirectorySeparatorChar);

        string result;

        if (System.IO.Path.IsPathRooted(path))
        {
            result =
                System.IO.Path.GetFullPath(path);
        }
        else
        {
            result =
                System.IO.Path.GetFullPath(
                    System.IO.Path.Combine(
                        _currentDirectory,
                        path));
        }

        if (!result.Equals(
                root,
                StringComparison.Ordinal) &&
            !result.StartsWith(
                root +
                System.IO.Path.DirectorySeparatorChar,
                StringComparison.Ordinal))
        {
            throw new IOException(
                "Path escapes EVI-ID internal root.");
        }

        return result;
    }

    private string ResolveExternalDirectoryForConsole(
        string path)
    {
        if (string.IsNullOrWhiteSpace(path) ||
            path == ".")
        {
            return _fileSystem!
                .GetExternalCurrentDocumentId();
        }

        string normalized =
            path.Replace('\\', '/');

        if (normalized == "/")
        {
            return _fileSystem!
                .GetExternalRootDocumentId();
        }

        string[] parts =
            normalized
                .TrimStart('/')
                .Split(
                    '/',
                    StringSplitOptions.RemoveEmptyEntries);

        string currentId;

        if (normalized.StartsWith(
                "/",
                StringComparison.Ordinal))
        {
            currentId =
                _fileSystem!
                    .GetExternalRootDocumentId();
        }
        else
        {
            currentId =
                _fileSystem!
                    .GetExternalCurrentDocumentId();
        }

        foreach (string part in parts)
        {
            if (part == ".")
                continue;

            if (part == "..")
            {
                throw new IOException(
                    "Use 'cd ..' to move to the parent directory.");
            }

            var entries =
                _fileSystem!.ListExternal(
                    currentId);

            var entry =
                entries.FirstOrDefault(
                    x =>
                        x.IsDirectory &&
                        string.Equals(
                            x.Name,
                            part,
                            StringComparison.OrdinalIgnoreCase));

            if (entry is null)
            {
                throw new DirectoryNotFoundException(
                    $"External directory not found: {part}");
            }

            currentId =
                entry.DocumentId;
        }

        return currentId;
    }

    private async Task ExecuteExternalCommandAsync(
        string executable,
        string[] arguments)
    {
        try
        {
            string workingDirectory =
                _fileSystem!.Space ==
                    EVIFileSpace.Internal
                    ? _currentDirectory
                    : "";

            var result =
                await CommandRunner.RunAsync(
                    executable,
                    workingDirectory,
                    arguments);

            if (!string.IsNullOrEmpty(
                    result.StandardOutput))
            {
                Logger.Info(
                    $"Command stdout: " +
                    $"{result.StandardOutput}");

                AppendConsole(
                    result.StandardOutput);
            }

            if (!string.IsNullOrEmpty(
                    result.StandardError))
            {
                Logger.Error(
                    $"Command stderr: " +
                    $"{result.StandardError}");

                AppendConsole(
                    result.StandardError);
            }

            Logger.Info(
                $"Command finished; " +
                $"exit code={result.ExitCode}");

            AppendConsole(
                $"Exit code: {result.ExitCode}\n");
        }
        catch (Exception ex)
        {
            Logger.Error(
                $"Command execution failed: {ex}");

            AppendConsole(
                $"{ex.GetType().Name}: " +
                $"{ex.Message}\n" +
                "Exit code: -1\n");
        }
    }

    private string GetPrompt()
    {
        if (_fileSystem!.Space ==
            EVIFileSpace.Internal)
        {
            return _currentDirectory;
        }

        return
            $"external:" +
            $"{_fileSystem.GetExternalCurrentPath()}";
    }

    private void UpdateWorkingDirectoryLabel()
    {
        if (_workingDirectoryLabel is not null)
        {
            _workingDirectoryLabel.Text =
                GetPrompt();
        }
    }

    private void AppendConsole(
        string text)
    {
        _consoleOutput?.Append(
            text);
    }

    private void TestSafRead()
    {
        if (string.IsNullOrWhiteSpace(
                _activeTreeUri) ||
            string.IsNullOrWhiteSpace(
                _activeDocumentId))
        {
            Logger.Info(
                "SAF TEST: no active folder.");

            return;
        }

        try
        {
            var entries =
                SafFileSystem.ListDirectory(
                    this,
                    _activeTreeUri,
                    _activeDocumentId);

            foreach (var entry in entries)
            {
                if (!string.Equals(
                        entry.Name,
                        "EVI-Refactor.csproj",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                Logger.Info(
                    $"SAF TEST: reading " +
                    $"'{entry.Name}'");

                string content =
                    SafFileSystem.ReadTextFile(
                        this,
                        _activeTreeUri,
                        entry.DocumentId);

                Logger.Info(
                    $"SAF TEST: read OK; " +
                    $"chars={content.Length}");

                string firstLine =
                    content
                        .Split('\n')
                        .FirstOrDefault()
                        ?.Trim()
                        ?? "";

                Logger.Info(
                    $"SAF TEST: first line = " +
                    $"{firstLine}");

                return;
            }

            Logger.Info(
                "SAF TEST: EVI-Refactor.csproj " +
                "not found.");
        }
        catch (Exception ex)
        {
            Logger.Error(
                $"SAF TEST failed: {ex}");
        }
    }

    private void OpenProjects()
    {
        ShowPanel(
            "projects");

        if (_activeDocumentId is null ||
            _activeTreeUri is null)
        {
            AppendConsole(
                "Connect an external folder first.\n");
            return;
        }

        RefreshExternalFolderContents();

        AppendConsole(
            "Projects refreshed.\n");
    }

    private void OpenExternalFolder()
    {
        var intent =
            new Intent(
                Intent.ActionOpenDocumentTree);

        intent.AddFlags(
            ActivityFlags.GrantReadUriPermission |
            ActivityFlags.GrantWriteUriPermission |
            ActivityFlags.GrantPersistableUriPermission |
            ActivityFlags.GrantPrefixUriPermission);

        StartActivityForResult(
            intent,
            OpenTreeRequest);
    }

    private void LoadPersistedExternalFolder()
    {
        try
        {
            var folders =
                ExternalFolderManager.GetFolders(
                    this);

            var folder =
                folders.FirstOrDefault();

            if (folder is null)
            {
                Logger.Info(
                    "No persisted external folder.");
                return;
            }

            _activeTreeUri =
                folder.UriString;

            var treeUri =
                Android.Net.Uri.Parse(
                    _activeTreeUri);

            if (treeUri is null)
            {
                Logger.Warning(
                    "Persisted external folder URI is invalid.");
                return;
            }

            if (!ExternalFolderManager.HasPersistedPermission(
                    this,
                    _activeTreeUri))
            {
                Logger.Warning(
                    "Persisted external folder permission " +
                    "is no longer available.");
                return;
            }

            _activeDocumentId =
                DocumentsContract.GetTreeDocumentId(
                    treeUri);

            if (string.IsNullOrWhiteSpace(
                    _activeDocumentId))
            {
                Logger.Warning(
                    "Could not obtain persisted tree DocumentId.");
                return;
            }

            _fileSystem!.ConfigureExternal(
                _activeTreeUri,
                _activeDocumentId);

            Logger.Info(
                "External folder restored: " +
                $"{folder.Name}; " +
                $"DocumentId={_activeDocumentId}");
        }
        catch (Exception ex)
        {
            Logger.Error(
                $"Failed to restore external folder: {ex}");
        }
    }

    protected override void OnActivityResult(
        int requestCode,
        Result resultCode,
        Intent? data)
    {
        base.OnActivityResult(
            requestCode,
            resultCode,
            data);

        if (requestCode !=
            OpenTreeRequest)
        {
            return;
        }

        if (resultCode != Result.Ok ||
            data?.Data is null)
        {
            Logger.Info(
                "External folder selection cancelled.");
            return;
        }

        try
        {
            Android.Net.Uri treeUri =
                data.Data;

            ActivityFlags grantedFlags =
                data.Flags &
                (
                    ActivityFlags.GrantReadUriPermission |
                    ActivityFlags.GrantWriteUriPermission
                );

            ContentResolver!.TakePersistableUriPermission(
                treeUri,
                grantedFlags);

            string treeUriString =
                treeUri.ToString()
                ?? throw new InvalidOperationException(
                    "Selected tree URI is unavailable.");

            string documentId =
                DocumentsContract.GetTreeDocumentId(
                    treeUri)
                ?? throw new InvalidOperationException(
                    "Selected tree DocumentId is unavailable.");

            _activeTreeUri =
                treeUriString;

            _activeDocumentId =
                documentId;

            ExternalFolderManager.AddFolder(
                this,
                treeUri,
                documentId);

            _fileSystem!.ConfigureExternal(
                treeUriString,
                documentId);

            Logger.Info(
                $"External folder connected: " +
                $"{documentId}; " +
                $"URI={treeUriString}");

            RefreshExternalFolderContents();

            AppendConsole(
                $"External folder connected:\n" +
                $"{documentId}\n");
        }
        catch (Exception ex)
        {
            Logger.Error(
                $"Failed to connect external folder: {ex}");

            AppendConsole(
                $"{ex.GetType().Name}: " +
                $"{ex.Message}\n");
        }
    }

    private void OpenDiagnostics()
    {
        Logger.Info(
            "Diagnostics activity opened.");

        var intent =
            new Intent(
                this,
                typeof(DiagnosticsActivity));

        StartActivity(
            intent);
    }
    
    private sealed class RootInsetsListener :
    Java.Lang.Object,
    View.IOnApplyWindowInsetsListener
{
    public WindowInsets? OnApplyWindowInsets(
        View? view,
        WindowInsets? insets)
    {
        if (view is null || insets is null)
            return insets;

        view.SetPadding(
            20 + insets.SystemWindowInsetLeft,
            12 + insets.SystemWindowInsetTop,
            20 + insets.SystemWindowInsetRight,
            8 + insets.SystemWindowInsetBottom);

        return insets;
    }
}
    
}
