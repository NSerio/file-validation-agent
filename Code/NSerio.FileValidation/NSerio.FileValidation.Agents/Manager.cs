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

	[Guid("A1B6CFE0-1452-4491-878B-E754D90F0CC9")]
	[kCura.Agent.CustomAttributes.Name("File Validation Manager (v6)")]
	public class Manager : kCura.Agent.AgentBase
	{

		#region " Vars "
			#endregion
		private const int BatchSize = 1000;

		public override void Execute()
		{
			System.Data.DataTable workspaces = null;
			Workspace workspace = null;
			int workspaceArtifactId = default(int);

			try {
				RaiseMessage("Retrieving workspaces", 10);
				workspaces = Helper.GetDBContext(-1).ExecuteSqlStatementAsDataTable(Database.Resources.RetrieveWorkspaces);

				if (workspaces.Rows.Count > 0) {
					foreach (System.Data.DataRow row in workspaces.Rows) {
						workspaceArtifactId = (int)row["CaseArtifactID"];

						if (DoesWorkspaceExist(workspaceArtifactId)) {
							using (IRSAPIClient proxy = Helper.GetServicesManager().CreateProxy<IRSAPIClient>(ExecutionIdentity.System)) {
								proxy.APIOptions.WorkspaceID = workspaceArtifactId;
								try {
									workspace = new Workspace(proxy, row, Helper);
									workspace.WorkspaceDBConnection = Helper.GetDBContext(workspace.ArtifactID);

									RaiseMessage(string.Format("Preparing workspace job [Workspace Artifact ID={0} Job Artifact ID={1}]", workspace.ArtifactID.ToString(), workspace.JobArtifactID.ToString()), 10);
									workspace.Prepare();
									RaiseMessage(string.Format("Workspace job ready [Workspace Artifact ID={0} Job Artifact ID={1}]", workspace.ArtifactID.ToString(), workspace.JobArtifactID.ToString()), 10);
								} catch (Exception ex) {
									workspace.HandleError(ex);
									throw ex;
								}
							}
						} else {
							//Cleanup manager and worker queue if workspace has been deleted
							RaiseMessage(string.Format("Workspace no longer exists. Removing the workspace from the queue tables [Workspace Artifact ID={0}]", workspaceArtifactId), 10);
							Helper.GetDBContext(-1).ExecuteNonQuerySQLStatement(string.Format(Database.Resources.CleanupDeletedCaseFromManagerQueue, workspaceArtifactId));
							Helper.GetDBContext(-1).ExecuteNonQuerySQLStatement(string.Format(Database.Resources.CleanupDeletedCaseFromWorkerQueue, BatchSize, workspaceArtifactId));
							RaiseMessage(string.Format("Workspace has been removed from the queue tables [Workspace Artifact ID={0}]", workspaceArtifactId), 10);
						}

					}
				} else {
					RaiseMessage("No workspaces to process", 10);
				}
			} catch (Exception ex) {
				if (ex.InnerException == null) {
					RaiseError(ex.Message, ex.Message);
				} else {
					RaiseError(ex.Message, ex.InnerException.Message);
				}
			}
		}

		private bool DoesWorkspaceExist(int workspaceArtifactId)
		{
			bool retVal = false;

			object result = Helper.GetDBContext(-1).ExecuteSqlStatementAsScalar(string.Format(Database.Resources.CheckIfCaseStillExists, workspaceArtifactId));
			if (!System.DBNull.Value.Equals(result) && (int)result > 0) {
				retVal = true;
			}

			return retVal;
		}

		public override string Name {
			get { return "File Validation Manager (v6)"; }
		}
	}
}
