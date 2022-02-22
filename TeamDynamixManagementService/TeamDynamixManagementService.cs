using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;

namespace TeamDynamixManagementService
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

    #endregion ---- Service State,Status & Events Messages ----

    #region ---- Service Class ----

    public partial class TeamDynamixManagementService : ServiceBase
    {
        #region ---- Service Private Properties ----

        // List of Service Threads.
        private List<Thread> ServiceThreads = new List<Thread>();

        // Service Status
        private ServiceStatus serviceStatus;

        #endregion ---- Service Private Properties ----

        #region ---- Service Class Constructor

        public TeamDynamixManagementService()
        {
            InitializeComponent();

            // Initialize the service status.
            serviceStatus = new ServiceStatus();
            serviceStatus.dwWaitHint = 100000;

            // Allow the service to be paused and continue.
            this.CanPauseAndContinue = true;
        }

        #endregion ---- Service Class Constructor

        #region ---- Service Control Methods ----

        protected override void OnStart(string[] args)
        {
            // Update the service state to Start Pending.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            // Delay Startup so we can attach the Debugger.
            Thread.Sleep(new TimeSpan(0, 1, 0));

            // Start the worker threads
            foreach (Thread ServiceThread in ServiceThreads)
            {
                ServiceThread.Start();
            }

            // Update the service state to Running.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        protected override void OnPause()
        {
            // Update the service state to pause pending.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_PAUSE_PENDING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            // ========================================================
            // Todo: This is where the on pause code needs to be added.
            // ========================================================

            // Update the service state to paused.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_PAUSED;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        protected override void OnContinue()
        {
            // Update the service state to continue pending.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_CONTINUE_PENDING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            // ========================================================
            // Todo: This is where the continue code needs to be added.
            // ========================================================

            // Update the service state to Running.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        protected override void OnStop()
        {
            // Update the service state to continue pending.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOP_PENDING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            // ========================================================
            // Todo: This is where the stop code needs to be added.
            // ========================================================

            // Update the service state to stopped
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOPPED;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        #endregion ---- Service Control Methods ----

        #region Service Functions

        // Service Status Function.
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(IntPtr handle, ref ServiceStatus serviceStatus);

        #endregion Service Functions

        #endregion ---- Service Class ----
    }
}