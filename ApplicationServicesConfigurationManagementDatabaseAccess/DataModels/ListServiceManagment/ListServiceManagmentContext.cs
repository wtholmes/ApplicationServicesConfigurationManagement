// Ignore Spelling: Elist

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ListServiceManagement.Models
{
    /// <summary>
    ///     List Service Management Database Context
    /// </summary>
    public partial class ListServiceManagementContext : DbContext
    {
        /// <summary>
        /// List Service Management Database Context Constructor
        /// </summary>
        public ListServiceManagementContext() : base("name=ListServiceManagement")
        {
            Database.SetInitializer<ListServiceManagementContext>(new ListServiceManagmentDBInitializer());
        }

        /// <summary>
        ///
        /// </summary>
        public virtual DbSet<ElistContact> ElistContacts { get; set; }

        /// <summary>
        ///
        /// </summary>
        public virtual DbSet<ElistContacts_History> ElistContacts_History { get; set; }

        /// <summary>
        ///
        /// </summary>
        public virtual DbSet<ElistOwnerTransfer> ElistOwnerTransfers { get; set; }

        /// <summary>
        ///
        /// </summary>
        public virtual DbSet<ElistOwnerTransfer_History> ElistOwnerTransfer_History { get; set; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
        }

        /// <summary>
        ///
        /// </summary>
        public class ListServiceManagmentDBInitializer : CreateDatabaseIfNotExists<ListServiceManagementContext>
        {
            /// <summary>
            ///
            /// </summary>
            /// <param name="context"></param>
            protected override void Seed(ListServiceManagementContext context)
            {
                List<String> HistoryTableTriggers = new List<String>() { "ElistContact", "ElistOwnerTransfer" };
                foreach (String HistoryTableTrigger in HistoryTableTriggers)
                {
                    Type type = Type.GetType(String.Format("ListServiceManagement.Models.{0}", HistoryTableTrigger));
                    PropertyInfo[] propertyInfo = type.GetProperties();
                    List<String> SourceFields = new List<String>();
                    List<String> DestinationFields = new List<String>();
                    String CreateTriggerSQL;

                    for (int i = 0; i < propertyInfo.Length; i++)
                    {
                        String PropertyName = propertyInfo[i].Name.Replace("SerializedMetaData", "MetaData");
                        DestinationFields.Add(String.Format("[{0}]", PropertyName));
                        SourceFields.Add(String.Format("d.[{0}]", PropertyName));
                    }

                    CreateTriggerSQL = Regex.Replace(
                            String.Format(@"
                                    CREATE TRIGGER [dbo].[{0}_SaveHistory]
                                        ON [dbo].[{0}s]
                                    AFTER UPDATE, DELETE
                                    AS
                                    IF EXISTS (SELECT * FROM Inserted)
                                      -- UPDATE Statement was executed
                                    INSERT INTO dbo.{0}_History (
                                        [Change],
                                        [ChangeTime],
                                        {1})
                                    SELECT
                                        'UPDATE',
                                        GETUTCDATE(),
                                        {2}
                                        FROM Deleted d
                                    INNER JOIN Inserted i ON i.{3} = d.{3}
                                    ELSE
                                      -- DELETE Statement was executed
                                    INSERT INTO dbo.{0}_History (
                                        [Change],
                                        [ChangeTime],
                                        {1})
                                    SELECT
                                        'DELETE',
                                        GETUTCDATE(),
                                        {2}
                                        FROM Deleted d",
                            HistoryTableTrigger,
                            String.Join(",\n", DestinationFields.ToArray()),
                            String.Join(",\n", SourceFields.ToArray()),
                            DestinationFields[0]),
                    @"([^\S\n]{2,}|[\r])", "").Trim();

                    // Execute the SQL commmand to create the trigger.
                    context.Database.ExecuteSqlCommand(CreateTriggerSQL, new SqlParameter[] { });
                }

                base.Seed(context);
            }
        }
    }
}