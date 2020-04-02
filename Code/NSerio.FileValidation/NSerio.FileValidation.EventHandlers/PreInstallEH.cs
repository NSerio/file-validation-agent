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
namespace NSerio.FileValidation.EventHandlers
{

    [Guid("C02865B6-E33B-4539-AA6F-3E454B8FF01D")]
    [kCura.EventHandler.CustomAttributes.Description("Creates the underlying tables needed for File Validation.")]
    [kCura.EventHandler.CustomAttributes.RunOnce(false)]
    public class PreInstallEH : kCura.EventHandler.PreInstallEventHandler
    {

        public override kCura.EventHandler.Response Execute()
        {
            kCura.EventHandler.Response response = new kCura.EventHandler.Response();
            response.Success = true;
            response.Message = "";

            try
            {
                Helper.GetDBContext(-1).ExecuteNonQuerySQLStatement(Database.Resources.CreateCasesQueueTable);
                Helper.GetDBContext(-1).ExecuteNonQuerySQLStatement(Database.Resources.CreateIndexOnCasesQueue);
                Helper.GetDBContext(-1).ExecuteNonQuerySQLStatement(Database.Resources.CreateDocumentQueueTable);
                Helper.GetDBContext(-1).ExecuteNonQuerySQLStatement(Database.Resources.CreateIndexOnDocumentQueue);
                Helper.GetDBContext(Helper.GetActiveCaseID()).ExecuteNonQuerySQLStatement(Database.Resources.CreateMissingFilesTable);

            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }

            return response;
        }
    }
}
