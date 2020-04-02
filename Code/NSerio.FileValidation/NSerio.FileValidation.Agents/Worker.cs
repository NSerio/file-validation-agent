using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using System.Net;
using System.Runtime.InteropServices;
using kCura.Agent;
using Relativity.API;
using kCura.Relativity.Client;
namespace NSerio.FileValidation.Agents
{

    [Guid("4F16806D-3F43-473F-BF1D-EBF1315BB8DD")]
    [kCura.Agent.CustomAttributes.Name("File Validation Worker (v6)")]
    public class Worker : kCura.Agent.AgentBase
    {

        #region " Vars "
        #endregion
        private const int BatchSize = 1000;

        public override void Execute()
        {
            Batch batch = null;
            System.Data.DataTable queryResult = null;
            System.Data.DataRow workspaceRow = null;
            Workspace workspace = null;
            int workspaceArtifactId = default(int);

            try
            {
                queryResult = Helper.GetDBContext(-1).ExecuteSqlStatementAsDataTable(string.Format(Database.Resources.RetrieveNextWorkspace, this.AgentID));
                if (queryResult.Rows.Count > 0)
                {
                    workspaceRow = queryResult.Rows[0];
                    workspaceArtifactId = (int)workspaceRow["CaseArtifactID"];

                    if (DoesWorkspaceExist(workspaceArtifactId))
                    {
                        using (IRSAPIClient proxy = Helper.GetServicesManager().CreateProxy<IRSAPIClient>(ExecutionIdentity.System))
                        {
                            proxy.APIOptions.WorkspaceID = workspaceArtifactId;
                            try
                            {
                                workspace = new Workspace(proxy, workspaceRow, Helper);
                                workspace.WorkspaceDBConnection = Helper.GetDBContext(workspace.ArtifactID);

                                RaiseMessage(string.Format("Retrieving next batch of {0} documents [Workspace Artifact ID={1}]", BatchSize.ToString(), workspace.ArtifactID), 10);
                                batch = new Batch(BatchSize, this.AgentID, Helper, workspace, proxy);

                                RaiseMessage(string.Format("Processing batch [Workspace Artifact ID={0} Job Artifact ID={1}]", workspace.ArtifactID.ToString(), workspace.JobArtifactID.ToString()), 10);
                                batch.Process();
                                RaiseMessage("Batch complete", 10);
                            }
                            catch (Exception ex)
                            {
                                workspace.HandleError(ex);
                                throw ex;
                            }
                        }
                    }
                    else
                    {
                        //Cleanup manager and worker queue if workspace has been deleted
                        RaiseMessage(string.Format("Workspace no longer exists. Removing the workspace from the queue tables [Workspace Artifact ID={0}]", workspaceArtifactId), 10);
                        Helper.GetDBContext(-1).ExecuteNonQuerySQLStatement(string.Format(Database.Resources.CleanupDeletedCaseFromManagerQueue, workspaceArtifactId));
                        Helper.GetDBContext(-1).ExecuteNonQuerySQLStatement(string.Format(Database.Resources.CleanupDeletedCaseFromWorkerQueue, BatchSize, workspaceArtifactId));
                        RaiseMessage(string.Format("Workspace has been removed from the queue tables [Workspace Artifact ID={0}]", workspaceArtifactId), 10);
                    }
                }
                else
                {
                    RaiseMessage("No batches ready", 10);
                }
            }
            catch (Exception ex)
            {
                if (ex.InnerException == null)
                {
                    RaiseError(ex.Message, ex.Message);
                }
                else
                {
                    RaiseError(ex.Message, ex.InnerException.Message);
                }

            }

        }

        private bool DoesWorkspaceExist(int workspaceArtifactId)
        {
            bool retVal = false;

            object result = Helper.GetDBContext(-1).ExecuteSqlStatementAsScalar(string.Format(Database.Resources.CheckIfCaseStillExists, workspaceArtifactId));
            if (!System.DBNull.Value.Equals(result) && (int)result > 0)
            {
                retVal = true;
            }

            return retVal;
        }

        public override string Name
        {
            get { return "File Validation Worker (v6)"; }
        }
    }
}
