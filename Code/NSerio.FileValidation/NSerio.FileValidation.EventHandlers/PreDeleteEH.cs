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
	public class PreDeleteEH : kCura.EventHandler.PreDeleteEventHandler, kCura.EventHandler.IDataEnabled
	{

		public override void Commit()
		{
		}

		public override kCura.EventHandler.Response Execute()
		{
			kCura.EventHandler.Response response = new kCura.EventHandler.Response();
			string sql = null;
			response.Success = true;
			response.Message = "";

			try {
				sql = string.Format(Database.Resources.RemoveFromCasesQueue, this.Application.ArtifactID.ToString(), this.ActiveArtifact.ArtifactID.ToString());
				Helper.GetDBContext(-1).ExecuteNonQuerySQLStatement(sql);
				sql = string.Format(Database.Resources.RemoveFromDocumentQueue, this.Application.ArtifactID.ToString(), this.ActiveArtifact.ArtifactID.ToString());
				Helper.GetDBContext(-1).ExecuteNonQuerySQLStatement(sql);
			} catch (Exception ex) {
				response.Success = false;
				response.Message = ex.Message;
			}

			return response;
		}

		public override kCura.EventHandler.FieldCollection RequiredFields {
			get { return new kCura.EventHandler.FieldCollection(); }
		}


		public override void Rollback()
		{
		}
	}
}
