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
	public class Query
	{

		public static void InsertMissingRecords(Relativity.API.IDBContext databaseConnection, int documentArtifactID, string filePath, string fileType)
		{
			string sql = Database.Resources.InsertMissingRecords;
			System.Data.SqlClient.SqlCommand sqlcommand = new System.Data.SqlClient.SqlCommand(sql, databaseConnection.GetConnection());

			sqlcommand.Parameters.Add("@DocumentArtifactID", SqlDbType.Int);
			sqlcommand.Parameters["@DocumentArtifactID"].Value = documentArtifactID;

			sqlcommand.Parameters.AddWithValue("@FilePath", SqlDbType.VarChar);
			sqlcommand.Parameters["@FilePath"].Value = filePath;

			sqlcommand.Parameters.AddWithValue("@FileType", SqlDbType.VarChar);
			sqlcommand.Parameters["@FileType"].Value = fileType;


			try {
				sqlcommand.ExecuteNonQuery();
			} catch (Exception ex) {
				throw;
			} finally {
				//No need to open or close Connection because the API takes care of it automatically
			}
		}


	}
}
