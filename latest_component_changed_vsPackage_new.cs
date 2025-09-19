using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace latest_component_changed_vs
{
    // Renderer personalizado para que el menú se vea similar al estilo de VS en modo oscuro
    public class VSMenuRenderer : ToolStripProfessionalRenderer
    {
        public VSMenuRenderer() : base(new VSColorTable()) { }
        
        // Asegurar que el texto del encabezado siempre se renderice correctamente
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            // Si es el elemento encabezado, personalizar el color del texto
            if (e.Item.Tag != null && e.Item.Tag.ToString() == "header")
            {
                // Color de texto rojo para el encabezado
                e.TextColor = Color.FromArgb(240, 71, 71);  // Rojo brillante para resaltar
                base.OnRenderItemText(e);
                return;
            }
            
            // Color de texto para ítems normales en tema oscuro
            if (!e.Item.Selected)
            {
                e.TextColor = Color.FromArgb(220, 220, 220);  // Texto claro para tema oscuro
            }
            else
            {
                e.TextColor = Color.FromArgb(255, 255, 255);  // Blanco para elementos seleccionados
            }
            
            base.OnRenderItemText(e);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            // Si es el encabezado (identificado por Tag o por sus propiedades visuales), siempre usar el renderizado base
            if (e.Item.Tag != null && e.Item.Tag.ToString() == "header")
            {
                base.OnRenderMenuItemBackground(e);
                return;
            }
            
            // Si el elemento no está seleccionado o está deshabilitado, usar renderizado base
            if (!e.Item.Selected || !e.Item.Enabled)
            {
                base.OnRenderMenuItemBackground(e);
                return;
            }

            var rect = new Rectangle(0, 0, e.Item.Width, e.Item.Height);

            // Usar color azul oscuro para selección en tema oscuro
            using (var brush = new SolidBrush(Color.FromArgb(62, 62, 64)))
            {
                e.Graphics.FillRectangle(brush, rect);
            }

            // Dibuja un borde sutil si es necesario, más oscuro para tema oscuro
            using (var pen = new Pen(Color.FromArgb(70, 70, 72)))
            {
                e.Graphics.DrawRectangle(pen, rect);
            }
        }

        // Personalizar el renderizado del fondo del encabezado
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            base.OnRenderToolStripBackground(e);
            
            // Después del renderizado base, dibujar fondos personalizados para elementos específicos
            foreach (ToolStripItem item in e.ToolStrip.Items)
            {
                if (item.Tag != null && item.Tag.ToString() == "header")
                {
                    // Dibujar fondo personalizado para el encabezado en tema oscuro
                    using (var brush = new SolidBrush(Color.FromArgb(45, 45, 48)))
                    {
                        e.Graphics.FillRectangle(brush, item.Bounds);
                    }
                    
                    // Línea inferior sutil para tema oscuro
                    using (var pen = new Pen(Color.FromArgb(70, 70, 72)))
                    {
                        e.Graphics.DrawLine(
                            pen, 
                            item.Bounds.Left, item.Bounds.Bottom - 1,
                            item.Bounds.Right, item.Bounds.Bottom - 1);
                    }
                }
            }
        }

        // Mejora la apariencia del check para tema oscuro
        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            Rectangle rect = new Rectangle(e.ImageRectangle.Left + 2, e.ImageRectangle.Top + 2,
                                          e.ImageRectangle.Width - 4, e.ImageRectangle.Height - 4);

            // Color de fondo para el check en tema oscuro
            using (var brush = new SolidBrush(Color.FromArgb(75, 75, 78)))
            {
                e.Graphics.FillRectangle(brush, rect);
            }

            // Color de la palomita en tema oscuro (más brillante para contraste)
            using (var pen = new Pen(Color.FromArgb(220, 220, 220), 2f))
            {
                // Dibuja un checkmark más similar al de VS en tema oscuro
                int x = rect.Left + 3;
                int y = rect.Top + rect.Height / 2;
                e.Graphics.DrawLine(pen, x, y, x + 2, y + 2);
                e.Graphics.DrawLine(pen, x + 2, y + 2, x + 6, y - 3);
            }
        }
    }

    // Tabla de colores personalizada para el estilo VS en modo oscuro
    public class VSColorTable : ProfessionalColorTable
    {
        // Colores para elementos seleccionados (azul oscuro)
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(51, 51, 52);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(51, 51, 52);
        public override Color MenuItemBorder => Color.FromArgb(51, 51, 52);
        
        // Borde del menú (más oscuro)
        public override Color MenuBorder => Color.FromArgb(60, 60, 60);
        
        // Colores para elementos presionados (azul oscuro)
        public override Color MenuItemPressedGradientBegin => Color.FromArgb(86, 86, 86);
        public override Color MenuItemPressedGradientEnd => Color.FromArgb(86, 86, 86);
        
        // Fondo y márgenes del menú desplegable (modo oscuro)
        public override Color ToolStripDropDownBackground => Color.FromArgb(37, 37, 38);
        public override Color ImageMarginGradientBegin => Color.FromArgb(37, 37, 38);
        public override Color ImageMarginGradientMiddle => Color.FromArgb(37, 37, 38);
        public override Color ImageMarginGradientEnd => Color.FromArgb(37, 37, 38);
        
        // Colores adicionales para consistencia en tema oscuro
        public override Color ButtonSelectedHighlight => Color.FromArgb(86, 86, 86);
        public override Color ButtonSelectedHighlightBorder => Color.FromArgb(86, 86, 86);
        public override Color ButtonPressedHighlight => Color.FromArgb(102, 102, 102);
        public override Color ButtonPressedHighlightBorder => Color.FromArgb(102, 102, 102);
    }
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("Latest Component Changed", "Updates status bar with the latest component changed in .gitconfig", "2.0")]
    [ProvideAutoLoad(UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuidString)]
    public sealed class MyCommandPackage : AsyncPackage
    {
        public const string PackageGuidString = "9430eccd-d8bc-4439-84d5-40a8ddf21079";

        // Command set GUID para nuestro comando
        public static readonly Guid CommandSet = new Guid("7ACE3E22-6BE1-41E8-9E4B-7F2C7E5DB8E7");

        // ID del comando que mostrará el selector de componentes
        public const int CommandId = 0x0100;
        // ID del comando para el ícono en la barra de herramientas
        public const int ToolbarCommandId = 0x0101;
        // ID del comando para el ícono en la barra de estado
        public const int StatusBarCommandId = 0x0102;

        private IVsStatusbar _statusBar;
        private FileSystemWatcher _fileWatcher;
        private System.Windows.Forms.Timer _statusUpdateTimer;
        private OleMenuCommand _selectorCommand;
        private OleMenuCommand _toolbarCommand;

        // Get GitConfigPath from user home directory
        private static readonly string GitConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".gitconfig");

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // Inicializar el paquete base
            await base.InitializeAsync(cancellationToken, progress);
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Get status bar service
            _statusBar = await GetServiceAsync(typeof(SVsStatusbar)) as IVsStatusbar;

            // Registrar el comando para el selector de componentes
            await RegisterCommandAsync();

            // Actualizar la barra de estado inmediatamente
            UpdateStatusBarWithComponent();

            // Configurar un temporizador para actualización periódica muy frecuente (cada 1 segundo)
            StartStatusUpdateTimer();

            // Configurar observador de archivo para .gitconfig
            SetupFileWatcher();

            // Imprimir confirmación de inicialización
            Debug.WriteLine("Latest Component Changed extension initialized successfully");
        }

        // Registrar el comando para mostrar el selector de componentes
        private async Task RegisterCommandAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            // Obtener el servicio de comandos
            var commandService = await GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                // Crear el comando del menú Herramientas
                var menuCommandID = new CommandID(CommandSet, CommandId);
                _selectorCommand = new OleMenuCommand(ExecuteComponentSelectorCommand, menuCommandID);
                commandService.AddCommand(_selectorCommand);

                // Crear el comando de la barra de herramientas con tooltip dinámico
                var toolbarCommandID = new CommandID(CommandSet, ToolbarCommandId);
                _toolbarCommand = new OleMenuCommand(ExecuteToolbarComponentSelectorCommand, toolbarCommandID);

                // Configurar el evento BeforeQueryStatus para actualizar el tooltip
                _toolbarCommand.BeforeQueryStatus += OnBeforeQueryToolbarStatus;

                commandService.AddCommand(_toolbarCommand);

                // El comando de la barra de estado no es necesario por ahora
                // Nos enfocamos en el botón de la barra de herramientas con el ícono </>

                Debug.WriteLine("Component selector commands registered successfully");
            }
        }

        // Actualizar el estado del comando de la barra de herramientas (para tooltip)
        private void OnBeforeQueryToolbarStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (sender is OleMenuCommand command)
            {
                try
                {
                    string currentComponent = GetCurrentComponent();
                    if (string.IsNullOrEmpty(currentComponent))
                    {
                        currentComponent = "No component set";
                    }

                    // Actualizar tanto el texto como el tooltip dinámicamente
                    command.Text = $"</> {currentComponent}";

                    Debug.WriteLine($"Toolbar button updated: </> {currentComponent}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error updating toolbar command status: {ex.Message}");
                }
            }
        }

        // Ejecutar el comando cuando se hace clic en el menú Herramientas
        private void ExecuteComponentSelectorCommand(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            ShowComponentSelector();
            Debug.WriteLine("Menu component selector command executed");
        }

        // Ejecutar el comando cuando se hace clic en el ícono de la barra de herramientas
        private void ExecuteToolbarComponentSelectorCommand(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Para evitar el warning VSTHRD001, utilizamos el JoinableTaskFactory
            _ = JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                await ShowComponentDropdownAsync();
                Debug.WriteLine("Toolbar component selector command executed");
            });
        }



        // Mostrar dropdown rápido de componentes (para el ícono de la barra de herramientas)
        private async Task ShowComponentDropdownAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                // Obtener la lista de componentes recientes
                List<string> components = GetComponentsRecentList();

                // Si no hay componentes, mostrar un mensaje rápido
                if (components.Count == 0)
                {
                    MessageBox.Show(
                        "No hay componentes recientes disponibles.",
                        "Latest Component Changed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                // Crear un menú contextual con estilo similar al de VS
                var contextMenu = new ContextMenuStrip();
                contextMenu.Font = new System.Drawing.Font("Segoe UI", 9F);
                contextMenu.Renderer = new VSMenuRenderer();
                contextMenu.ShowCheckMargin = true;
                contextMenu.ShowImageMargin = true;

                string currentComponent = GetCurrentComponent();

                // Primero agregar un encabezado (similar a "extension vsc (current)" en la captura)
                string headerText = $"Recent Components Changed";
                var headerItem = new ToolStripMenuItem(headerText)
                {
                    Enabled = false,
                    Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold),
                    BackColor = Color.FromArgb(45, 45, 48),  // Color de fondo oscuro para tema VS dark
                    ForeColor = Color.FromArgb(240, 71, 71),  // Color de texto rojo
                    Tag = "header",  // Marcar como encabezado para identificación fácil
                    AutoSize = false,  // Evitar cambios de tamaño automáticos
                    Height = 22  // Altura fija similar a los encabezados de VS
                };
                contextMenu.Items.Add(headerItem);

                // Agregar un separador después del título
                var separator = new ToolStripSeparator();
                separator.Paint += (s, e) =>
                {
                    // Dibujar una línea más sutil, como la que usa VS
                    PaintEventArgs args = e as PaintEventArgs;
                    if (args != null)
                    {
                        ToolStripSeparator item = s as ToolStripSeparator;
                        if (item != null)
                        {
                            args.Graphics.DrawLine(
                                new Pen(Color.FromArgb(70, 70, 72)),  // Línea más oscura para tema oscuro
                                0, item.Height / 2,
                                item.Width, item.Height / 2);
                        }
                    }
                };
                contextMenu.Items.Add(separator);

                foreach (string component in components)
                {
                    // Determinar si es el componente actual y eliminar "(current)" si ya existe
                    string cleanComponent = component.EndsWith(" (current)", StringComparison.OrdinalIgnoreCase)
                        ? component.Substring(0, component.Length - 9)
                        : component;

                    bool isCurrentComponent = string.Equals(cleanComponent, currentComponent, StringComparison.OrdinalIgnoreCase);

                    // Agregar "(current)" al texto del elemento si es el actual
                    string displayText = isCurrentComponent ? $"{cleanComponent} (current)" : cleanComponent;

                    var menuItem = new ToolStripMenuItem(displayText);

                    // Marcar el componente actual
                    if (isCurrentComponent)
                    {
                        menuItem.Checked = true;
                        menuItem.CheckState = CheckState.Checked;
                    }

                    // Agregar el manejador de clic
                    menuItem.Click += async (s, args) =>
                    {
                        // Cambiar al componente seleccionado (sin "(current)")
                        SetCurrentComponent(cleanComponent); // Usamos la versión limpia sin "(current)"

                        // Actualizar la barra de estado en el hilo de UI
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        UpdateStatusBarWithComponent();

                        // Cerrar el menú
                        contextMenu.Close();
                    };

                    contextMenu.Items.Add(menuItem);
                }

                // Mostrar el menú en la posición del cursor
                contextMenu.Show(Cursor.Position);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing component dropdown: {ex.Message}");
                MessageBox.Show(
                    $"Error al mostrar el dropdown de componentes: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // Mostrar el selector de componentes como un formulario (para el menú Herramientas)
        private void ShowComponentSelector()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                // Obtener la lista de componentes recientes
                List<string> components = GetComponentsRecentList();

                // Si no hay componentes, mostrar un mensaje
                if (components.Count == 0)
                {
                    MessageBox.Show(
                        "No hay componentes recientes para seleccionar.\nPuede añadir componentes mediante commits convencionales.",
                        "Latest Component Changed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                // Crear un formulario simple para mostrar los componentes
                using (var form = new ComponentSelectorForm(components, GetCurrentComponent()))
                {
                    if (form.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(form.SelectedComponent))
                    {
                        // Actualizar el componente actual en .gitconfig
                        SetCurrentComponent(form.SelectedComponent);

                        // Actualizar la barra de estado
                        UpdateStatusBarWithComponent();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing component selector: {ex.Message}");
                MessageBox.Show(
                    $"Error al mostrar el selector de componentes: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // Formulario para seleccionar un componente
        private class ComponentSelectorForm : Form
        {
            private ListBox _listBox;
            private Button _btnOK;
            private Button _btnCancel;

            public string SelectedComponent { get; private set; }

            public ComponentSelectorForm(List<string> components, string currentComponent)
            {
                // Configurar el formulario
                Text = "Seleccionar Componente";
                Size = new System.Drawing.Size(400, 300);
                StartPosition = FormStartPosition.CenterScreen;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;

                // Crear el listbox para los componentes
                _listBox = new ListBox
                {
                    Dock = DockStyle.Top,
                    Height = 200,
                    SelectionMode = SelectionMode.One
                };

                // Añadir los componentes al listbox
                foreach (var component in components)
                {
                    // Determinar si es el componente actual y eliminar "(current)" si ya existe
                    string cleanComponent = component.EndsWith(" (current)", StringComparison.OrdinalIgnoreCase)
                        ? component.Substring(0, component.Length - 9)
                        : component;

                    bool isCurrentComponent = string.Equals(cleanComponent, currentComponent, StringComparison.OrdinalIgnoreCase);

                    // Agregar texto "(current)" si es el componente actual
                    string displayText = isCurrentComponent ? $"{cleanComponent} (current)" : cleanComponent;

                    // Agregar al listbox
                    _listBox.Items.Add(displayText);

                    // Seleccionar el componente actual
                    if (isCurrentComponent)
                    {
                        _listBox.SelectedItem = displayText;
                    }
                }

                // Si no hay ningún elemento seleccionado, seleccionar el primero
                if (_listBox.SelectedIndex == -1 && _listBox.Items.Count > 0)
                {
                    _listBox.SelectedIndex = 0;
                }

                // Manejar el doble clic para seleccionar y cerrar
                _listBox.DoubleClick += (s, e) =>
                {
                    if (_listBox.SelectedItem != null)
                    {
                        // Eliminar el sufijo "(current)" si existe
                        string selectedText = _listBox.SelectedItem.ToString();
                        SelectedComponent = selectedText.EndsWith(" (current)", StringComparison.OrdinalIgnoreCase)
                            ? selectedText.Substring(0, selectedText.Length - 9)
                            : selectedText;
                        DialogResult = DialogResult.OK;
                        Close();
                    }
                };

                // Crear los botones
                _btnOK = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Location = new System.Drawing.Point(210, 220),
                    Size = new System.Drawing.Size(75, 23)
                };

                _btnCancel = new Button
                {
                    Text = "Cancelar",
                    DialogResult = DialogResult.Cancel,
                    Location = new System.Drawing.Point(295, 220),
                    Size = new System.Drawing.Size(75, 23)
                };

                // Manejar el clic en OK
                _btnOK.Click += (s, e) =>
                {
                    if (_listBox.SelectedItem != null)
                    {
                        // Eliminar el sufijo "(current)" si existe
                        string selectedText = _listBox.SelectedItem.ToString();
                        SelectedComponent = selectedText.EndsWith(" (current)", StringComparison.OrdinalIgnoreCase)
                            ? selectedText.Substring(0, selectedText.Length - 9)
                            : selectedText;
                    }
                };

                // Añadir los controles al formulario
                Controls.Add(_listBox);
                Controls.Add(_btnOK);
                Controls.Add(_btnCancel);

                // Configurar los botones de aceptar y cancelar
                AcceptButton = _btnOK;
                CancelButton = _btnCancel;
            }
        }

        // Setup a file watcher to monitor .gitconfig for changes
        private void SetupFileWatcher()
        {
            try
            {
                if (File.Exists(GitConfigPath))
                {
                    Debug.WriteLine($"Monitoring .gitconfig at: {GitConfigPath}");

                    // Crear nuevo watcher
                    _fileWatcher = new FileSystemWatcher
                    {
                        Path = Path.GetDirectoryName(GitConfigPath),
                        Filter = Path.GetFileName(GitConfigPath),
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime | NotifyFilters.FileName
                    };

                    // Agregar manejadores para eventos de cambio
                    _fileWatcher.Changed += OnGitConfigChanged;
                    _fileWatcher.Created += OnGitConfigChanged;
                    _fileWatcher.Renamed += OnGitConfigChanged;

                    // Activar el watcher
                    _fileWatcher.EnableRaisingEvents = true;

                    Debug.WriteLine("Git config file watcher activated successfully");
                }
                else
                {
                    Debug.WriteLine($"Warning: .gitconfig file not found at {GitConfigPath}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting up file watcher: {ex.Message}");
            }
        }

        // Manejador para cambios en el archivo .gitconfig
        private void OnGitConfigChanged(object sender, FileSystemEventArgs e)
        {
            Debug.WriteLine($"Git config file changed: {e.ChangeType} - {e.FullPath}");

            // Enfoque simplificado para actualizar la UI
            // Solo ignoramos los errores y permitimos que la actualización periódica del timer maneje esto
            try
            {
                // Ignoramos cualquier error aquí
                Debug.WriteLine($"File change detected. Status bar will update on next timer tick.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error handling file change event: {ex.Message}");
            }
        }

        // Start a timer to update status bar periodically (1 second)
        private void StartStatusUpdateTimer()
        {
            // Usando un timer de Windows Forms que se ejecuta en el hilo de UI
            // Esto evita problemas de sincronización con la UI
            var uiTimer = new System.Windows.Forms.Timer();
            uiTimer.Interval = 1000; // 1 segundo
            uiTimer.Tick += (sender, e) =>
            {
                try
                {
                    // En el evento Tick de Windows.Forms.Timer ya estamos en el hilo de UI
                    UpdateStatusBarWithComponent();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in status update timer: {ex.Message}");
                }
            };
            uiTimer.Start();

            // Guardamos referencia para poder detenerlo después
            _statusUpdateTimer = uiTimer;

            Debug.WriteLine("Status update timer started successfully");
        }

        // Update status bar with latest component from .gitconfig
        private void UpdateStatusBarWithComponent()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                if (_statusBar != null)
                {
                    // Obtener el componente actual
                    string component = GetLatestComponentChanged();

                    // Limpiar cualquier texto existente primero
                    _statusBar.Clear();

                    // Crear el texto con el ícono "</>" al inicio
                    string currentComp = GetCurrentComponent();
                    if (string.IsNullOrEmpty(currentComp))
                    {
                        currentComp = "No component";
                    }

                    string statusText = $"</> {currentComp}";

                    // Establecer el texto en la barra de estado
                    _statusBar.SetText(statusText);

                    // Registro de depuración
                    Debug.WriteLine($"Status bar updated: {statusText}");
                }
                else
                {
                    Debug.WriteLine("Status bar service is null");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating status bar: {ex.Message}");
            }
        }

        // Obtener el componente actual (sin el prefijo "</>")
        private string GetCurrentComponent()
        {
            string fullComponent = GetLatestComponentChanged();
            if (fullComponent.StartsWith("</>"))
            {
                return fullComponent.Substring(4).Trim();
            }
            return string.Empty;
        }

        // Obtener la lista de componentes recientes desde .gitconfig
        private List<string> GetComponentsRecentList()
        {
            List<string> components = new List<string>();
            try
            {
                // Obtener la lista de componentes usando git command
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = "config --get variable.latest-components-recent-list",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();

                // Si hay una lista, procesarla
                if (!string.IsNullOrEmpty(output))
                {
                    // Dividir la lista por comas
                    string[] componentArray = output.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    // Añadir cada componente a la lista
                    foreach (string component in componentArray)
                    {
                        string trimmed = component.Trim();
                        if (!string.IsNullOrEmpty(trimmed))
                        {
                            components.Add(trimmed);
                        }
                    }
                }

                // Si la lista está vacía, añadir el componente actual si existe
                if (components.Count == 0)
                {
                    string current = GetCurrentComponent();
                    if (!string.IsNullOrEmpty(current))
                    {
                        components.Add(current);
                    }
                }

                return components;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting components recent list: {ex.Message}");
                return components;
            }
        }

        // Establecer el componente actual en .gitconfig
        private void SetCurrentComponent(string component)
        {
            if (string.IsNullOrEmpty(component))
            {
                return;
            }

            try
            {
                // Establecer el componente actual
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

                // Actualizar la lista de componentes recientes
                UpdateComponentsRecentList(component);

                Debug.WriteLine($"Current component set to: {component}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting current component: {ex.Message}");
            }
        }

        // Actualizar la lista de componentes recientes
        private void UpdateComponentsRecentList(string component)
        {
            if (string.IsNullOrEmpty(component))
            {
                return;
            }

            try
            {
                // Obtener la lista actual
                List<string> components = GetComponentsRecentList();

                // Eliminar el componente si ya existe
                components.RemoveAll(c => string.Equals(c, component, StringComparison.OrdinalIgnoreCase));

                // Añadir el componente al principio
                components.Insert(0, component);

                // Limitar a 5 componentes
                if (components.Count > 5)
                {
                    components = components.Take(5).ToList();
                }

                // Crear una cadena con los componentes separados por comas
                string componentsString = string.Join(",", components);

                // Actualizar la lista en .gitconfig
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = $"config --global variable.latest-components-recent-list \"{componentsString}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                process.WaitForExit();

                Debug.WriteLine($"Components recent list updated: {componentsString}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating components recent list: {ex.Message}");
            }
        }

        // Get latest component from .gitconfig using git command or direct file reading
        private string GetLatestComponentChanged()
        {
            try
            {
                // Intentar obtener de Git primero (más confiable)
                try
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "git",
                            Arguments = "config --get variable.latest-component-changed",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    };

                    process.Start();
                    string output = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(output))
                    {
                        Debug.WriteLine($"Componente obtenido vía git command: {output}");
                        return "</> " + output;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error al ejecutar git command: {ex.Message}");
                }

                // Como respaldo, intentar leer el archivo directamente
                if (File.Exists(GitConfigPath))
                {
                    try
                    {
                        string[] lines = File.ReadAllLines(GitConfigPath);
                        bool inVariableSection = false;

                        foreach (string line in lines)
                        {
                            string trimmedLine = line.Trim();

                            // Buscar la sección [variable]
                            if (trimmedLine.Equals("[variable]", StringComparison.OrdinalIgnoreCase))
                            {
                                inVariableSection = true;
                                continue;
                            }

                            // Si estamos en la sección correcta, buscar latest-component-changed
                            if (inVariableSection && trimmedLine.StartsWith("latest-component-changed", StringComparison.OrdinalIgnoreCase))
                            {
                                string[] parts = trimmedLine.Split(new[] { '=' }, 2);
                                if (parts.Length == 2)
                                {
                                    string value = parts[1].Trim();
                                    if (!string.IsNullOrEmpty(value))
                                    {
                                        Debug.WriteLine($"Componente obtenido vía archivo: {value}");
                                        return "</> " + value;
                                    }
                                }
                            }

                            // Si llegamos a otra sección, salir del bucle
                            if (inVariableSection && trimmedLine.StartsWith("["))
                            {
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error leyendo .gitconfig: {ex.Message}");
                    }
                }

                // Si no se encontró valor, mostrar mensaje predeterminado
                return "</> No component changed";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error general: {ex.Message}");
                return "Error: " + ex.Message;
            }
        }

        // Cleanup resources when package is unloaded
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _statusUpdateTimer?.Dispose();
                _fileWatcher?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}