using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace ServiceEventLoggingManager
{
    #region ---- Windows Event Logging ----

    public class WindowsEventLogClient
    {
        #region --- Public Properties

        public String EventLogSource { get; private set; }
        public String EventLogName { get; private set; }
        public EventLogEntryType EventLogEntryType { get; set; }
        public Int32 EventID { get; set; }
        public Dictionary<String, String> EventDetails { get; set; }

        #endregion --- Public Properties

        #region --- Private Properties ---

        private EventLog eventLog;

        #endregion --- Private Properties ---

        #region --- Public Constructors ---

        public WindowsEventLogClient(String EventLogSource, String EventLogName)
        {
            this.EventLogSource = EventLogSource;
            this.EventLogName = EventLogName;

            // Check if the specified log exists.

            try
            {
                if (!EventLog.SourceExists(this.EventLogSource))
                {
                    EventLog.CreateEventSource(this.EventLogSource, this.EventLogName);
                }
            }
            catch (Exception exception)
            {
                using (EventLog eventLog = new EventLog("Application"))
                {
                    eventLog.Source = "Application";
                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.AppendFormat("\nUnable to create EventSource");
                    stringBuilder.AppendFormat("\nEvent Source Name: {0}", EventLogSource);
                    stringBuilder.AppendFormat("\nEvent Log Name   : {0}", EventLogName);
                    stringBuilder.AppendFormat("\nThis instance of WindowsEventLogClient will log to the application log.");
                    stringBuilder.AppendFormat("\nTo resolve this error, register the spcified event source.");
                    stringBuilder.AppendFormat("\n----------\n");
                    stringBuilder.AppendFormat("\nException Message: {0}", exception.Message);
                    stringBuilder.AppendFormat("\nException Source : {0}", exception.Source);
                    stringBuilder.AppendFormat("\nInner Exception  : {0}", exception.InnerException);
                    stringBuilder.AppendFormat("\nStack Trace      :\n");
                    stringBuilder.AppendFormat("\n{0}", exception.StackTrace);
                    eventLog.WriteEntry(stringBuilder.ToString(), EventLogEntryType.Error, 50000);
                }

                this.EventLogSource = "Application";
                this.EventLogName = "Application";
            }
            

            if (EventLog.SourceExists(this.EventLogSource))
            {
                this.EventDetails = new Dictionary<String, String>();
                eventLog = new EventLog(this.EventLogName);
                eventLog.Source = this.EventLogSource;
            }
        }

        #endregion --- Public Constructors ---

        /// <summary>
        /// Add to the Event Detail that will be written to the log by WriteEventLogEntry.
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Value"></param>
        public void AddEventDetail(String Name, String Value)
        {
            EventDetails.Add(Name, Value);
        }

        /// <summary>
        /// Writes an event to the log using the given event paramters.
        /// </summary>
        /// <param name="eventLogEntryType"></param>
        /// <param name="eventID"></param>
        /// <param name="EventMessage"></param>
        public void WriteEventLogEntry(EventLogEntryType eventLogEntryType, Int32 eventID, String EventMessage)
        {
            this.EventLogEntryType = eventLogEntryType;
            this.EventID = eventID;
            StringBuilder stringBuilder = new StringBuilder("No Details Available");
            if (EventMessage != null)
            {
                stringBuilder.Clear();
                stringBuilder.AppendFormat("\n{0}\n", EventMessage);
            }
            eventLog.WriteEntry(stringBuilder.ToString(), eventLogEntryType, eventID);
            this.EventDetails.Clear();
        }

        /// <summary>
        /// Writes and event to the log and optionaly include and event details that have been accumlated.
        /// </summary>
        /// <param name="eventLogEntryType"></param>
        /// <param name="eventID"></param>
        /// <param name="EventMessage"></param>
        /// <param name="IncludeEventDetails"></param>
        public void WriteEventLogEntry(EventLogEntryType eventLogEntryType, Int32 eventID, String EventMessage, Boolean IncludeEventDetails)
        {
            this.EventLogEntryType = eventLogEntryType;
            this.EventID = eventID;

            StringBuilder stringBuilder = new StringBuilder("No Details Available");
            if (EventMessage != null)
            {
                stringBuilder.Clear();
                stringBuilder.AppendFormat("\n{0}\n", EventMessage);
            }

            if (this.EventDetails.Count > 0 && IncludeEventDetails)
            {
                stringBuilder.Clear();
                String MessageFormat = "\n{0,-" + (EventDetails.Max(a => a.Key.Length) + 2) + "} : {1}";
                foreach (String Key in EventDetails.Keys)
                {
                    stringBuilder.AppendFormat(MessageFormat, Key, EventDetails[Key]);
                }
            }
            eventLog.WriteEntry(stringBuilder.ToString(), eventLogEntryType, eventID);
            this.EventDetails.Clear();
        }

        public void WriteEventLogEntry(EventLogEntryType eventLogEntryType, Int32 eventID, Exception exception, String EventMessage)
        {
            this.EventLogEntryType = eventLogEntryType;
            this.EventID = eventID;

            switch (this.EventLogEntryType)
            {
                // Modify events in the range of 0-999 to the 4000-4999 range in the entry type is warning.
                case EventLogEntryType.Warning:
                    if (this.EventID < 1000) { this.EventID = this.EventID + 4000; }
                    break;
                // Modify events in the range of 0-999 to the 5999 range in the entry type is error.
                case EventLogEntryType.Error:
                    if (this.EventID < 1000) { this.EventID = this.EventID + 5000; }
                    break;
                // Do not modify events that are outside the 0-999 range.
                default:
                    break;
            }

            StringBuilder stringBuilder = new StringBuilder("No Details Available");
            if(EventMessage != null)
            {
                stringBuilder.Clear();
                stringBuilder.AppendFormat("\n{0}\n", EventMessage);
            }
            if (this.EventDetails.Count > 0)
            {
                stringBuilder.Clear();
                String MessageFormat = "\n{0,-" + (EventDetails.Max(a => a.Key.Length) + 2) + "} : {1}";
                foreach (String Key in EventDetails.Keys)
                {
                    stringBuilder.AppendFormat(MessageFormat, Key, EventDetails[Key]);
                }
            }
            stringBuilder.AppendFormat("\n----------\n");
            stringBuilder.AppendFormat("\nException Message: {0}", exception.Message);
            stringBuilder.AppendFormat("\nException Source : {0}", exception.Source);
            stringBuilder.AppendFormat("\nInner Exception  : {0}", exception.InnerException);
            stringBuilder.AppendFormat("\nStack Trace      :\n");
            stringBuilder.AppendFormat("\n{0}", exception.StackTrace);

            eventLog.WriteEntry(stringBuilder.ToString(), eventLogEntryType, this.EventID);
            this.EventDetails.Clear();
        }

        public void WriteEventLogEntry(EventLogEntryType eventLogEntryType, Int32 eventID, Dictionary<String, String> EventDetails)
        {
            StringBuilder stringBuilder = new StringBuilder();
            String MessageFormat = "\n{0,-" + (EventDetails.Max(a => a.Key.Length) + 2) + "} : {1}";

            foreach (String Key in EventDetails.Keys)
            {
                stringBuilder.AppendFormat(MessageFormat, Key, EventDetails[Key]);
            }

            eventLog.WriteEntry(stringBuilder.ToString(), eventLogEntryType, eventID);
        }

        public void WriteEventLogEntry(EventLogEntryType eventLogEntryType, Int32 eventID, Dictionary<String, String> EventDetails, Exception exception)
        {
            switch (eventLogEntryType)
            {
                // Modify events in the range of 0-999 to the 4000-4999 range in the entry type is warning.

                case EventLogEntryType.Warning:
                    if (eventID < 1000) { eventID = eventID + 4000; }
                    break;
                // Modify events in the range of 0-999 to the 5000-5999 range if the entry type is error.
                case EventLogEntryType.Error:
                    if (eventID < 1000) { eventID = eventID + 5000; }
                    break;
                // Do not modify events that are outside the 0-999 range.
                default:
                    break;
            }
            StringBuilder stringBuilder = new StringBuilder();
            if (EventDetails.Count > 0)
            {
                String MessageFormat = "\n{0,-" + (EventDetails.Max(a => a.Key.Length) + 2) + "} : {1}";
                foreach (String Key in EventDetails.Keys)
                {
                    stringBuilder.AppendFormat(MessageFormat, Key, EventDetails[Key]);
                }
            }
            stringBuilder.AppendFormat("\n----------\n");
            stringBuilder.AppendFormat("\nException Message: {0}", exception.Message);
            stringBuilder.AppendFormat("\nException Source : {0}", exception.Source);
            stringBuilder.AppendFormat("\nInner Exception  : {0}", exception.InnerException);
            stringBuilder.AppendFormat("\nStack Trace      :\n");
            stringBuilder.AppendFormat("\n{0}", exception.StackTrace);
            eventLog.WriteEntry(stringBuilder.ToString(), eventLogEntryType, eventID);
        }
    }

    #endregion ---- Windows Event Logging ----

    #region ---- Syslog Event Logging ----

    public enum SysLogSeverity
    {
        Emergency = 0,
        Alert = 1,
        Critical = 2,
        Error = 3,
        Warning = 4,
        Notice = 5,
        Information = 6,
        Debug = 7,
    }

    public enum SysLogFacility
    {
        Kernel = 0,
        User = 1,
        Mail = 2,
        Daemon = 3,
        Auth = 4,
        Syslog = 5,
        Lpr = 6,
        News = 7,
        UUCP = 8,
        Cron = 9,
        Security = 10,
        FTP = 11,
        NTP = 12,
        Audit = 13,
        Alert = 14,
        Clock = 15,
        Local0 = 16,
        Local1 = 17,
        Local2 = 18,
        Local3 = 19,
        Local4 = 20,
        Local5 = 21,
        Local6 = 22,
        Local7 = 23,
    }

    /// <summary>
    ///     A
    /// </summary>
    public class SysLogMessage
    {
        // [RFC5424] Syslog Message Version
        public Int32 Version
        { get { return 1; } }

        /// <summary>
        ///     Encode the syslog message using the [RFC5424] specification.
        /// </summary>
        public Boolean RFC5424 { get; set; }

        /// <summary>
        ///     The Syslog Facility for this syslog message.
        /// </summary>
        public SysLogFacility SysLogFacility { get; set; }

        /// <summary>
        ///     The Syslog Severity for this syslog message.
        /// </summary>
        public SysLogSeverity SysLogSeverity { get; set; }

        /// <summary>
        ///     The application name for this syslog message. [RFC5424] ignored for [RFC3164].
        /// </summary>
        public String AppName { get; set; }

        /// <summary>
        ///     The process id for this syslog message. [RFC5424] ignored for [RFC3164].
        /// </summary>
        public String ProcID { get; set; }

        /// <summary>
        ///     The message id for this syslog message. [RFC5424] ignored for [RFC3164].
        /// </summary>
        public String MsgID { get; set; }

        /// <summary>
        ///     The structurd data string for this syslog message. [RFC5424] ignored for [RFC3164].
        /// </summary>
        public String StructuredData { get; set; }

        /// <summary>
        ///     The unstructure message text for this syslog message
        /// </summary>
        public String Message { get; set; }

        /// <summary>
        ///     Default Constructor
        /// </summary>
        public SysLogMessage()
        {
            RFC5424 = false;
        }

        /// <summary>
        ///     [RFC3164] Formattted Message Constructor.
        /// </summary>
        /// <param name="sysLogFacility">Syslog Facility</param>
        /// <param name="sysLogSeverity">Syslog Severity</param>
        /// <param name="message">Syslog Message Text</param>
        public SysLogMessage(SysLogFacility sysLogFacility, SysLogSeverity sysLogSeverity, String message)
        {
            RFC5424 = false;
            SysLogFacility = sysLogFacility;
            SysLogSeverity = sysLogSeverity;
            Message = message ?? "-";
        }

        /// <summary>
        ///     [RFC5424] Formatted Message Constructor
        /// </summary>
        /// <param name="sysLogFacility">Syslog Facility</param>
        /// <param name="sysLogSeverity">Syslog Severity</param>
        /// <param name="appName">Application Name</param>
        /// <param name="procID">Process ID</param>
        /// <param name="msgID">Message ID</param>
        /// <param name="structuredData">[RFC5424] Structred Data String</param>
        /// <param name="message">Syslog Message Text</param>
        public SysLogMessage(SysLogFacility sysLogFacility, SysLogSeverity sysLogSeverity, String appName, String procID, String msgID, String structuredData, String message)
        {
            RFC5424 = true;
            SysLogFacility = sysLogFacility;
            SysLogSeverity = sysLogSeverity;
            AppName = appName ?? "-";
            ProcID = procID ?? "-";
            MsgID = msgID ?? "-";
            StructuredData = structuredData ?? "-";
            Message = message ?? "-";
        }
    }

    /// <summary>
    ///     Extended UDP Client Class to Support SyslogClient
    /// </summary>
    public class UdpClientEx : System.Net.Sockets.UdpClient
    {
        public UdpClientEx() : base()
        {
        }

        public UdpClientEx(IPEndPoint ipe) : base(ipe)
        {
        }

        ~UdpClientEx()
        {
            if (this.Active) this.Close();
        }

        public bool IsActive
        {
            get { return this.Active; }
        }
    }

    /// <summary>
    ///     Implements and [RFC3164] and [RFC5424] Syslog Client.
    /// </summary>
    public class SysLogClient : IDisposable
    {
        #region Constructors

        /// <summary>
        ///     Create Syslog Client Instance Using Default Port 514.
        /// </summary>
        /// <param name="server">Syslog Server Name or IP Address</param>
        public SysLogClient(String server)
        {
            Server = server;
            Port = 514;
            InitializeSyslogClient();
        }

        /// <summary>
        ///     Create Syslog Client Instance Specifing a Specific Port.
        /// </summary>
        /// <param name="server">Syslog Server Name or IP Address</param>
        /// <param name="port">Syslog Server Port to use.</param>
        public SysLogClient(String server, Int32 port)
        {
            Server = server;
            Port = port;
            InitializeSyslogClient();
        }

        /// <summary>
        ///     Create a Syslog Client Instance Using a Specific Port and Send an [RFC3164] formatted Syslog Message.
        /// </summary>
        /// <param name="server">Syslog Server Name or IP Address</param>
        /// <param name="port">Syslog Server Port to use.</param>
        /// <param name="sysLogFacility">Syslog Facility (enum  LoggingManager.SysLogFacility)</param>
        /// <param name="sysLogSeverity">Syslog Severity (enum LoggingManager.SyslogSeverity</param>
        /// <param name="MessageText">The Syslog Message Text</param>
        public SysLogClient(String server, Int32 port, SysLogFacility sysLogFacility, SysLogSeverity sysLogSeverity, String MessageText)
        {
            Server = server;
            Port = port;
            InitializeSyslogClient();
            SendSyslogMessage(sysLogFacility, sysLogSeverity, MessageText);
        }

        /// <summary>
        ///     Create a Syslog Client Instance Using a Specific Port and Send an [RFC5424] formatted Syslog Message.
        /// </summary>
        /// <param name="server">Syslog Server Name or IP Address</param>
        /// <param name="port">Syslog Server Port to use.</param>
        /// <param name="sysLogFacility">Syslog Facility (enum  LoggingManager.SysLogFacility)</param>
        /// <param name="sysLogSeverity">Syslog Severity (enum LoggingManager.SyslogSeverity</param>
        /// <param name="StructuredData">Structred Data String formatted in compliance with [RFC5424].</param>
        /// <param name="MessageText">The Syslog Message Text</param>
        public SysLogClient(String server, Int32 port, SysLogFacility sysLogFacility, SysLogSeverity sysLogSeverity, String StructuredData, String MessageText)
        {
            Server = server;
            Port = port;
            InitializeSyslogClient();
            SendSyslogMessage(sysLogFacility, sysLogSeverity, StructuredData, MessageText);
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        ///     Syslog Server Name or IP Address.
        /// </summary>
        public String Server { get; set; }

        /// <summary>
        ///     Syslog Server Port Number
        /// </summary>
        public Int32 Port { get; set; }

        /// <summary>
        ///     Use [RFC5424] Version 1 formatting for the SyslogMessage.
        /// </summary>
        public Boolean RFC5424 { get; set; }

        /// <summary>
        ///     Use IPV6 to connect to the SysLog Server
        /// </summary>
        public Boolean UseIPV6 { get; set; }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        ///     Default Dispose
        /// </summary>
        public void Dispose()
        {
            if (udpClientEx.IsActive) udpClientEx.Close();
        }

        /// <summary>
        ///     Send an [RFC3164] formatted Syslog Message.
        /// </summary>
        /// <param name="sysLogFacility">Syslog Facility (enum  LoggingManager.SysLogFacility)</param>
        /// <param name="sysLogSeverity">Syslog Severity (enum LoggingManager.SyslogSeverity</param>
        /// <param name="MessageText">The Syslog Message Text</param>
        public void SendSyslogMessage(SysLogFacility sysLogFacility, SysLogSeverity sysLogSeverity, String MessageText)
        {
            SysLogMessage sysLogMessage = new SysLogMessage()
            {
                SysLogSeverity = sysLogSeverity,
                SysLogFacility = sysLogFacility,
                Message = MessageText
            };

            SendSyslogMessage(sysLogMessage);
        }

        /// <summary>
        ///     Send an [RFC5424] formatted Syslog Message.
        /// </summary>
        /// <param name="sysLogFacility">Syslog Facility (enum  LoggingManager.SysLogFacility)</param>
        /// <param name="sysLogSeverity">Syslog Severity (enum LoggingManager.SyslogSeverity</param>
        /// <param name="StructuredData">Structred Data String formatted in compliance with [RFC5424].</param>
        /// <param name="MessageText">The Syslog Message Text</param>
        public void SendSyslogMessage(SysLogFacility sysLogFacility, SysLogSeverity sysLogSeverity, String StructuredData, String MessageText)
        {
            SysLogMessage sysLogMessage = new SysLogMessage()
            {
                RFC5424 = true,
                SysLogFacility = sysLogFacility,
                SysLogSeverity = sysLogSeverity,
                StructuredData = StructuredData,
                Message = MessageText
            };

            SendSyslogMessage(sysLogMessage);
        }

        /// <summary>
        ///     Sends a SysLogMessage specified as a SyslogMessage Class
        /// </summary>
        /// <param name="sysLogMessage">An instance of a SyslogMessage Class</param>
        public void SendSyslogMessage(SysLogMessage sysLogMessage)
        {
            if (!udpClientEx.IsActive && syslogServerIPAddress != null)
            {
                udpClientEx.Connect(syslogServerIPAddress, Port);
            }

            try
            {
                if (udpClientEx.IsActive)
                {
                    byte[] encodedSysLogMessage = EncodeSyslogMessage(sysLogMessage);
                    udpClientEx.Send(encodedSysLogMessage, encodedSysLogMessage.Length);
                }
                else
                {
                    throw new Exception("Syslog client Socket is not connected. Please set the SysLogServerIp property");
                }
            }
            catch (Exception exp)
            {
            }
        }

        #endregion Public Methods

        #region Private Properties

        private String hostname;
        private IPEndPoint iPEndPoint;
        private UdpClientEx udpClientEx;
        private IPAddress syslogServerIPAddress;

        #endregion Private Properties

        #region Private Methods

        /// <summary>
        ///     Configures the SyslogClient Class Network Connection to the SyslogServer
        /// </summary>
        private void InitializeSyslogClient()
        {
            // Get the hostname of this client.
            hostname = Dns.GetHostEntry(Dns.GetHostName()).HostName.Trim().ToLower();
            // Get the list of IP Addresses for the specified server.
            List<IPAddress> SyslogServerIPAddresses = Dns.GetHostEntry(Server).AddressList.ToList();

            // Get the list of IP Addresses for this host.
            List<IPAddress> HostIPAddresses = Dns.GetHostEntry(Dns.GetHostName()).AddressList.ToList();

            // Get the list of Default Gateways.
            List<IPAddress> DefaultGatewayAddresses = NetworkInterface.GetAllNetworkInterfaces()
                        .Where(n => n.OperationalStatus == OperationalStatus.Up)
                        .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                        .SelectMany(n => n.GetIPProperties()?.GatewayAddresses)
                        .Select(g => g?.Address)
                        .Where(a => a != null)
                        .ToList();

            foreach (IPAddress iPAddress in HostIPAddresses)
            {
                NetworkInterface networkInterface = NetworkInterface.GetAllNetworkInterfaces()
                            .Where(i => i.OperationalStatus.Equals(OperationalStatus.Up) &&
                            i.GetIPProperties().UnicastAddresses.ToList().Where(a => a.Address.Equals(iPAddress)).FirstOrDefault() != null)
                            .FirstOrDefault();

                if (networkInterface != null)
                {
                    if (networkInterface.GetIPProperties().GatewayAddresses.Count != 0)
                    {
                        if (UseIPV6)
                        {
                            if (iPAddress.AddressFamily.Equals(AddressFamily.InterNetworkV6) && iPAddress.IsIPv6LinkLocal.Equals(false))
                            {
                                if (SyslogServerIPAddresses.Contains(iPAddress))
                                {
                                    syslogServerIPAddress = iPAddress;
                                }
                                else
                                {
                                    syslogServerIPAddress = SyslogServerIPAddresses
                                        .Where(a => a.AddressFamily.Equals(AddressFamily.InterNetworkV6)
                                        && a.IsIPv6LinkLocal.Equals(false)
                                        ).FirstOrDefault();
                                }
                                iPEndPoint = new IPEndPoint(iPAddress, 0);
                                udpClientEx = new UdpClientEx(iPEndPoint);

                                break;
                            }
                        }
                        else
                        {
                            if (iPAddress.AddressFamily.Equals(AddressFamily.InterNetwork))
                            {
                                if (SyslogServerIPAddresses.Contains(iPAddress))
                                {
                                    syslogServerIPAddress = iPAddress;
                                }
                                else
                                {
                                    syslogServerIPAddress = SyslogServerIPAddresses
                                        .Where(a => a.AddressFamily.Equals(AddressFamily.InterNetwork))
                                        .FirstOrDefault();
                                }
                                iPEndPoint = new IPEndPoint(iPAddress, 0);
                                udpClientEx = new UdpClientEx(iPEndPoint);

                                break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Converts a Syslog Message Class into a properly encoded byte array
        /// </summary>
        /// <param name="sysLogMessage">An instance of a SyslogMessage class</param>
        /// <returns> The Encoded SyslogMessage as an array of byte.</returns>
        private byte[] EncodeSyslogMessage(SysLogMessage sysLogMessage)
        {
            // List of encoded bytes for this syslog message.
            List<Byte> encodedSyslogMessage = new List<Byte>();

            // Create an [RFC5424] formatted SyslogMessage
            if (RFC5424)
            {
                // Encode the Message Header in (ASCII) and add it to the SyslogMessage Per: [RFC5234]

                String msgHeader = String.Format("<{0}>{1} {2} {3} {4} {5} {6} ",
                            ((Convert.ToInt32(sysLogMessage.SysLogFacility) * 8) + Convert.ToInt32(sysLogMessage.SysLogSeverity)),
                            sysLogMessage.Version,
                            String.Format("{0}Z", DateTime.UtcNow.ToString("s")),
                            hostname ?? "-",
                            sysLogMessage.ProcID ?? "-",
                            sysLogMessage.AppName ?? "-",
                            sysLogMessage.MsgID ?? "-");

                encodedSyslogMessage.AddRange(System.Text.Encoding.ASCII.GetBytes(msgHeader));
                // Encode the Structured Data Element in (ASCII) and add it to the SyslogMessage Per: [RFC5234]
                String structuredData = sysLogMessage.StructuredData ?? "-";
                encodedSyslogMessage.AddRange(System.Text.Encoding.ASCII.GetBytes(String.Format("{0} ", structuredData)));

                // Encode the Message Element in (UTF8) if it is not Null and add it to the SyslogMessage Per: [RFC5234]
                if (sysLogMessage.Message != null)
                {
                    encodedSyslogMessage.AddRange(System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sysLogMessage.Message)).ToArray());
                }
            }
            // Create an [RFC3164] formatted SysLogMessage
            else
            {
                // Encode the Message Header in (ASCII) and add it to the SyslogMessage Per: [RFC3164]
                int Priority = Convert.ToInt32(sysLogMessage.SysLogFacility) * 8 + Convert.ToInt32(sysLogMessage.SysLogSeverity);
                String msg = String.Format("<{0}>{1} {2} {3}",
                    Priority,
                    DateTime.UtcNow.ToString("MMM dd HH:mm:ss"),
                    hostname ?? "-",
                    sysLogMessage.Message ?? "-");

                encodedSyslogMessage.AddRange(System.Text.Encoding.ASCII.GetBytes(msg));

                //return System.Text.Encoding.ASCII.GetBytes(msg);
            }

            return encodedSyslogMessage.ToArray();
        }

        #endregion Private Methods
    }

    #endregion ---- Syslog Event Logging ----
}