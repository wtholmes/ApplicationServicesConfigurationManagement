using System;
using System.Threading;

namespace ActiveDirectoryManagementService
{
    public partial class ActiveDirectoryManagementService
    {
        /// <summary>
        ///     Rename this thread when implementing a fuctional thread.
        /// </summary>
        public void Template_Thread()
        {
            // Create a new random number generator.
            Random rnd = new Random();

            while (!(serviceStatus.dwCurrentState.Equals(ServiceState.SERVICE_STOPPED) || (serviceStatus.dwCurrentState.Equals(ServiceState.SERVICE_STOP_PENDING))))
            {
                // Wait while the service start is pending.
                while (serviceStatus.dwCurrentState.Equals(ServiceState.SERVICE_START_PENDING))
                {
                    Thread.Sleep(new TimeSpan(0, 0, 5));
                }

                // Log the thread's state when the service goes into a running state.
                if (serviceStatus.dwCurrentState.Equals(ServiceState.SERVICE_RUNNING))
                {
                    // Todo: Add Logging Code Here
                }

                while (serviceStatus.dwCurrentState.Equals(ServiceState.SERVICE_RUNNING))
                {
                    #region Worker Functionality

                    try
                    {
                        // Thread Actions Completed Sleep for One Minute.
                        Thread.Sleep(new TimeSpan(0, 1, 0));
                    }
                    // General Exception
                    catch (Exception exp)
                    {
                        //Todo: Implement General Exception Action,
                    }

                    #endregion Worker Functionality
                }

                // Log the thread state when the service goes into a paused state.
                if (serviceStatus.dwCurrentState.Equals(ServiceState.SERVICE_PAUSED))
                {
                    //Todo: Implement Log Actions.
                }

                // Wait here while the service is paused.
                while (serviceStatus.dwCurrentState.Equals(ServiceState.SERVICE_PAUSED))
                {
                    // Todo: implement the thread's paused state here.
                    Thread.Sleep(new TimeSpan(0, 1, 0));
                }
                // Log the thread state when the service goes into a stop pending state.
                if (serviceStatus.dwCurrentState.Equals(ServiceState.SERVICE_STOP_PENDING))
                {
                    // Todo: Implement Log Actions.
                }

                // Wait here while the service is in stop pending state.
                while (serviceStatus.dwCurrentState.Equals(ServiceState.SERVICE_STOP_PENDING))
                {
                    // Todo: implement the thread's stop state here.
                    Thread.Sleep(new TimeSpan(0, 1, 0));
                }

                // Log the thread state when the service goes into a stopped state.
                if (serviceStatus.dwCurrentState.Equals(ServiceState.SERVICE_STOPPED))
                {
                    // Todo: Implement Log Actions.
                }
            }
        }
    }
}