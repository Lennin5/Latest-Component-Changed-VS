using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace latest_component_changed_vs
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("Latest Component Changed", "Updates status bar with the latest component changed in .gitconfig", "1.0")]
    [Guid(PackageGuidString)]
    public sealed class MyCommandPackage : AsyncPackage
    {
        public const string PackageGuidString = "9430eccd-d8bc-4439-84d5-40a8ddf21079";

        private IVsSolution _solution;
        private uint _solutionEventsCookie;
        private IVsStatusbar _statusBar;
        private System.Threading.Timer _statusUpdateTimer;
        private FileSystemWatcher _fileWatcher;

        // Get GitConfigPath from user home directory
        private static readonly string GitConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
            ".gitconfig");

        // Command ID for our menu command
        public const int CommandId = 0x0100;
        
        // Command set GUID for our command
        public static readonly Guid CommandSet = new Guid("7ACE3E22-6BE1-41E8-9E4B-7F2C7E5DB8E7");
        
        // Command for showing component selector
        private Microsoft.VisualStudio.Shell.OleMenuCommand _selectorCommand;
        
        // Método que se ejecuta al inicializar el paquete de forma asíncrona
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // Inicializar el paquete base
            await base.InitializeAsync(cancellationToken, progress);
            await InitializeServicesAsync();

            // Register our command
            await RegisterCommandAsync();

            // Suscribir eventos de solución y comenzar temporizador de actualización de barra de estado
            await SubscribeSolutionEventsAsync();
            StartStatusUpdateTimer();
            
            // Setup file watcher for .gitconfig
            SetupFileWatcher();
        }

        // Register the command for showing the component selector
        private async Task RegisterCommandAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            
            // Add our command to the command service
            var commandService = await GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                _selectorCommand = new OleMenuCommand(ExecuteCommand, menuCommandID);
                commandService.AddCommand(_selectorCommand);
            }
        }

        // Execute our command when the menu item is clicked
        private void ExecuteCommand(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            // Show the component selector
            ShowComponentSelector();
        }

        // Método para inicializar los servicios necesarios
        private async Task InitializeServicesAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            
            _statusBar = await GetServiceAsync(typeof(SVsStatusbar)) as IVsStatusbar;
            Assumes.Present(_statusBar);

            _solution = await GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
            Assumes.Present(_solution);
        }

        // Método para suscribir los eventos de solución
        private async Task SubscribeSolutionEventsAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            _solution?.AdviseSolutionEvents(this, out _solutionEventsCookie);
        }

        // Setup a file watcher to monitor .gitconfig for changes
        private void SetupFileWatcher()
        {
            try 
            {
                if (File.Exists(GitConfigPath))
                {
                    _fileWatcher = new FileSystemWatcher(
                        Path.GetDirectoryName(GitConfigPath),
                        Path.GetFileName(GitConfigPath));
                    
                    _fileWatcher.Changed += (s, e) =>
                    {
                        // Update status bar when .gitconfig changes - sin usar async delegate
                        try 
                        {
                            // Usamos un método diferente para actualizar la UI
                            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
                            timer.Interval = 100;
                            timer.Tick += (ts, te) =>
                            {
                                timer.Stop();
                                timer.Dispose();
                                
                                // Ejecutamos esto en el hilo de UI usando un enfoque sin async/await
                                if (ThreadHelper.CheckAccess())
                                {
                                    string message = GetLatestComponentChanged();
                                    SetStatusBarMessage(message);
                                }
                                else
                                {
                                    ThreadHelper.Generic.Invoke(() =>
                                    {
                                        string message = GetLatestComponentChanged();
                                        SetStatusBarMessage(message);
                                    });
                                }
                            };
                            timer.Start();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error updating status bar: {ex.Message}");
                        }
                    };
                    
                    _fileWatcher.EnableRaisingEvents = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting up file watcher: {ex.Message}");
            }
        }

        // Método para iniciar el temporizador que actualizará la barra de estado cada 2 segundos
        private void StartStatusUpdateTimer()
        {
            _statusUpdateTimer = new System.Threading.Timer(UpdateStatusBar, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
        }

        // Método que se ejecuta cada vez que el temporizador hace tic para actualizar la barra de estado
        private void UpdateStatusBar(object state)
        {
            try
            {
                // En lugar de usar ThreadHelper.JoinableTaskFactory.Run(async delegate {...}),
                // usamos una alternativa más segura
                if (ThreadHelper.CheckAccess())
                {
                    // Ya estamos en el hilo de UI
                    string message = GetLatestComponentChanged();
                    SetStatusBarMessage(message);
                }
                else
                {
                    // Invocamos en el hilo de UI
                    ThreadHelper.Generic.Invoke(() =>
                    {
                        string message = GetLatestComponentChanged();
                        SetStatusBarMessage(message);
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating status bar: {ex.Message}");
            }
        }

        // Show the component selector dialog
        private void ShowComponentSelector()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            // Get the recent components list
            var components = GetComponentsRecentList();
            
            if (components.Count == 0)
            {
                System.Windows.Forms.MessageBox.Show("No recent components found.", "Component Selector", 
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                return;
            }
            
            // Create and show the component selector form
            using (var form = new ComponentSelectorForm(components, GetLatestComponentChanged()))
            {
                if (form.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(form.SelectedComponent))
                {
                    // Update the latest component in git config
                    SetCurrentComponent(form.SelectedComponent);
                }
            }
        }
        
        // Form class for component selection
        private class ComponentSelectorForm : Form
        {
            private ListBox _listBox;
            private Button _btnOK;
            private Button _btnCancel;
            
            public string SelectedComponent { get; private set; }
            
            public ComponentSelectorForm(List<string> components, string currentComponent)
            {
                Text = "Recent Components";
                Size = new System.Drawing.Size(400, 300);
                StartPosition = FormStartPosition.CenterScreen;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;
                
                // Create the list box
                _listBox = new ListBox
                {
                    Dock = DockStyle.Top,
                    Height = 200,
                    SelectionMode = SelectionMode.One
                };
                
                // Add components to the list
                foreach (var component in components)
                {
                    _listBox.Items.Add(component);
                    
                    // Select the current component
                    if (component == currentComponent)
                    {
                        _listBox.SelectedItem = component;
                    }
                }
                
                // Add double-click handler to select and close
                _listBox.DoubleClick += (s, e) => 
                {
                    if (_listBox.SelectedItem != null)
                    {
                        SelectedComponent = _listBox.SelectedItem.ToString();
                        DialogResult = DialogResult.OK;
                        Close();
                    }
                };
                
                // Create the buttons
                _btnOK = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Location = new System.Drawing.Point(210, 220),
                    Size = new System.Drawing.Size(75, 23)
                };
                
                _btnCancel = new Button
                {
                    Text = "Cancel",
                    DialogResult = DialogResult.Cancel,
                    Location = new System.Drawing.Point(295, 220),
                    Size = new System.Drawing.Size(75, 23)
                };
                
                // Handle OK button click
                _btnOK.Click += (s, e) =>
                {
                    if (_listBox.SelectedItem != null)
                    {
                        SelectedComponent = _listBox.SelectedItem.ToString();
                    }
                };
                
                // Add controls to form
                Controls.Add(_listBox);
                Controls.Add(_btnOK);
                Controls.Add(_btnCancel);
                
                // Set cancel button and accept button
                CancelButton = _btnCancel;
                AcceptButton = _btnOK;
            }
        }

        // Método para establecer el mensaje en la barra de estado
        private void SetStatusBarMessage(string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            if (_statusBar != null)
            {
                // Clear the status bar
                _statusBar.Clear();
                
                // Set the text with icon
                object icon = (short)Microsoft.VisualStudio.Shell.Interop.Constants.SBAI_General;
                _statusBar.SetText(message);
                
                // In Visual Studio, we'll use a separate menu command for the component selector 
                // since the status bar click behavior is different from VS Code
            }
        }
        
        // Get the latest component changed from git config
        private string GetLatestComponentChanged()
        {
            string component = GetGitConfigVariableValue("latest-component-changed");
            return string.IsNullOrEmpty(component) ? "</> No component changed" : "</> " + component;
        }

        // Get the list of recent components from git config
        private List<string> GetComponentsRecentList()
        {
            string componentsString = GetGitConfigVariableValue("latest-components-recent-list", false);
            if (string.IsNullOrEmpty(componentsString))
            {
                return new List<string>();
            }
            
            return componentsString.Split(',')
                .Select(c => c.Trim())
                .Where(c => !string.IsNullOrEmpty(c))
                .ToList();
        }
        
        // Set the current component in git config
        private void SetCurrentComponent(string component)
        {
            if (string.IsNullOrEmpty(component))
            {
                return;
            }
            
            try
            {
                // Run git command to set the current component
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = $"config --global variable.latest-component-changed \"{component}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                process.WaitForExit();
                
                // Update components list if needed
                UpdateComponentsRecentList(component);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting current component: {ex.Message}");
            }
        }
        
        // Update the recent components list in git config
        private void UpdateComponentsRecentList(string component)
        {
            if (string.IsNullOrEmpty(component))
            {
                return;
            }
            
            try
            {
                // Get current list
                var components = GetComponentsRecentList();
                
                // If component already exists, move it to the top
                components.RemoveAll(c => c.Equals(component, StringComparison.OrdinalIgnoreCase));
                
                // Add component to the beginning
                components.Insert(0, component);
                
                // Limit to 5 components
                while (components.Count > 5)
                {
                    components.RemoveAt(components.Count - 1);
                }
                
                // Join components with commas
                string newList = string.Join(",", components);
                
                // Update the git config
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = $"config --global variable.latest-components-recent-list \"{newList}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating components list: {ex.Message}");
            }
        }

        // Método para obtener el valor de una variable del archivo .gitconfig
        private string GetGitConfigVariableValue(string variableName, bool addPrefix = true)
        {
            try
            {
                // Use git command to get the value
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = $"config --get variable.{variableName}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                string output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                
                if (string.IsNullOrEmpty(output))
                {
                    return addPrefix ? "</> No component changed" : string.Empty;
                }
                
                return addPrefix ? "</> " + output : output;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting git config variable: {ex.Message}");
                return addPrefix ? "Error" : string.Empty;
            }
        }

        // Implementación de los eventos de solución de IVsSolutionEvents
        public int OnAfterCloseSolution(object pUnkReserved) => VSConstants.S_OK;
        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy) => VSConstants.S_OK;
        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded) => VSConstants.S_OK;
        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution) => VSConstants.S_OK;
        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved) => VSConstants.S_OK;
        public int OnBeforeCloseSolution(object pUnkReserved) => VSConstants.S_OK;
        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy) => VSConstants.S_OK;
        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel) => VSConstants.S_OK;
        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel) => VSConstants.S_OK;
        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel) => VSConstants.S_OK;

        // Método para liberar recursos: temporizador y eventos de solución
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _statusUpdateTimer?.Dispose();
                _fileWatcher?.Dispose();
                // Usamos un enfoque más seguro sin async delegate
                try
                {
                    if (ThreadHelper.CheckAccess())
                    {
                        UnsubscribeSolutionEvents();
                    }
                    else
                    {
                        ThreadHelper.Generic.Invoke(() =>
                        {
                            UnsubscribeSolutionEvents();
                        });
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error unsubscribing solution events: {ex.Message}");
                }
            }
            base.Dispose(disposing);
        }

        // Método para desuscribir los eventos de solución: IVsSolutionEvents
        private void UnsubscribeSolutionEvents()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_solution != null && _solutionEventsCookie != 0)
            {
                _solution.UnadviseSolutionEvents(_solutionEventsCookie);
                _solutionEventsCookie = 0;
            }
        }
    }
}
