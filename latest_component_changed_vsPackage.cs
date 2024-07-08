using EnvDTE;
using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace VSIXProject1
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("Custom Extension", "Sample extension that updates status bar with latest component changed on -gitconfig global variable", "1.0")]
    [ProvideAutoLoad(UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    [Guid(MyCommandPackage.PackageGuidString)]
    public sealed class MyCommandPackage : AsyncPackage, IVsSolutionEvents
    {
        public const string PackageGuidString = "9430eccd-d8bc-4439-84d5-40a8ddf21079";
        private IVsSolution _solution;
        private uint _solutionEventsCookie;
        private IVsStatusbar _statusBar;
        private System.Threading.Timer _statusUpdateTimer;
        private string _persistentMessage = "Hola Mundo & Hello World";

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);

            // Obtiene el servicio de la barra de estado
            _statusBar = await GetServiceAsync(typeof(SVsStatusbar)) as IVsStatusbar;
            Assumes.Present(_statusBar);

            // Obtiene el servicio de la solución
            _solution = await GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
            Assumes.Present(_solution);

            // Suscribe a los eventos de la solución para saber cuándo se abren o cierran proyectos
            _solution.AdviseSolutionEvents(this, out _solutionEventsCookie);


            // Inicia un temporizador para actualizar la barra de estado cada segundo
            _statusUpdateTimer = new System.Threading.Timer(UpdateStatusBar, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }

        // Implementación de IVsSolutionEvents

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            // Proyecto cargado
            //string message = GetGitConfigVariableValue("latest-component-changed");
            //SetStatusBarMessage(message + " - A");
            return VSConstants.S_OK;
        }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            // Proyecto abierto
            //string message = GetGitConfigVariableValue("latest-component-changed");
            //SetStatusBarMessage(message + " - B");
            return VSConstants.S_OK;
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            // Solución abierta
            //string message = GetGitConfigVariableValue("latest-component-changed");
            //SetStatusBarMessage(message + " - C");
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        private void UpdateStatusBar(object state)
        {
            // Asegúrate de ejecutar el código en el hilo de la interfaz de usuario
            ThreadHelper.JoinableTaskFactory.Run(async delegate
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                string message = GetGitConfigVariableValue("latest-component-changed");
                SetStatusBarMessage(message + " - D");
            });
        }

        private void SetStatusBarMessage(string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_statusBar != null)
            {
                _statusBar.SetText(message);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _statusUpdateTimer?.Dispose();
                if (_solution != null && _solutionEventsCookie != 0)
                {
                    _solution.UnadviseSolutionEvents(_solutionEventsCookie);
                    _solutionEventsCookie = 0;
                }
            }
            base.Dispose(disposing);
        }

        private string GetGitConfigVariableValue(string variableName)
        {
            string gitConfigPath = @"C:\Users\lennin\.gitconfig"; // Ruta al archivo .gitconfig
            try
            {
                string[] lines = File.ReadAllLines(gitConfigPath);
                foreach (string line in lines)
                {
                    if (line.Contains(variableName))
                    {
                        // Obtener el valor de la variable
                        return "</> " + line.Split('=')[1].Trim();
                    }
                }
                // Retornar un valor predeterminado si la variable no se encuentra
                return "Variable no encontrada";
            }
            catch (Exception ex)
            {
                // Manejar cualquier error (por ejemplo, archivo no encontrado, etc.)
                Console.WriteLine($"Error al leer el archivo .gitconfig: {ex.Message}");
                return "Error";
            }
        }
    }

}

