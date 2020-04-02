using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using kCura.Relativity.Client;
using DTOs = kCura.Relativity.Client.DTOs;

namespace NSerio.FileValidation.Agents
{

    public class Workspace
    {

        #region " Properties "
        public int ArtifactID { get; set; }
        public int JobArtifactID { get; set; }
        public Relativity.API.IDBContext WorkspaceDBConnection { get; set; }
        public string FlagFieldColumnName { get; set; }
        public Relativity.API.IDBContext EDDSDBConnection { get; set; }
        #endregion

        #region " Vars "
        private bool DocumentsQueued;
        private bool NoResults;
        private int FlagFieldArtifactID;
        private int ID;
        private bool InProgress;
        //private SqlDataReader Reader;
        private DataTable DocumentResult;
        private Relativity.API.IHelper Helper;
        private IRSAPIClient Connection;
        #endregion

        #region " Constructors "
        public Workspace(IRSAPIClient connection, DataRow workspaceRow, Relativity.API.IHelper helper)
        {
            try
            {
                ArtifactID = (int)workspaceRow["CaseArtifactID"];
                ID = (int)workspaceRow["ID"];
                JobArtifactID = (int)workspaceRow["JobArtifactID"];
                InProgress = Convert.ToBoolean(workspaceRow["Status"]);
                FlagFieldColumnName = Convert.ToString(workspaceRow["FlagFieldColumnName"]);
                Helper = helper;
                EDDSDBConnection = Helper.GetDBContext(-1);
                WorkspaceDBConnection = Helper.GetDBContext(ArtifactID);
                Connection = connection;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        #region " Public Methods "
        public void Prepare()
        {
            try
            {
                if (InProgress == false)
                {
                    HandleWaitingJob();
                }
                else if (InProgress == true)
                {
                    CheckDocumentQueue();
                    if (DocumentsQueued == false)
                    {
                        HandleCompletedjob();
                    }
                    
                    if(NoResults == true)
                    {
                        HandleNoResultsJob();
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public void HandleError(Exception excep)
        {
            string errorMessage = null;

            try
            {
                if (excep.InnerException == null)
                {
                    errorMessage = excep.Message;
                }
                else
                {
                    errorMessage = excep.Message + " " + excep.InnerException.Message;
                }

                MainApp.RSAPI.JobQuery.UpdateStatus(Connection, JobArtifactID, MainApp.Helper.Constant.JOB_STATUS_ERROR);
                MainApp.RSAPI.JobQuery.UpdateErrorMessage(Connection, JobArtifactID, errorMessage);

                UpdateStatusInQueue(-1);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        #endregion

        #region " Private Methods "
        private void CheckDocumentQueue()
        {
            string sql = null;
            object result = null;

            try
            {
                DocumentsQueued = false;
                sql = string.Format(Database.Resources.DoQueueRecordsExist, ArtifactID.ToString(), JobArtifactID.ToString());
                result = EDDSDBConnection.ExecuteSqlStatementAsScalar(sql);
                if ((result != null))
                {
                    if (!System.DBNull.Value.Equals(result))
                    {
                        DocumentsQueued = true;
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void HandleWaitingJob()
        {
            try
            {
                ClearPreviousRecords();
                RetrieveDocuments();
                QueueDocuments();
                ClearSavedSearchTable();
                MainApp.RSAPI.JobQuery.UpdateStatus(Connection, JobArtifactID, MainApp.Helper.Constant.JOB_STATUS_IN_PROGRESS);
                UpdateStatusInQueue(1);

            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void HandleCompletedjob()
        {
            try
            {
                RemoveFromQueue();
                MainApp.RSAPI.JobQuery.UpdateStatus(Connection, JobArtifactID, MainApp.Helper.Constant.JOB_STATUS_NEW);
                MainApp.RSAPI.JobQuery.UpdateErrorMessage(Connection, JobArtifactID, string.Format("Job completed at {0}", DateTime.Now));
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void HandleNoResultsJob()
        {
            try
            {
                RemoveFromQueue();
                MainApp.RSAPI.JobQuery.UpdateStatus(Connection, JobArtifactID, MainApp.Helper.Constant.JOB_STATUS_NEW);
                MainApp.RSAPI.JobQuery.UpdateErrorMessage(Connection, JobArtifactID, string.Format("The file type selected was not found on the saved search"));
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void QueueDocuments()
        {
            Relativity.API.IDBContext eddsReaderConnection = null;

            try
            {
                NoResults = false;

                if (DocumentResult.Rows.Count <= 0)
                {
                    NoResults =true;
                }

                eddsReaderConnection = EDDSDBConnection;
                eddsReaderConnection.GetConnection().ChangeDatabase("EDDS");
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(eddsReaderConnection.GetConnection()))
                {
                    bulkCopy.BulkCopyTimeout = 300;
                    bulkCopy.DestinationTableName = MainApp.Helper.Constant.DOCUMENT_QUEUE_TABLE_NAME;
                    bulkCopy.ColumnMappings.Add("CaseArtifactID", "CaseArtifactID");
                    bulkCopy.ColumnMappings.Add("JobArtifactID", "JobArtifactID");
                    bulkCopy.ColumnMappings.Add("AgentID", "AgentID");
                    bulkCopy.ColumnMappings.Add("TimeStamp", "TimeStamp");
                    bulkCopy.ColumnMappings.Add("Status", "Status");
                    bulkCopy.ColumnMappings.Add("DocumentArtifactID", "DocumentArtifactID");
                    //bulkCopy.WriteToServer(Reader);
                    //Reader.Close();
                    bulkCopy.WriteToServer(DocumentResult);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void RetrieveDocuments()
        {
            string sql = null;

            try
            {
                sql = string.Format(Database.Resources.RetrieveDocuments, ArtifactID.ToString(), JobArtifactID.ToString());

                // Check for save search
                if (ValidateSavedSearh(JobArtifactID))
                {
                    var ssArtifactId = GetSavedSearchArtifactID(JobArtifactID);
                    FilterBySavedSearch(ssArtifactId);

                    sql = string.Concat(sql," ", Database.Resources.SavedSearchFilter);
                }

                // Check for File type selection
                if (ValidateFileType(JobArtifactID))
                {
                    var fileTypeCode = GetFileTypeCode();
                    sql = string.Concat(sql, " ", string.Format(Database.Resources.FileTypeFilter, fileTypeCode, JobArtifactID));
                }

                DocumentResult = WorkspaceDBConnection.ExecuteSqlStatementAsDataTable(sql, 300);
                //Reader = WorkspaceDBConnection.ExecuteSQLStatementAsReader(sql, 300);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private bool ValidateFileType(int jobArtifactID)
        {
            bool result = false;

            try
            {
                var fileTypeCode = GetFileTypeCode();
                if (fileTypeCode != null)
                {
                    string sql = string.Format(Database.Resources.CheckForFileType, fileTypeCode, JobArtifactID.ToString());
                    var dbResult = WorkspaceDBConnection.ExecuteSqlStatementAsScalar(sql);

                    result = (int)dbResult > 0;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return result;
        }

        private int? GetFileTypeCode()
        {
            int? result = null;
            DTOs.Field ftField = Connection.Repositories.Field.ReadSingle(new Guid(MainApp.Helper.Constant.FIELD_DOCUMENT_TYPE));

            if (ftField != null)
            {
                result = ftField.ChoiceTypeID;
            }

            return result;
        }

        private int GetSavedSearchArtifactID(int jobArtifactID)
        {
            int result = -1;

            try
            {
                string sql = string.Format(Database.Resources.CheckForJobSS, JobArtifactID.ToString());
                var dbResult = WorkspaceDBConnection.ExecuteSqlStatementAsScalar(sql);
                if (dbResult != null)
                {
                    result = (int)dbResult;
                }
            }
            catch (Exception)
            {
                throw;
            }

            return result;
        }

        private bool ValidateSavedSearh(int jobArtifactID)
        {
            bool result = false;

            try
            {
                var ssArtifactId = GetSavedSearchArtifactID(jobArtifactID);

                result = (int)ssArtifactId > 0;
            }
            catch (Exception)
            {
                throw;
            }

            return result;
        }

        private RelativityScriptResult FilterBySavedSearch(int SavedSearch)
        {
            // STEP 1: Find script
            DTOs.RelativityScript script = this.GetRelativityScriptByName();

            // STEP 2: Set parameters and execute script
            List<RelativityScriptInput> scriptInputs = new List<RelativityScriptInput>();

            scriptInputs.Add(new RelativityScriptInput("SavedSearch", SavedSearch.ToString()));

            RelativityScriptResult scriptResult = Connection.Repositories.RelativityScript.ExecuteRelativityScript(script, scriptInputs);
            if (!scriptResult.Success)
                throw new ApplicationException(scriptResult.Message);

            return scriptResult;
        }

        private DTOs.RelativityScript GetRelativityScriptByName()
        {
            DTOs.RelativityScript script = null;

            //Aseemble Relativity script query
            TextCondition nameCondition = new TextCondition(DTOs.RelativityScriptFieldNames.Name, TextConditionEnum.EqualTo, MainApp.Helper.Constant.SAVED_SEARCH_NAME);

            DTOs.Query<DTOs.RelativityScript> relScriptQuery = 
                new DTOs.Query<DTOs.RelativityScript>
            {
                Condition = nameCondition,
                Fields = DTOs.FieldValue.NoFields
            };

            try
            {
                // Execute query
                DTOs.QueryResultSet<DTOs.RelativityScript> relScriptQueryResults = null;

                relScriptQueryResults = Connection.Repositories.RelativityScript.Query(relScriptQuery);

                if (relScriptQueryResults.Success)
                {
                    script = relScriptQueryResults.Results[0].Artifact;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return script;
        }
        private void ClearPreviousRecords()
        {
            string sql = null;

            try
            {
                sql = Database.Resources.RemoveFromMissingFiles;
                WorkspaceDBConnection.ExecuteNonQuerySQLStatement(sql);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void RemoveFromQueue()
        {
            string sql = null;

            try
            {
                sql = string.Format(Database.Resources.RemoveFromCasesQueue, ArtifactID.ToString(), JobArtifactID.ToString());
                EDDSDBConnection.ExecuteNonQuerySQLStatement(sql);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void ClearSavedSearchTable()
        {
            try
            {
                string sql = Database.Resources.DeleteSSTable;
                var dbResult = WorkspaceDBConnection.ExecuteNonQuerySQLStatement(sql);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void UpdateStatusInQueue(int status)
        {
            string sql = null;

            try
            {
                sql = string.Format(Database.Resources.UpdateCasesQueue, status.ToString(), ArtifactID.ToString(), JobArtifactID.ToString());
                EDDSDBConnection.ExecuteNonQuerySQLStatement(sql);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        #endregion

    }
}
