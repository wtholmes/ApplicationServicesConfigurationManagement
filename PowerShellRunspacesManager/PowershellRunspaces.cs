using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace PowerShellRunspaceManager
{
    #region ---- Powershell Runspaces Class ----

    /// <summary>
    /// This Generic class permits access to PowerShell RunSpaces.
    /// It is designed to be inherited for application specific PowerShell
    /// environments such as Exchange and Office365.
    ///
    /// Version 2.1
    ///
    /// Copyright © 2010-2022 William T. Holmes All rights reserved
    ///
    /// </summary>
    public class PowershellRunspaces
    {
        #region ---- Protected Properties ----

        protected Runspace _psRunSpace;
        protected Collection<ErrorRecord> _psCommandErrors;
        protected List<Exception> _Exceptions;
        protected List<String> _LoggedMessages;
        protected WSManConnectionInfo connectionInfo;
        protected int SessionCounter;

        protected Stopwatch stopwatch;

        #endregion ---- Protected Properties ----

        #region ---- Class Constructors ----

        /// <summary>
        ///     Default Constructor.
        /// </summary>
        public PowershellRunspaces()
        {
            _psCommandErrors = new Collection<ErrorRecord>();
            _Exceptions = new List<Exception>();
            _LoggedMessages = new List<String>();
            stopwatch = new Stopwatch();
        }

        #endregion ---- Class Constructors ----

        #region ---- Public Properties ----

        /// <summary>
        /// Gets or sets the PowerShell Runspace.
        /// </summary>
        public Runspace psRunSpace
        {
            get
            {
                return _psRunSpace;
            }
            set
            {
                _psRunSpace = value;
            }
        }

        /// <summary>
        ///     Public read only property returns a list of all exceptions that have occurred in this instance of class
        /// </summary>
        /// <value>
        ///     <para>
        ///         Type List<Exception>()
        ///     </para>
        /// </value>
        /// <remarks>
        ///
        /// </remarks>
        public List<Exception> Exceptions
        {
            // Get all exceptions that have occurred in this instance of the class...
            get
            {
                return _Exceptions;
            }
        }

        /// <summary>
        ///     Public read only property returns a list of all exceptions that have occurred in this instance of class
        /// </summary>
        /// <value>
        ///     <para>
        ///         Type List<Exception>()
        ///     </para>
        /// </value>
        /// <remarks>
        ///
        /// </remarks>
        public List<String> LoggedMessages
        {
            // Get all exceptions that have occurred in this instance of the class...
            get
            {
                return _LoggedMessages;
            }
        }

        /// <summary>
        ///     Public read only property returns the last exception that occurred in this instance of the class.
        /// </summary>
        /// <value>
        ///     <para>
        ///         Type Exception.
        ///     </para>
        /// </value>
        /// <remarks>
        ///
        /// </remarks>
        public Exception LastException
        {
            // Get the last exception that occurred in this instance of the class...
            get
            {
                if (Exceptions.Count > 0)
                {
                    Int32 LastExceptionIndex = Exceptions.Count - 1;
                    return Exceptions[LastExceptionIndex];
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets any errors that occurred during a pipeline invoke.
        /// These are not Windows Exceptions but rather PowerShell
        /// Commandlet Errors.
        /// </summary>
        public Collection<ErrorRecord> psCommandErrors
        {
            get
            {
                return _psCommandErrors;
            }
        }

        #endregion ---- Public Properties ----

        #region ---- Public Methods ----

        /// <summary>
        /// This Method Opens a PowerShell runspace for the Class Instance after first
        /// checking its state. Only a single runspace can occur in the class. The method
        /// will re-open the Runspace if it is in a broken state.
        /// </summary>
        public void RunSpaceOpen()
        {
            try
            {
                if (_psRunSpace != null)
                {
                    switch (_psRunSpace.RunspaceStateInfo.State)
                    {
                        case RunspaceState.BeforeOpen:
                            _psRunSpace.Open();
                            break;

                        case RunspaceState.Broken:

                            // Log the broken session state and add an exception to the exception list.
                            _LoggedMessages.Add(String.Format("[{0} UTC]: Powershell Runspace State Is Broken. Clearing and reopening.", DateTime.UtcNow));
                            Exception BrokenSession = new Exception("Powershell Runspace State Is Broken. Clearing and reopening.");
                            BrokenSession.Source = "PowerShell RunSpaces";
                            _Exceptions.Add(BrokenSession);

                            // Dispose of the broken runspace and create a new one.
                            _psRunSpace = null;
                            _psRunSpace = RunspaceFactory.CreateRunspace(connectionInfo);
                            _psRunSpace.Open();
                            break;

                        case RunspaceState.Closed:
                            _psRunSpace.Open();
                            break;

                        default:
                            // Session is Open...
                            break;
                    }
                }
            }
            catch (Exception exp)
            {
                _Exceptions.Add(exp);
            }
        }

        /// <summary>
        /// This method closes the PowerShell runspace if it is currently open.
        /// </summary>
        public void RunSpaceClose()
        {
            if (_psRunSpace != null)
            {
                switch (_psRunSpace.RunspaceStateInfo.State)
                {
                    case RunspaceState.Opened:
                        try
                        {
                            _psRunSpace.Close();
                        }
                        catch (Exception exp)
                        {
                            _Exceptions.Add(exp);
                        }
                        break;

                    case RunspaceState.Broken:
                        _psRunSpace.Close();
                        break;

                    default:
                        // Session is not in an opened state.
                        break;
                }
            }
        }

        /// <summary>
        /// This Method Closes the Runspace if its Open and the Disposes of the runspace.
        /// </summary>
        public void Dispose()
        {
            if (_psRunSpace != null)
            {
                RunSpaceClose();
                _psRunSpace.Dispose();
                _psRunSpace = null;
            }
        }

        /// <summary>
        /// This method clears the exception list.
        /// </summary>
        public void ClearExceptions()
        {
            _Exceptions.Clear();
        }

        /// <summary>
        /// This method clears the Logged Messages list.
        /// </summary>
        public void ClearLoggedMessages()
        {
            _LoggedMessages.Clear();
        }

        /// <summary>
        /// Execute a PowerShell CommandLet in the current RunSpace and return the results.
        /// </summary>
        /// <remarks>
        ///   1. Creates a PowerShell Command Object<br />
        ///   2. Adds the Specified Command Parameters to the Command Object.<br />
        ///   3. Creates a PowerShell Pipeline in the PowerShell RunSpace.<br />
        ///   4. Adds the COmmand Object to the Pipeline<br />
        ///   5. Invokes the Pipeline.<br />
        ///   6. Collects the Returned Power Shell Objects.
        /// </remarks>
        /// <param name="Command">Powershell Commandlet to run</param>
        /// <param name="CommandParameters">Parameters to pass to the PowerShell
        /// Commandlet</param>
        /// <returns>
        /// A Collection of PSObjects
        /// </returns>
        public Collection<PSObject> InvokeCommand(PowerShellCommand powerShellCommand)
        {
            if (_psRunSpace != null)
            {
                Collection<PSObject> psCommandResults = new Collection<PSObject>();

                try
                {
                    Int32 MaxRetry = 5;
                    Int32 RetryCount = 0;

                    while (RetryCount <= MaxRetry)
                    {
                        while (_psRunSpace.RunspaceStateInfo.State != RunspaceState.Opened)
                        {
                            RunSpaceOpen();
                            if (_psRunSpace.RunspaceStateInfo.State != RunspaceState.Opened)
                            {
                                Thread.Sleep(20);
                            }
                        }

                        Command psCommand = new Command(powerShellCommand.command);
                        StringBuilder CommandParamterList = new StringBuilder();
                        foreach (PSCommandParameter<String, Object> CommandParameter in powerShellCommand.commandParameters)
                        {
                            psCommand.Parameters.Add(CommandParameter.PSParameterName, CommandParameter.PSParameterValue);
                            CommandParamterList.Append(String.Format(" -{0} {1}", CommandParameter.PSParameterName, CommandParameter.PSParameterValue));
                        }
                        Pipeline psPipeLine = _psRunSpace.CreatePipeline();
                        psPipeLine.Commands.Add(psCommand);

                        _LoggedMessages.Add(String.Format("[{0} UTC]: Executing PSCommand: {1}{2}", DateTime.UtcNow, powerShellCommand.command, CommandParamterList.ToString()));

                        if (RetryCount == 0)
                        {
                            stopwatch.Reset();
                        }
                        stopwatch.Start();

                        try
                        {
                            psCommandResults = psPipeLine.Invoke();
                            stopwatch.Stop();
                            _LoggedMessages.Add(String.Format("[{0} UTC]: PipeLine Execution Time: {1}ms", DateTime.UtcNow, stopwatch.ElapsedMilliseconds));
                            psPipeLine.Dispose();
                            break;
                        }
                        catch (Exception exp)
                        {
                            // Log the exception.
                            _Exceptions.Add(exp);

                            // Dispose of the pipeline.
                            psPipeLine.Dispose();

                            // Increment the retry counter.
                            RetryCount++;

                            // Log the retry.
                            _LoggedMessages.Add(String.Format("[{0} UTC]: PipeLine Failed Execution after: {1}ms. Retrying Invoke Command {2} more times.", DateTime.UtcNow, stopwatch.ElapsedMilliseconds, (MaxRetry + 1) - RetryCount));
                            _LoggedMessages.Add(String.Format("\n------\nException:\n\n{0}\n------\n", exp));

                            // Wait before retrying
                            Thread.Sleep(new TimeSpan(0, 0, 1));
                        }
                    }

                    if (psCommandResults.Count > 0)
                    {
                        return psCommandResults;
                    }
                    else
                    {
                        psCommandResults.Clear();
                        return psCommandResults;
                    }
                }
                catch (Exception exp)
                {
                    _Exceptions.Add(exp);
                    psCommandResults.Clear();
                    return psCommandResults;
                }
            }
            else
            {
                //Console.ForegroundColor = ConsoleColor.Red;
                //Console.WriteLine("The powershell runspace is null");
                //Console.ForegroundColor = ConsoleColor.White;
                return null;
            }
        }

        public DataSet InvokeCommandResultToDataSet(PowerShellCommand powerShellCommand)
        {
            return PSResultsToDataSet(InvokeCommand(powerShellCommand));
        }

        public String PSResultsToJSONDirect(Collection<PSObject> PSResults)
        {
            return JsonConvert.SerializeObject(PSResults);
        }

        /// <summary>
        /// Returns a JSON formatted string from a Collection of PSObject
        /// </summary>
        /// <param name="PSResults">A collection of PSObject</param>
        /// <returns>JSON formatted String</returns>
        public String PSResultsToJSON(Collection<PSObject> PSResults)
        {
            // Convert the PSResults into a DataSet.
            DataSet PSResultsDataSet = PSResultsToDataSet(PSResults);

            // Create a "flattened" version of the DataSet.
            DataTable arragnedTable = PSResultsDataSet.Tables["Objects"].Clone();

            foreach (DataRow ObjectRow in PSResultsDataSet.Tables["Objects"].Rows)
            {
                DataRow drow = arragnedTable.NewRow();
                foreach (DataColumn dc in PSResultsDataSet.Tables["Objects"].Columns)
                {
                    drow[dc.ColumnName] = ObjectRow[dc.ColumnName];
                }

                foreach (DataRelation dr in PSResultsDataSet.Relations)
                {
                    String ColumnName = dr.ChildTable.TableName;
                    DataRow[] ColumnValues = ObjectRow.GetChildRows(dr);

                    int ccount = ColumnValues.Length;

                    if (dr.ChildTable.Columns.Contains("Key"))
                    {
                        if (!arragnedTable.Columns.Contains(ColumnName))
                        {
                            arragnedTable.Columns.Add(ColumnName, typeof(List<Dictionary<String, String>>));
                        }

                        List<Dictionary<String, String>> values = new List<Dictionary<String, String>>();
                        Dictionary<String, String> KeyValue = new Dictionary<String, String>();
                        foreach (DataRow ColumnValue in ColumnValues)
                        {
                            if (!KeyValue.ContainsKey(ColumnValue["Key"].ToString()))
                            {
                                KeyValue.Add(ColumnValue["Key"].ToString(), ColumnValue["Value"].ToString());
                            }
                            else
                            {
                                values.Add(KeyValue);
                                KeyValue.Clear();
                                KeyValue.Add(ColumnValue["Key"].ToString(), ColumnValue["Value"].ToString());
                            }
                        }
                        values.Add(KeyValue);
                        drow[ColumnName] = values;
                    }
                    else
                    {
                        if (!arragnedTable.Columns.Contains(ColumnName))
                        {
                            arragnedTable.Columns.Add(ColumnName, typeof(List<String>));
                        }
                        List<String> values = new List<String>();
                        foreach (DataRow ColumnValue in ColumnValues)
                        {
                            values.Add(ColumnValue["Value"].ToString());
                        }
                        drow[ColumnName] = values;
                    }
                }

                arragnedTable.Rows.Add(drow);
            }

            return JsonConvert.SerializeObject(arragnedTable);
        }

        /// <summary>
        /// Returns a JSON formatted string from a DataSet
        /// </summary>
        /// <param name="PSResultsDataSet"></param>
        /// <returns>JSON formatted String</returns>
        public String DataSetToJSON(DataSet PSResultsDataSet)
        {
            // Create a "flattened" version of the DataSet.
            DataTable arragnedTable = PSResultsDataSet.Tables["Objects"].Clone();

            foreach (DataRow ObjectRow in PSResultsDataSet.Tables["Objects"].Rows)
            {
                DataRow drow = arragnedTable.NewRow();
                foreach (DataColumn dc in PSResultsDataSet.Tables["Objects"].Columns)
                {
                    drow[dc.ColumnName] = ObjectRow[dc.ColumnName];
                }

                foreach (DataRelation dr in PSResultsDataSet.Relations)
                {
                    String ColumnName = dr.ChildTable.TableName;
                    DataRow[] ColumnValues = ObjectRow.GetChildRows(dr);

                    int ccount = ColumnValues.Length;

                    if (dr.ChildTable.Columns.Contains("Key"))
                    {
                        if (!arragnedTable.Columns.Contains(ColumnName))
                        {
                            arragnedTable.Columns.Add(ColumnName, typeof(List<Dictionary<String, String>>));
                        }

                        List<Dictionary<String, String>> values = new List<Dictionary<String, String>>();
                        Dictionary<String, String> KeyValue = new Dictionary<String, String>();
                        foreach (DataRow ColumnValue in ColumnValues)
                        {
                            if (!KeyValue.ContainsKey(ColumnValue["Key"].ToString()))
                            {
                                KeyValue.Add(ColumnValue["Key"].ToString(), ColumnValue["Value"].ToString());
                            }
                            else
                            {
                                values.Add(KeyValue);
                                KeyValue.Clear();
                                KeyValue.Add(ColumnValue["Key"].ToString(), ColumnValue["Value"].ToString());
                            }
                        }
                        values.Add(KeyValue);
                        drow[ColumnName] = values;
                    }
                    else
                    {
                        if (!arragnedTable.Columns.Contains(ColumnName))
                        {
                            arragnedTable.Columns.Add(ColumnName, typeof(List<String>));
                        }
                        List<String> values = new List<String>();
                        foreach (DataRow ColumnValue in ColumnValues)
                        {
                            values.Add(ColumnValue["Value"].ToString());
                        }
                        drow[ColumnName] = values;
                    }
                }

                arragnedTable.Rows.Add(drow);
            }

            return JsonConvert.SerializeObject(arragnedTable);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="PSResults"></param>
        /// <returns></returns>
        public DataSet PSResultsToDataSet(Collection<PSObject> PSResults)
        {
            DataSet psCommandResults = new DataSet("PSResults");

            // Create the properties table for the scalar properties.
            DataTable propertiesTable = new DataTable("Objects");
            using (DataColumn Id = new DataColumn())
            {
                Id.ColumnName = "Object_Id";
                Id.DataType = typeof(Int32);
                Id.AutoIncrement = true;
                Id.AutoIncrementSeed = 1;
                Id.AutoIncrementStep = 1;
                propertiesTable.Columns.Add(Id);
            }
            psCommandResults.Tables.Add(propertiesTable);

            // Create a the Multivalue properties table.
            DataTable mvPropertiesTable = new DataTable("mvProperties");
            using (DataColumn Id = new DataColumn())
            {
                Id.ColumnName = "mvProperties_Id";
                Id.DataType = typeof(Int32);
                Id.AutoIncrement = true;
                Id.AutoIncrementSeed = 1;
                Id.AutoIncrementStep = 1;
                mvPropertiesTable.Columns.Add(Id);
            }
            mvPropertiesTable.Columns.Add("Object_Id", typeof(Int32));
            mvPropertiesTable.Columns.Add("Key", typeof(String));
            mvPropertiesTable.Columns.Add("Value", typeof(String));

            Int32 PSResultIndex = 0;

            foreach (PSObject PSResult in PSResults)
            {
                PSResultIndex++;
                DataRow PSResultRow = propertiesTable.NewRow();
                psCommandResults.Tables["Objects"].Rows.Add(PSResultRow);

                foreach (Object PSResultProperty in PSResult.Properties)
                {
                    if (PSResultProperty.GetType().Name.Equals("PSProperty"))
                    {
                        PSProperty Property = (PSProperty)PSResultProperty;

                        if (PSResult.Properties[Property.Name].Value != null)
                        {
                            String PropertyName = PSResult.Properties[Property.Name].Name.ToString();
                            Object PropertyValue = PSResult.Properties[Property.Name].Value;
                            Type PropertyValueType = PSResult.Properties[PropertyName].Value.GetType();

                            // Handle Types of PSObject.
                            if (PropertyValueType.Name.Equals("PSObject"))
                            {
                                ArrayList MultiValuePSObject = PSObjectToArrayList((PSObject)PropertyValue);
                                if (MultiValuePSObject.Count > 0)
                                {
                                    // Set the MultiValue Property Table Name.
                                    String PropertyTableName = String.Format("{0}", PropertyName);

                                    // Create a table for this multi-value property if it does not exist.
                                    if (!psCommandResults.Tables.Contains(PropertyTableName))
                                    {
                                        DataTable PropertyTable = mvPropertiesTable.Clone();

                                        PropertyTable.TableName = PropertyTableName;
                                        String IDColumnName = String.Format("{0}_Id", PropertyTableName);
                                        PropertyTable.Columns["mvProperties_Id"].ColumnName = IDColumnName;
                                        psCommandResults.Tables.Add(PropertyTable);

                                        String DataRelationName = String.Format("{0}.Object_Id-to-Objects.Object_Id", PropertyTableName);
                                        DataColumn ParentColumn = psCommandResults.Tables["Objects"].Columns["Object_ID"];
                                        DataColumn ChildColumn = psCommandResults.Tables[PropertyTableName].Columns["Object_ID"];
                                        DataRelation MultiValueRelation = new DataRelation(DataRelationName, ParentColumn, ChildColumn);
                                        MultiValueRelation.Nested = true;
                                        psCommandResults.Relations.Add(MultiValueRelation);

                                        // Remove the Key Column for Simple Lists it is only required for Key/Value Data.
                                        if (MultiValuePSObject[0].GetType().Name.Equals("String"))
                                        {
                                            psCommandResults.Tables[PropertyTableName].Columns.Remove("Key");
                                        }
                                    }

                                    if (MultiValuePSObject[0].GetType().Name.Equals("String"))
                                    {
                                        foreach (String value in MultiValuePSObject)
                                        {
                                            DataRow mvPropertyRow = psCommandResults.Tables[PropertyTableName].NewRow();
                                            mvPropertyRow["Object_Id"] = Convert.ToInt32(PSResultRow["Object_Id"]);
                                            mvPropertyRow["Value"] = value;
                                            psCommandResults.Tables[PropertyTableName].Rows.Add(mvPropertyRow);
                                        }
                                    }
                                    // Handle HashValues...
                                    else if (MultiValuePSObject[0].GetType().Name.Equals("Hashtable"))
                                    {
                                        for (int index = 0; index < MultiValuePSObject.Count; index++)
                                        {
                                            Hashtable ht = (Hashtable)MultiValuePSObject[index];
                                            foreach (String Key in ht.Keys)
                                            {
                                                DataRow mvPropertyRow = psCommandResults.Tables[PropertyTableName].NewRow();
                                                mvPropertyRow["Object_Id"] = Convert.ToInt32(PSResultRow["Object_Id"]);
                                                mvPropertyRow["Key"] = Key;
                                                mvPropertyRow["Value"] = ht[Key];
                                                psCommandResults.Tables[PropertyTableName].Rows.Add(mvPropertyRow);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // This is a unknown object type...
                                    }
                                }
                            }
                            else if (PropertyValueType.Name.Equals("List`1"))
                            {
                                if (PropertyValueType.FullName.Contains("AssignedLicense"))
                                {
                                    /*List<AssignedLicense> assignedLicenses = Property.Value as List<AssignedLicense>;
                                    foreach (AssignedLicense assignedLicense in assignedLicenses)
                                    {
                                    }*/
                                }
                            }
                            else if (PropertyValueType.Name.Equals("Dictionary`2"))
                            {
                                Dictionary<String, String> zz = (Dictionary<String, String>)Property.Value;
                            }
                            else
                            {
                                DataColumnCollection columns = propertiesTable.Columns;
                                if (!(columns.Contains(PropertyName)))
                                {
                                    propertiesTable.Columns.Add(PropertyName, PropertyValueType);
                                }
                                PSResultRow[PropertyName] = PropertyValue;
                            }
                        }
                    }
                    else if (PSResultProperty.GetType().Name.Equals("PSNoteProperty"))
                    {
                        PSNoteProperty Property = (PSNoteProperty)PSResultProperty;
                        if (PSResult.Properties[Property.Name].Value != null)
                        {
                            Object PropertyValue = Property.Value;
                            Type PropertyType = PropertyValue.GetType();

                            DataColumnCollection columns = propertiesTable.Columns;
                            if (!(columns.Contains(Property.Name)))
                            {
                                propertiesTable.Columns.Add(Property.Name, PropertyType);
                            }
                            PSResultRow[Property.Name] = PropertyValue;
                        }
                        else
                        {
                        }
                    }
                    else
                    {
                        // Unhanded Type.
                    }
                }
            }
            return psCommandResults;
        }

        /// <summary>
        /// Convert a Collection&lt;PSObject&gt; and return a new DataTable.
        /// </summary>
        /// <param name="PSResults">A PSResults collection as returned InvokeCommand</param>
        /// <param name="DataTableName">Name of DataTable that will be returned.</param>
        /// <returns>
        /// A DataTable populated with the Properties of each PSObject in the
        /// Collection&lt;PSObject&gt;
        /// </returns>
        public DataTable PSResultsToDataTable(Collection<PSObject> PSResults, String DataTableName)
        {
            DataTable Results = new DataTable(DataTableName);

            // Configure and populate the DataTable....
            // Configure and populate the DataTable....
            foreach (PSObject obj in PSResults)
            {
                DataRow ResultRow = Results.NewRow();
                foreach (PSProperty objProperty in obj.Properties)
                {
                    if (objProperty.Value != null)
                    {
                        Type ValueType = objProperty.Value.GetType();
                        DataColumnCollection columns = Results.Columns;
                        if (!(columns.Contains(objProperty.Name)))
                        {
                            Results.Columns.Add(objProperty.Name, typeof(Object));
                        }
                        ResultRow[objProperty.Name] = obj.Properties[objProperty.Name].Value;
                    }
                }
                Results.Rows.Add(ResultRow);
            }
            return Results;
        }

        /// <summary>
        /// Add or append a Collection&lt;PSObject&gt;to an existing DataTable.
        /// </summary>
        /// <param name="PSResults">A PSResults collection as returned InvokeCommand</param>
        /// <param name="ResultsTable">A DataTable to receive the results.</param>
        /// <param name="AppendResults">Append results to table if true, otherwise clear the
        /// DataTable before adding the results.</param>
        public void PSResultsToDataTable(Collection<PSObject> PSResults, DataTable ResultsTable, Boolean AppendResults)
        {
            // If not appending results then clear the DataTable...
            if (!AppendResults)
            {
                ResultsTable.Clear();
                ResultsTable.Columns.Clear();
            }

            // Configure and populate the DataTable....
            foreach (PSObject obj in PSResults)
            {
                DataRow ResultRow = ResultsTable.NewRow();
                foreach (PSProperty objProperty in obj.Properties)
                {
                    if (objProperty.Value != null)
                    {
                        Type ValueType = objProperty.Value.GetType();

                        DataColumnCollection columns = ResultsTable.Columns;
                        if (!(columns.Contains(objProperty.Name)))
                        {
                            ResultsTable.Columns.Add(objProperty.Name, typeof(Object));
                        }

                        // Check that we have a match between the expected Column Data Type and the DataType.
                        // Otherwise do not add a value for the row.
                        Type ColumnType = ResultsTable.Columns[objProperty.Name].GetType();
                        if (true)
                        {
                            ResultRow[objProperty.Name] = obj.Properties[objProperty.Name].Value;
                        }
                    }
                }
                ResultsTable.Rows.Add(ResultRow);
            }
        }

        /// <summary>
        /// Convert a Collection&lt;PSObject&gt; and return a new DataTable that has complex data types converted to serialized primitive types.
        /// </summary>
        /// <param name="PSResults">A PSResults collection as returned InvokeCommand</param>
        /// <param name="DataTableName">Name of DataTable that will be returned.</param>
        /// <returns>
        /// A DataTable populated with the Properties of each PSObject in the
        /// Collection&lt;PSObject&gt;
        /// </returns>
        public DataTable PSResultsToDataTableSearialized(Collection<PSObject> PSResults, String DataTableName)
        {
            DataTable Results = new DataTable(DataTableName);

            // Configure and populate the DataTable....
            foreach (PSObject Mailbox in PSResults)
            {
                DataRow ResultRow = Results.NewRow();
                foreach (PSProperty MailboxProperty in Mailbox.Properties)
                {
                    Object MailboxPropertyValue = null;
                    if (Mailbox.Properties[MailboxProperty.Name].Value != null)
                    {
                        Type ValueType = Mailbox.Properties[MailboxProperty.Name].Value.GetType();
                        if (ValueType.Name.Equals("PSObject"))
                        {
                            ArrayList MultiValuePSObject = PSObjectToArrayList((PSObject)MailboxProperty.Value);

                            if (MultiValuePSObject.Count > 0)
                            {
                                if (MultiValuePSObject[0].GetType().Name.Equals("String"))
                                {
                                    String[] MultiValue = new String[MultiValuePSObject.Count];
                                    for (int index = 0; index < MultiValuePSObject.Count; index++)
                                    {
                                        MultiValue[index] = Convert.ToString(MultiValuePSObject[index]);
                                    }
                                    MailboxPropertyValue = MultiValue;
                                }
                                else if (MultiValuePSObject[0].GetType().Name.Equals("Hashtable"))
                                {
                                    SerializableDictionary<String, String>[] MultiValue = new SerializableDictionary<String, String>[MultiValuePSObject.Count];
                                    for (int index = 0; index < MultiValuePSObject.Count; index++)
                                    {
                                        Hashtable ht = (Hashtable)MultiValuePSObject[0];
                                        SerializableDictionary<String, String> dict = new SerializableDictionary<string, string>();
                                        foreach (String Key in ht.Keys)
                                        {
                                            dict.Add(Convert.ToString(Key), Convert.ToString(ht[Key]));
                                        }
                                        MultiValue[index] = dict;
                                    }
                                    MailboxPropertyValue = MultiValue;
                                }
                                else
                                {
                                    MailboxPropertyValue = "";
                                }
                            }
                            else
                            {
                                MailboxPropertyValue = "";
                            }
                        }
                        else
                        {
                            MailboxPropertyValue = MailboxProperty.Value;
                        }
                    }
                    else
                    {
                        MailboxPropertyValue = "";
                    }

                    Type ColumnType = MailboxPropertyValue.GetType();

                    DataColumnCollection columns = Results.Columns;
                    if (!(columns.Contains(MailboxProperty.Name)))
                    {
                        Results.Columns.Add(MailboxProperty.Name, ColumnType);
                    }
                    ResultRow[MailboxProperty.Name] = MailboxPropertyValue;
                }
                Results.Rows.Add(ResultRow);
            }
            return Results;
        }

        /// <summary>
        /// Remove the List of DataColumns from the given DataTable
        /// </summary>
        /// <param name="dt">DataTable</param>
        /// <param name="ColumnsToRemove">List of columns to remove</param>
        public void RemoveDataTableColumns(DataTable dt, List<String> ColumnsToRemove)
        {
            string[] ColumnNames = dt.Columns.Cast<DataColumn>()
                         .Select(x => x.ColumnName)
                         .ToArray();

            foreach (String ColumnName in ColumnNames)
            {
                if (ColumnsToRemove.Contains(ColumnName, StringComparer.OrdinalIgnoreCase))
                {
                    dt.Columns.Remove(ColumnName);
                }
            }
        }

        /// <summary>
        /// Keep the list of DataColumns in a DataTable and remove all others.
        /// </summary>
        /// <param name="dt">DataTable</param>
        /// <param name="ColumnsToKeep">List of Columns to Keep</param>
        public void KeepDataTableColumns(DataTable dt, List<String> ColumnsToKeep)
        {
            string[] ColumnNames = dt.Columns.Cast<DataColumn>()
                         .Select(x => x.ColumnName)
                         .ToArray();

            foreach (String ColumnName in ColumnNames)
            {
                if (!ColumnsToKeep.Contains(ColumnName, StringComparer.OrdinalIgnoreCase))
                {
                    dt.Columns.Remove(ColumnName);
                }
            }
        }

        /// <summary>
        /// Convert a Collection<PSObject> and return a XML formatted String representing the data.
        /// </summary>
        /// <param name="PSResults"></param>
        /// <returns></returns>

        public String PSResultsToXMLString(Collection<PSObject> PSResults)
        {
            DataTable dt = new DataTable("Results");
            PSResultsToDataTable(PSResults, dt, false);
            foreach (DataRow dr in dt.Rows)
            {
                foreach (DataColumn dc in dt.Columns)
                {
                    Object ColumnValue = dr[dc.ColumnName];
                    String DataType = ColumnValue.GetType().Name;

                    if (ColumnValue.GetType().Name.Equals("PSObject"))
                    {
                        ArrayList psObjectValues = PSObjectToArrayList((PSObject)ColumnValue);
                        if (psObjectValues.Count > 0)
                        {
                            if (psObjectValues[0].GetType().Name.Equals("String"))
                            {
                                List<String> elements = new List<String>();
                                foreach (Object psObjectValue in psObjectValues)
                                {
                                    elements.Add(psObjectValue.ToString());
                                }
                                XElement xmlElements = new XElement("Values", elements.Select(val => new XElement("Value", val)));
                                dr[dc.ColumnName] = xmlElements;
                            }
                            else if (psObjectValues[0].GetType().Name.Equals("Hashtable"))
                            {
                                List<XElement> elements = new List<XElement>();
                                foreach (Object psObjectValue in psObjectValues)
                                {
                                    Dictionary<String, String> element = new Dictionary<String, String>();
                                    Hashtable ht = (Hashtable)psObjectValue;
                                    element = ht.Cast<DictionaryEntry>().ToDictionary(de => de.Key.ToString(), de => de.Value.ToString());
                                    XElement xElem = new XElement("Properties", element.Select(kv => new XElement(kv.Key, kv.Value)));
                                    elements.Add(xElem);
                                }
                                XElement xmlElements = new XElement("Values", elements.Select(val => new XElement("Value", val)));
                                dr[dc.ColumnName] = xmlElements;
                            }
                        }
                        else
                        {
                            // There are no elements in the PSObject.
                            // Set the column value to an empty string.
                            dr[dc.ColumnName] = "";
                        }
                    }
                    else if (ColumnValue.GetType().Name.Equals("DBNull"))
                    {
                        dr[dc.ColumnName] = "";
                    }
                }

                using (StringWriter sw = new StringWriter())
                {
                    dt.WriteXml(sw, XmlWriteMode.WriteSchema);
                    return sw.ToString();
                }
            }
            return "";
        }

        /// <summary>
        /// Converts a Collection&lt;PsObject&gt; to a JSON String.
        /// </summary>
        /// <param name="PSResults">A PSResults collection as returned by
        /// InvokeCommand</param>
        /// <returns>
        /// A JSON formatted string containing the properties and Values of the PSResults
        /// collection.
        /// </returns>
        public String PSResultsToJSONString(Collection<PSObject> PSResults)
        {
            DataTable dt = new DataTable("Results");
            PSResultsToDataTable(PSResults, dt, false);

            List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();
            Dictionary<string, object> row;
            foreach (DataRow dr in dt.Rows)
            {
                row = new Dictionary<string, object>();
                foreach (DataColumn col in dt.Columns)
                {
                    object dataValue = dr[col];
                    String DataType = dataValue.GetType().Name;
                    List<String> ExcludedTypes = new List<String>(new String[] { "PSObject", "DBNull" });

                    if (!(ExcludedTypes.Contains(dataValue.GetType().Name)))
                    {
                        row.Add(col.ColumnName, dataValue);
                    }
                    else
                    {
                        if (DataType.Equals("PSObject"))
                        {
                            //Cast the value to a PSObject...
                            PSObject psObj = (PSObject)dataValue;

                            var CollectionMembers = PSObjectToArrayList(psObj);
                            row.Add(col.ColumnName, CollectionMembers);
                        }
                    }
                }
                rows.Add(row);
            }

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            String JSONString = serializer.Serialize(rows);
            return JSONString;
        }

        /// <summary>
        /// Convert a PSObject to an ArrayList so that it may serialized...
        /// </summary>
        /// <remarks>
        /// This method reduces a complex PowerShell object to its primitives. The resulting
        /// ArrayList is compatible with serialization for both XML and JSON serializers.
        /// </remarks>
        /// <param name="psObj">The PowerShell object to be converted</param>
        /// <returns>
        /// An array list containing the converted PSObject
        /// </returns>
        public ArrayList PSObjectToArrayList(PSObject psObj)
        {
            ArrayList psObjArrayList = new ArrayList();

            if (psObj.ImmediateBaseObject.GetType().Name.Equals("ArrayList"))
            {
                ArrayList CollectionMembers = (ArrayList)psObj.ImmediateBaseObject;
                foreach (Object CollectionMember in CollectionMembers)
                {
                    if (CollectionMember.GetType().Name.Equals("PSObject"))
                    {
                        // If the member is also a PSObject that recurse,,,
                        PSObject _psObj = (PSObject)CollectionMember;
                        if (_psObj.ImmediateBaseObject.GetType().Name.Equals("PSObject"))
                        {
                            psObjArrayList.Add(PSObjectToArrayList(_psObj));
                        }
                        else if (_psObj.ImmediateBaseObject.GetType().Name.Equals("PSCustomObject"))
                        {
                            // Convert PSCustomObject to HashTable...
                            Hashtable PropertyValue = new Hashtable();
                            foreach (PSProperty Property in _psObj.Properties)
                            {
                                PropertyValue.Add(Property.Name, Property.Value);
                            }
                            // Add the HashTable to the Arraylist...
                            psObjArrayList.Add(PropertyValue);
                        }
                    }
                    else
                    {
                        // Just add the members to the ArrayList...
                        psObjArrayList.Add(CollectionMember);
                    }
                }
            }
            return psObjArrayList;
        }
    }

    #endregion ---- Public Methods ----

    #endregion ---- Powershell Runspaces Class ----

    #region ---- PowerShell Command Class ----

    [Serializable]
    [XmlType(TypeName = "PowerShellCommand")]
    public class PowerShellCommand
    {
        #region PowerShellCommand Private Properties

        private String _command;

        #endregion PowerShellCommand Private Properties

        #region PowerShellCommand Public Properties

        public String command
        {
            get { return _command; }
            set { _command = value; }
        }

        public List<PSCommandParameter<String, Object>> commandParameters { get; set; }

        #endregion PowerShellCommand Public Properties

        #region PowerShellCommand Constructors

        public PowerShellCommand()
        {
            _command = null;
            commandParameters = new List<PSCommandParameter<String, Object>>();
        }

        public PowerShellCommand(String command)
        {
            _command = command;
            commandParameters = new List<PSCommandParameter<String, Object>>();
        }

        public PowerShellCommand(String command, List<PSCommandParameter<String, Object>> commandParameters)
        {
            _command = command;
            this.commandParameters = commandParameters;
        }

        public PowerShellCommand(String command, String parameterName, Object parameterValue)
        {
            _command = command;
            commandParameters = new List<PSCommandParameter<String, Object>>();
            PSCommandParameter<String, Object> commandParam = new PSCommandParameter<String, Object>();
            commandParam.PSParameterName = parameterName;
            commandParam.PSParameterValue = parameterValue;
            commandParameters.Add(commandParam);
        }

        #endregion PowerShellCommand Constructors

        #region PowerShellCommand Public Methods

        public void AddCommandParameter(String ParameterName, Object ParameterValue)
        {
            PSCommandParameter<String, Object> commandParam = new PSCommandParameter<String, Object>();
            commandParam.PSParameterName = ParameterName;
            commandParam.PSParameterValue = ParameterValue;
            commandParameters.Add(commandParam);
        }

        #endregion PowerShellCommand Public Methods
    }

    #endregion ---- PowerShell Command Class ----

    #region ---- PowerShellRunspaceManager Public NameSpace Structs and Classes ----

    /// <summary>
    /// This Struct is used to pass command parameters to powershell commands.
    /// </summary>
    /// <typeparam name="PSparameterName"></typeparam>
    /// <typeparam name="PSparameterValue"></typeparam>
    [Serializable]
    [XmlType(TypeName = "PSCommandParamter")]
    public struct PSCommandParameter<PSparameterName, PSparameterValue>
    {
        public PSparameterName PSParameterName { get; set; }

        public PSparameterValue PSParameterValue { get; set; }
    }

    public struct ObjectProvisioningState
    {
        public DateTime RecordTime { get; set; }
        public String CurrentState { get; set; }
        public String DesiredState { get; set; }
        public String ProvisioningAction { get; set; }
    }

    #endregion ---- PowerShellRunspaceManager Public NameSpace Structs and Classes ----

    #region ---- Serializable Dictionary Class ----

    [XmlRoot("dictionary")]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable
    {
        #region IXmlSerializable Members

        public SerializableDictionary() : base()
        {
        }

        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(System.Xml.XmlReader reader)
        {
            XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
            XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));

            bool wasEmpty = reader.IsEmptyElement;
            reader.Read();

            if (wasEmpty)
                return;

            while (reader.NodeType != System.Xml.XmlNodeType.EndElement)
            {
                reader.ReadStartElement("item");

                reader.ReadStartElement("key");
                TKey key = (TKey)keySerializer.Deserialize(reader);
                reader.ReadEndElement();

                reader.ReadStartElement("value");
                TValue value = (TValue)valueSerializer.Deserialize(reader);
                reader.ReadEndElement();

                this.Add(key, value);

                reader.ReadEndElement();
                reader.MoveToContent();
            }
            reader.ReadEndElement();
        }

        public void WriteXml(System.Xml.XmlWriter writer)
        {
            XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
            XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));

            foreach (TKey key in this.Keys)
            {
                writer.WriteStartElement("item");

                writer.WriteStartElement("key");
                keySerializer.Serialize(writer, key);
                writer.WriteEndElement();

                writer.WriteStartElement("value");
                TValue value = this[key];
                valueSerializer.Serialize(writer, value);
                writer.WriteEndElement();

                writer.WriteEndElement();
            }
        }

        #endregion IXmlSerializable Members
    }

    #endregion ---- Serializable Dictionary Class ----
}