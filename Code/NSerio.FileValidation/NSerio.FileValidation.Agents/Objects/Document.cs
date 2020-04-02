using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using kCura.Relativity.Client;
using DTOs = kCura.Relativity.Client.DTOs;
namespace NSerio.FileValidation.Agents
{
	public class Document
	{

		#region " Vars "
		private bool HasMissingFiles;
		private int ID;
		private Batch Batch;
        private System.Data.DataTable Files;
        private IRSAPIClient Connection;
        #endregion

        #region " Properties "
        public int ArtifactID { get; set; }
		#endregion

		#region " Constructors "
		public Document(System.Data.DataRow row, Batch batch, IRSAPIClient connection)
		{
			try {
				ArtifactID = (int)row["DocumentArtifactID"];
				ID = (int)row["ID"];
				Files = new System.Data.DataTable();
				HasMissingFiles = false;
				Batch = batch;
                Connection = connection;
			} catch (Exception ex) {
				throw;
			}
		}
		#endregion

		#region " Public Methods "
		public void Process()
		{
			try {
				RetrieveFiles();
				ProcessFiles();
				UpdateFlag();
                RemoveDocument();
			} catch (Exception ex) {
				throw;
			}
		}
		#endregion

		#region " Private Methods "
		private void RemoveDocument()
		{
			string sql = null;

			try {
				sql = string.Format(Database.Resources.RemoveFromDocumentQueue, ID.ToString());
				Batch._Workspace.EDDSDBConnection.ExecuteNonQuerySQLStatement(sql);
			} catch (Exception ex) {
				throw;
			}
		}

		private void RetrieveFiles()
		{
			string sql = null;

			try {
				sql = string.Format(Database.Resources.RetrieveFiles, ArtifactID.ToString());
				Files = Batch._Workspace.WorkspaceDBConnection.ExecuteSqlStatementAsDataTable(sql);
			} catch (Exception ex) {
				throw;
			}
		}

		private void ProcessFiles()
		{
			File file = null;

			try {
				if (Files.Rows.Count > 0) {
					foreach (System.Data.DataRow row in Files.Rows) {
						file = new File(row, this, Batch._Workspace.WorkspaceDBConnection);
						file.Validate();
                        CreateFileValidationResult(file._IsMissing, file._Type);
						if (file._IsMissing)
							HasMissingFiles = true;
					}
				}
			} catch (Exception ex) {
				throw;
			}
		}

		private void UpdateFlag()
		{
			string sql = null;

			try {
				if (HasMissingFiles) {
					sql = string.Format(Database.Resources.UpdateFlagFile, Batch._Workspace.FlagFieldColumnName, "1", ArtifactID.ToString());
				} else {
					sql = string.Format(Database.Resources.UpdateFlagFile, Batch._Workspace.FlagFieldColumnName, "0", ArtifactID.ToString());
				}
				Batch._Workspace.WorkspaceDBConnection.ExecuteNonQuerySQLStatement(sql);
			} catch (Exception ex) {
				throw;
			}
		}

        private void CreateFileValidationResult(bool isMissing, string fileType)
        {
            try
            {
                var fvResult = new DTOs.RDO();
                fvResult.ArtifactTypeGuids.Add(new Guid(MainApp.Helper.Constant.OBJECT_TYPE_FILE_VALIDATIOM_RESULT_GUID));

                DTOs.FieldValueList<DTOs.Document> docObjects = new DTOs.FieldValueList<DTOs.Document>();
                var doc = new DTOs.Document(ArtifactID);
                docObjects.Add(doc);

                fvResult.Fields.Add(new DTOs.FieldValue(new Guid(MainApp.Helper.Constant.FIELD_FR_NAME), DateTime.UtcNow.ToString("MM-dd-yyyy HH:mm:ss")));
                fvResult.Fields.Add(new DTOs.FieldValue(new Guid(MainApp.Helper.Constant.FIELD_FR_DOCUMENT), docObjects));
                fvResult.Fields.Add(new DTOs.FieldValue(new Guid(MainApp.Helper.Constant.FIELD_FR_VALIDATION_DATE), DateTime.UtcNow));
                fvResult.Fields.Add(new DTOs.FieldValue(new Guid(MainApp.Helper.Constant.FIELD_FR_EXIST), isMissing));
                fvResult.Fields.Add(new DTOs.FieldValue(new Guid(MainApp.Helper.Constant.FIELD_FR_TYPE), fileType));

                DTOs.WriteResultSet<DTOs.RDO> writeResults = Connection.Repositories.RDO.Create(fvResult);

                if (!writeResults.Success)
                {
                    throw new Exception(string.Format("An error occurred in result creation: {0}", writeResults.Results[0].Message));
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            
        }

       
        #endregion

    }
}
