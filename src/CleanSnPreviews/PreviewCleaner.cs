using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;

namespace CleanSnPreviews
{
    public class PreviewCleaner
    {
        private static string CR = Environment.NewLine;

        // Business logic properties
        public PreviewCleanerModes Mode { get; set; }
        public int Iteration { get; set; }
        public int Top { get; set; }
        public ToolLogger logger { get; set; }

        public PreviewCleaner(Dictionary<string, string> parameters = null)
        {
            this.Iteration = 1;
            logger = new ToolLogger() {LogName = "cleanthumbslog"};
            if (parameters != null)
            {
                if (parameters.ContainsKey("TOP"))
                {
                    int top;
                    int.TryParse((string)parameters["TOP"], out top);
                    this.Top = (top > 0 ? top : 100);
                }

                PreviewCleanerModes mode = PreviewCleanerModes.ShowOnly;
                if (parameters.ContainsKey("MODE"))
                {
                    Enum.TryParse((string)parameters["MODE"], true, out mode);
                }
                this.Mode = mode;
            }
        }

        private const string deleteScript = "proc_Node_DeletePhysical";

        private string SelectScript
        {
            get
            {
                string result =  $"SELECT TOP {(this.Top > 0 ? this.Top : 100)} [NodeId], [Name], [Path], [Type] FROM [dbo].[NodeInfoView] WHERE Type = 'PreviewImage' ORDER BY [Path]";
                return result;
            }
        }

        private static int _operationSleep;

        private static int OperationSleep
        {
            get
            {
                if (_operationSleep == 0)
                {
                    string operationSleepFromconfig = ConfigurationManager.AppSettings["OperationSleep"];
                    if (string.IsNullOrWhiteSpace(operationSleepFromconfig) ||
                        !int.TryParse(operationSleepFromconfig, out _operationSleep))
                    {
                        _operationSleep = 2000;
                    }
                }
                return _operationSleep;
            }
        }


        // DB connection properties
        private static string _connectionString;

        private static string ConnectionString
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_connectionString))
                {
                    _connectionString = ConfigurationManager.ConnectionStrings["SnCrMsSql"].ConnectionString;
                }
                return _connectionString;
            }
        }

        private static int _sqlCommandTimeout;

        private static int SqlCommandTimeout
        {
            get
            {
                if (_sqlCommandTimeout == 0)
                {
                    string sqlCommandTimeoutFromconfig = ConfigurationManager.AppSettings["SqlCommandTimeout"];
                    if (string.IsNullOrWhiteSpace(sqlCommandTimeoutFromconfig) ||
                        !int.TryParse(sqlCommandTimeoutFromconfig, out _sqlCommandTimeout))
                    {
                        _sqlCommandTimeout = 120;
                    }
                }
                return _sqlCommandTimeout;
            }
        }

        // Business logic's main operation methods
        public void RunPreviewImageCleaner()
        {

            using (SqlConnection connection = new SqlConnection() { ConnectionString = ConnectionString })
            {
                this.logger.LogWriteLine($"Operation mode: {this.Mode}");
                this.logger.LogWriteLine($"Connecting to {connection.DataSource}/{connection.Database}...");
                this.logger.LogWriteLine();

                connection.Open();
                int row = 1;
                bool repeat;
                do
                {
                    repeat = false;
                    Dictionary<int, string> deleteQueue = new Dictionary<int, string>();
                    using (SqlCommand command = new SqlCommand(SelectScript, connection))
                    {
                        command.CommandType = CommandType.Text;
                        command.CommandTimeout = SqlCommandTimeout;
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                // Delete operation will continoue till all the prewiews has been removed from the system
                                if (this.Mode == PreviewCleanerModes.Delete)
                                {
                                    repeat = true;
                                }
                                // while there is another record present
                                while (reader.Read())
                                {
                                    var nodeId = reader[0];
                                    var nodeName = reader[1];
                                    var nodePath = reader[2];
                                    var nodeType = reader[3];
                                    //this.logger.LogWriteLine($"{row++} \t{nodeId} {nodeName} ");
                                    this.logger.LogWriteLine($"{nodePath} ");
                                    switch (this.Mode)
                                    {
                                        case PreviewCleanerModes.Delete:
                                            // Here will be dragons... 
                                            //DeletePreview(connection, (int)nodeId, cleanerInstance);
                                            //deleteQueue.Add((int)nodeId);
                                            deleteQueue.Add((int)nodeId, (string)nodeType);
                                            break;
                                        case PreviewCleanerModes.ShowOnly:
                                            // it!s only a lizard (simulation)
                                            break;
                                    }
                                }
                            }
                        }

                        // Delete operation clean the db from selected previews
                        if (this.Mode == PreviewCleanerModes.Delete)
                        {
                            this.logger.LogWrite("Queried items delete operation is in progress...");
                            foreach (var previewData in deleteQueue)
                            {
                                // Key = previewId, Value = nodeType
                                if (DeletePreview(connection, previewData.Key, previewData.Value))
                                {
                                    this.logger.LogWrite(".");
                                }
                                else
                                {
                                    this.logger.LogWriteLine();
                                    this.logger.LogWriteLine("Something went wrong!");
                                }
                            }
                            this.logger.LogWriteLine();
                        }
                    }

                    // Wait for a little between iterations
                    Thread.Sleep(OperationSleep);
                } while (repeat);

            }
        }

        private bool DeletePreview(SqlConnection connection, int nodeID, string nodeType)
        {
            bool result = false;
            if (nodeID > 0 && nodeType == "PreviewImage")
            {
                try
                {
                    //string deleteScriptWithParameter = $"{deleteScript} @NodeId {nodeID.ToString()}";
                    using (SqlCommand deleteCommand = new SqlCommand(deleteScript, connection))
                    {
                        deleteCommand.CommandType = CommandType.StoredProcedure;
                        deleteCommand.CommandTimeout = SqlCommandTimeout;
                        deleteCommand.Parameters.Add("@NodeId", SqlDbType.Int).Value = nodeID;

                        // what date should be here?
                        deleteCommand.Parameters.Add("@Timestamp", SqlDbType.Timestamp).Value = DBNull.Value;
                        int affectedRows = deleteCommand.ExecuteNonQuery();
                        if (affectedRows > 0)
                        {
                            result = true;
                        }
                    }
                }
                catch (SqlException e)
                {
                    this.logger.LogWriteLine();
                    this.logger.LogWriteLine("========================================");
                    this.logger.LogWriteLine("CleanSnPreviews delete task throw an exception:");
                    this.logger.LogWriteLine(e.Message);
                    result = false;
                }
            }
            return result;
        }
    }

}
