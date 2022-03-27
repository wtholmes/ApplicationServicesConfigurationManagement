using System;
using System.Data;
using System.Data.Entity.SqlServer;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;

namespace PowerShellRunspaceManager
{
    [Serializable]
    public class AsyncPSCommand
    {
        #region AsyncPSCommand Private Properties

        // Dummy reference to allow this class to be used by callers without requiring them
        // to reference the entity framework.
        private static SqlProviderServices instance = SqlProviderServices.Instance;

        #endregion AsyncPSCommand Private Properties

        #region AsyncPSCommand Public Properties

        public Int64 CommandID { get; set; }

        public String TargetService { get; set; }

        public PowerShellCommand PowerShellCommand { get; set; }

        public DataSet CommandResults { get; set; }

        public String CommandResultsAsJSON { get; set; }

        #endregion AsyncPSCommand Public Properties

        #region AsyncPSCommand Constructor

        public AsyncPSCommand()
        {
            CommandResults = new DataSet("CommandResults");
        }

        #endregion AsyncPSCommand Constructor

        #region AsyncPSCommand Public Methods

        public void GetQueuedCommand()
        {
            using (var context = new PowerShellRunspacesManagerDataModel())
            {
                CommandQueue queuedCommand = queuedCommand = context.CommandQueues
                   .Where(qC => qC.CommandState == "NEW")
                   .FirstOrDefault();

                if (queuedCommand != null)
                {
                    AsyncPSCommand TheQueuedCommmand = Deserilaize(queuedCommand.AsyncCommand);

                    this.CommandID = queuedCommand.CommandID;
                    this.TargetService = queuedCommand.TargetService;
                    this.PowerShellCommand = TheQueuedCommmand.PowerShellCommand;
                    this.CommandResults = TheQueuedCommmand.CommandResults;
                    this.CommandResultsAsJSON = TheQueuedCommmand.CommandResultsAsJSON;

                    queuedCommand.CommandState = "PROCESSING";
                    context.SaveChanges();
                }
            }
        }

        public void SaveCommandResults()
        {
            using (var context = new PowerShellRunspacesManagerDataModel())
            {
                CommandQueue queuedCommand = queuedCommand = context.CommandQueues
                   .Where(qC => qC.CommandID == CommandID)
                   .FirstOrDefault();

                if (queuedCommand != null)
                {
                    queuedCommand.AsyncCommand = Serialize();
                    queuedCommand.CommandState = "READY";
                    queuedCommand.CompletionTime = DateTime.UtcNow;
                    context.SaveChanges();
                }
            }
        }

        public void QueueCommand()
        {
            String CommandState = "NEW";
            using (var context = new PowerShellRunspacesManagerDataModel())
            {
                String z = this.Serialize();

                var AsyncCommand = new CommandQueue
                {
                    TargetService = TargetService,
                    SubmitTime = DateTime.UtcNow,
                    AsyncCommand = this.Serialize(),
                    CommandState = CommandState
                };
                context.CommandQueues.Add(AsyncCommand);
                context.SaveChanges();

                // Get the ID of the command we just saved.
                Int64 ID = AsyncCommand.CommandID;

                // Query the results of the command.
                CommandQueue queuedCommand = queuedCommand = context.CommandQueues
                    .Where(qC => qC.CommandID == ID)
                    .FirstOrDefault();

                //Wait for the results read state to occur.
                while (!CommandState.Equals("READY"))
                {
                    if (queuedCommand.CommandState.Equals("READY"))
                    {
                        CommandState = queuedCommand.CommandState;
                    }
                    else
                    {
                        Thread.Sleep(10);
                        context.Entry(queuedCommand).Reload();
                    }
                }
                // Deserialize the command.
                AsyncPSCommand pq = this.Deserilaize(queuedCommand.AsyncCommand);
                this.CommandID = queuedCommand.CommandID;
                this.TargetService = queuedCommand.TargetService;
                this.CommandResults = pq.CommandResults;
                this.CommandResultsAsJSON = pq.CommandResultsAsJSON;

                queuedCommand.CommandState = "COMPLETE";
                context.SaveChanges();
            }
        }

        public String Serialize()
        {
            XmlSerializer SerializeThis = new XmlSerializer(typeof(AsyncPSCommand));
            String SerializedObject = null;
            using (var ObjectStringWriter = new StringWriter())
            {
                using (XmlWriter writer = XmlWriter.Create(ObjectStringWriter))
                {
                    SerializeThis.Serialize(writer, this);
                    SerializedObject = ObjectStringWriter.ToString();
                }
            }
            return SerializedObject;
        }

        public AsyncPSCommand Deserilaize(String SearlizedObect)
        {
            XmlSerializer DeSerializeThis = new XmlSerializer(typeof(AsyncPSCommand));
            AsyncPSCommand _asyncPSCommand = new AsyncPSCommand();
            using (TextReader reader = new StringReader(SearlizedObect))
            {
                _asyncPSCommand = (AsyncPSCommand)DeSerializeThis.Deserialize(reader);

                this.CommandResults = _asyncPSCommand.CommandResults;
            }
            return _asyncPSCommand;
        }

        #endregion AsyncPSCommand Public Methods
    }
}