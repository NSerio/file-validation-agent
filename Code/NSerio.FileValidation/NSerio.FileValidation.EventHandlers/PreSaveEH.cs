using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
namespace NSerio.FileValidation.EventHandlers
{
	public class PreSaveEH : kCura.EventHandler.PreSaveEventHandler, kCura.EventHandler.IDataEnabled
	{

		public override kCura.EventHandler.Response Execute()
		{
			kCura.EventHandler.Response response = new kCura.EventHandler.Response();
			string tableName = null;

			response.Success = true;
			response.Message = "";

			//If job is new
			if (this.ActiveArtifact.IsNew) {

				try {
					//Make sure a record does not already exist
					tableName = RetrieveArtifactTypeName();
					if (DoesRecordExist(tableName)) {
						response.Success = false;
						response.Message = NSerio.FileValidation.MainApp.Helper.Constant.EM_JOB_ALREADY_EXISTS;
						return response;
					}

					//Set job status to New
					UpdateStatus();
				} catch (Exception ex) {
					response.Success = false;
					response.Message = ex.Message;
				}

			}

			return response;
		}

		private void UpdateStatus()
		{
			string sql = null;
			object result = null;
			int statusFieldArtifactID = default(int);

			try {
				sql = string.Format(Database.Resources.RetrieveArtifactIDByGuid, NSerio.FileValidation.MainApp.Helper.Constant.FIELD_JOB_STATUS_GUID);
				result = Helper.GetDBContext(Helper.GetActiveCaseID()).ExecuteSqlStatementAsScalar(sql);
				if (!System.DBNull.Value.Equals(result)) {
					statusFieldArtifactID = (int)result;
					this.ActiveArtifact.Fields[statusFieldArtifactID].Value.Value = NSerio.FileValidation.MainApp.Helper.Constant.JOB_STATUS_NEW;
				}
			} catch (Exception ex) {
				throw;
			}
		}

		public override kCura.EventHandler.FieldCollection RequiredFields {
			get {
				kCura.EventHandler.FieldCollection fieldCollection = new kCura.EventHandler.FieldCollection();
				fieldCollection.Add(new kCura.EventHandler.Field(NSerio.FileValidation.MainApp.Helper.Constant.FIELD_JOB_STATUS_GUID));
				return fieldCollection;
			}
		}

		private bool DoesRecordExist(string tableName)
		{
			bool retVal = false;
			object result = null;
			string sql = null;

			try {
				sql = string.Format(Database.Resources.DoesRecordExistAlready, tableName);
				result = Helper.GetDBContext(Helper.GetActiveCaseID()).ExecuteSqlStatementAsScalar(sql);
				if ((result != null)) {
					if (!System.DBNull.Value.Equals(result)) {
						if (!((int)result == this.ActiveArtifact.ArtifactID)) {
							retVal = true;
						}
					}
				}
				return retVal;
			} catch (Exception ex) {
				throw;
			}
		}

		private string RetrieveArtifactTypeName()
		{
			string sql = null;
			object result = null;
			string retVal = null;

			try {
				sql = string.Format(Database.Resources.RetrieveArtifactTypeName, this.ActiveArtifact.ArtifactTypeID.ToString());
				result = Helper.GetDBContext(Helper.GetActiveCaseID()).ExecuteSqlStatementAsScalar(sql);
				if ((result != null)) {
					if (!System.DBNull.Value.Equals(result)) {
						retVal = Convert.ToString(result);
					}
				}
				return retVal;
			} catch (Exception ex) {
				throw;
			}
		}

	}
}
