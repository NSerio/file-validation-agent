using kCura.Relativity.Client;
using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
namespace NSerio.FileValidation.Agents
{
	public class Batch
	{

		#region " Vars "
		private int BatchSize;
		private int AgentID;
		private System.Data.DataTable Documents;
        private Relativity.API.IDBContext EDDSDBConnection;
        private IRSAPIClient Connection;
        #endregion

        #region " Properties "
        public Relativity.API.IHelper _Helper { get; set; }
		public Workspace _Workspace { get; set; }
		#endregion

		#region " Constructors "
		public Batch(int batchSize, int agentID, Relativity.API.IHelper helper, Workspace workspace, IRSAPIClient connection)
		{
			try {
				BatchSize = batchSize;
				AgentID = agentID;
				Documents = new System.Data.DataTable();
				_Workspace = workspace;
				_Helper = helper;
				EDDSDBConnection = _Helper.GetDBContext(-1);
                Connection = connection;
			} catch (Exception ex) {
				throw;
				throw;
			}
		}
		#endregion

		#region " Public Methods "
		public void Process()
		{
			try {
				RetrieveDocuments();
				CheckDocuments();
			} catch (Exception ex) {
				throw;
				throw;
			}
		}
		#endregion

		#region " Private Methods "
		private void RetrieveDocuments()
		{
			try {
				RetrieveUnfinishedRecords();
				if (Documents.Rows.Count == 0) {
					RetrieveNextRecords();
				}
			} catch (Exception ex) {
				throw;
			}
		}

		private void CheckDocuments()
		{
			Document document = null;

			try {
				if (Documents.Rows.Count > 0) {
					foreach (System.Data.DataRow docRow in Documents.Rows) {
						document = new Document(docRow, this, Connection);
						document.Process();
					}
				}
			} catch (Exception ex) {
				throw;
			}
		}

		private void RetrieveUnfinishedRecords()
		{
			string sql = null;

			try {
				sql = string.Format(Database.Resources.RetrieveUnfinishedRecords, BatchSize.ToString(), AgentID.ToString(), _Workspace.ArtifactID, _Workspace.JobArtifactID);
				Documents = EDDSDBConnection.ExecuteSqlStatementAsDataTable(sql);
			} catch (Exception ex) {
				throw;
			}
		}

		private void RetrieveNextRecords()
		{
			string sql = null;

			try {
				sql = string.Format(Database.Resources.RetrieveNextRecords, BatchSize.ToString(), AgentID.ToString(), _Workspace.ArtifactID, _Workspace.JobArtifactID);
				Documents = EDDSDBConnection.ExecuteSqlStatementAsDataTable(sql);
			} catch (Exception ex) {
				throw;
			}
		}
		#endregion

	}
}
