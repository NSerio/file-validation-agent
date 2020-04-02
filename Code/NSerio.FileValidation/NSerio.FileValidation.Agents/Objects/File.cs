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
	public class File
	{

		#region " Vars "
		private Document _Document;
		private string _Guid;
		public string _Location;
		public string _Type;
			#endregion
		private Relativity.API.IDBContext _WorkspaceDBConnection;

		#region " Properties "
		public bool _IsMissing { get; set; }
		#endregion

		#region " Constructors "
		public File(System.Data.DataRow fileRow, Document document, Relativity.API.IDBContext workspaceDBConnection)
		{
			try {
				_Document = document;
				_Guid = Convert.ToString(fileRow["FileGuid"]);
				_Location = Convert.ToString(fileRow["Location"]);
				_Type = Convert.ToString(fileRow["FileType"]);
				_WorkspaceDBConnection = workspaceDBConnection;
			} catch (Exception ex) {
				throw ex;
			}
		}
		#endregion

		#region " Public Methods "
		public void Validate()
		{
			try {
				if (!System.IO.File.Exists(_Location)) {
					_IsMissing = true;
					InsertMissingRecords();
				} else {
					_IsMissing = false;
				}
			} catch (Exception ex) {
				throw ex;
			}
		}
		#endregion

		#region " Private Methods "
		private void InsertMissingRecords()
		{
			try {
				Query.InsertMissingRecords(_WorkspaceDBConnection, _Document.ArtifactID, _Location, _Type);
			} catch (Exception ex) {
				throw ex;
			}
		}
		#endregion

	}
}
