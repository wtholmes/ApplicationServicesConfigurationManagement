using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ActiveDirectoryManagementService
{
    #region ---- Service State,Status & Events Messages ----
    public enum ServiceState
    {
        SERVICE_STOPPED = 0x00000001,
        SERVICE_START_PENDING = 0x00000002,
        SERVICE_STOP_PENDING = 0x00000003,
        SERVICE_RUNNING = 0x00000004,
        SERVICE_CONTINUE_PENDING = 0x00000005,
        SERVICE_PAUSE_PENDING = 0x00000006,
        SERVICE_PAUSED = 0x00000007,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ServiceStatus
    {
        public int dwServiceType;
        public ServiceState dwCurrentState;
        public int dwControlsAccepted;
        public int dwWin32ExitCode;
        public int dwServiceSpecificExitCode;
        public int dwCheckPoint;
        public int dwWaitHint;
    };

    #endregion

    #region ---- Service Class ----
    public partial class ActiveDirectoryManagementService : ServiceBase
    {
        // List of Service Threads.
        private List<Thread> ServiceThreads = new List<Thread>();

        // Service Status
        private ServiceStatus serviceStatus;

        #region ---- Service Constructor ----
        public ActiveDirectoryManagementService()
        {
            InitializeComponent();
        }

        #endregion

        #region ---- Service Control Methods ----
        protected override void OnStart(string[] args)
        {
        }

        protected override void OnPause()
        {
            base.OnPause();
        }

        protected override void OnContinue()
        {
            base.OnContinue();
        }

        protected override void OnStop()
        {
        }

        #endregion
    }
    #endregion
}
