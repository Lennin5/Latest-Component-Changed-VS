using EnvDTE;
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

namespace VSIXProject1
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("Custom Extension", "Updates status bar with the latest component changed in -gitconfig variable", "1.0")]
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

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);
            await InitializeServicesAsync();

            SubscribeSolutionEvents();
            StartStatusUpdateTimer();
        }

        private async Task InitializeServicesAsync()
        {
            _statusBar = await GetServiceAsync(typeof(SVsStatusbar)) as IVsStatusbar;
            Assumes.Present(_statusBar);

            _solution = await GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
            Assumes.Present(_solution);
        }

        private void SubscribeSolutionEvents()
        {
            if (_solution != null)
            {
                _solution.AdviseSolutionEvents(this, out _solutionEventsCookie);
            }
        }

        private void StartStatusUpdateTimer()
        {
            _statusUpdateTimer = new System.Threading.Timer(UpdateStatusBar, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }

        private void UpdateStatusBar(object state)
        {
            ThreadHelper.JoinableTaskFactory.Run(async delegate
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                string message = GetGitConfigVariableValue("latest-component-changed");
                SetStatusBarMessage(message);
            });
        }

        private void SetStatusBarMessage(string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _statusBar?.SetText(message);
        }

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
                        return "</> " + line.Split('=')[1].Trim();
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

        // Implementación de IVsSolutionEvents

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            // No se requiere ninguna acción aquí
            return VSConstants.S_OK;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            //SetStatusBarMessage();
            return VSConstants.S_OK;
        }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            //SetStatusBarMessage();
            return VSConstants.S_OK;
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            //SetStatusBarMessage();
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            // No se requiere ninguna acción aquí
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            // No se requiere ninguna acción aquí
            return VSConstants.S_OK;
        }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            // No se requiere ninguna acción aquí
            return VSConstants.S_OK;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            // No se requiere ninguna acción aquí
            return VSConstants.S_OK;
        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            // No se requiere ninguna acción aquí
            return VSConstants.S_OK;
        }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            // No se requiere ninguna acción aquí
            return VSConstants.S_OK;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _statusUpdateTimer?.Dispose();
                UnsubscribeSolutionEvents();
            }
            base.Dispose(disposing);
        }

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
