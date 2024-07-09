using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace LatestComponentChangedVS
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("Custom Extension", "Updates status bar with the latest component changed in -gitconfig variable", "5.0")]
    [ProvideAutoLoad(UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    [Guid(MyCommandPackage.PackageGuidString)]
    public sealed class MyCommandPackage : AsyncPackage, IVsSolutionEvents
    {
        public const string PackageGuidString = "9430eccd-d8bc-4439-84d5-40a8ddf21079";

        private IVsSolution _solution;
        private uint _solutionEventsCookie;
        private IVsStatusbar _statusBar;
        private System.Threading.Timer _statusUpdateTimer;

        private const string GitConfigPath = @"C:\Users\lennin\.gitconfig"; // Ruta al archivo .gitconfig

        // Método que se ejecuta al inicializar el paquete de forma asíncrona
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // Inicializar el paquete base
            await base.InitializeAsync(cancellationToken, progress);
            await InitializeServicesAsync();

            // Suscribir eventos de solución y comenzar temporizador de actualización de barra de estado
            SubscribeSolutionEvents();
            StartStatusUpdateTimer();
        }

        // Método para inicializar los servicios necesarios
        private async Task InitializeServicesAsync()
        {
            _statusBar = await GetServiceAsync(typeof(SVsStatusbar)) as IVsStatusbar;
            Assumes.Present(_statusBar);

            _solution = await GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
            Assumes.Present(_solution);
        }

        // Método para suscribir los eventos de solución
        private void SubscribeSolutionEvents()
        {
            _solution?.AdviseSolutionEvents(this, out _solutionEventsCookie);
        }

        // Método para iniciar el temporizador que actualizará la barra de estado cada 1 segundo
        private void StartStatusUpdateTimer()
        {
            _statusUpdateTimer = new System.Threading.Timer(UpdateStatusBar, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }

        // Método que se ejecuta cada vez que el temporizador hace tic para actualizar la barra de estado
        private void UpdateStatusBar(object state)
        {
            ThreadHelper.JoinableTaskFactory.Run(async delegate
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                string message = GetGitConfigVariableValue("latest-component-changed");
                SetStatusBarMessage(message);
            });
        }

        // Método para establecer el mensaje en la barra de estado
        private void SetStatusBarMessage(string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _statusBar?.SetText(message);
        }

        // Método para obtener el valor de una variable del archivo .gitconfig
        private string GetGitConfigVariableValue(string variableName)
        {
            try
            {
                if (!File.Exists(GitConfigPath))
                {
                    return "Archivo .gitconfig no encontrado";
                }

                string[] lines = File.ReadAllLines(GitConfigPath);
                foreach (string line in lines)
                {
                    if (line.Contains(variableName))
                    {
                        // Si la variable está vacía, retornar un mensaje
                        if (line.Split('=')[1].Trim() == "")
                        {
                            return "</> No component changed";
                        }
                        else
                        {
                            // Retornar el valor de la variable
                            return "</> " + line.Split('=')[1].Trim();
                        }

                    }
                }

                return "Variable no encontrada";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al leer el archivo .gitconfig: {ex.Message}");
                return "Error";
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
                UnsubscribeSolutionEvents();
            }
            base.Dispose(disposing);
        }

        // Método para desuscribir los eventos de solución: IVsSolutionEvents
        private void UnsubscribeSolutionEvents()
        {
            if (_solution != null && _solutionEventsCookie != 0)
            {
                _solution.UnadviseSolutionEvents(_solutionEventsCookie);
                _solutionEventsCookie = 0;
            }
        }
    }
}
