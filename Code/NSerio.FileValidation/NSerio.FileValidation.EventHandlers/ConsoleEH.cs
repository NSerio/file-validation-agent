using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using kCura.EventHandler;
using kCura.Relativity.Client;
using Relativity.API;
namespace NSerio.FileValidation.EventHandlers
{
    public class ConsoleEH : kCura.EventHandler.ConsoleEventHandler, kCura.EventHandler.IDataEnabled
    {
        #region " Vars "
        private kCura.EventHandler.Console _settingsConsole = new kCura.EventHandler.Console();
        private ConsoleButton submit = new ConsoleButton();
        private ConsoleButton retry = new ConsoleButton();
        private ConsoleButton cancel = new ConsoleButton();
        private ConsoleLinkButton refresh = new ConsoleLinkButton();
        private int FlagFieldFieldArtifactID;
        private int FlagFieldArtifactID;
        private string FlagFieldColumnName;
        #endregion

        public override kCura.EventHandler.Console GetConsole(PageEvent pageEvent)
        {
            string status = RetrieveCurrentStatus();

            submit.DisplayText = "Submit";

            if (status == MainApp.Helper.Constant.JOB_STATUS_NEW)
                submit.Enabled = true;
            else
                submit.Enabled = false;
            submit.Name = "submit";
            submit.RaisesPostBack = true;
            submit.ToolTip = "Click here to submit the job.";

            retry.DisplayText = "Retry/Resolve Errors";
            if (status == MainApp.Helper.Constant.JOB_STATUS_ERROR)
            {
                retry.Enabled = true;
            }
            else
            {
                retry.Enabled = false;
            }
            retry.Name = "retry";
            retry.RaisesPostBack = true;
            retry.ToolTip = "Click here to re-submit the request and resolve any errors.";

            cancel.DisplayText = "Cancel";
            if (status == MainApp.Helper.Constant.JOB_STATUS_IN_PROGRESS || status == MainApp.Helper.Constant.JOB_STATUS_WAITING)
            {
                cancel.Enabled = true;
            }
            else
            {
                cancel.Enabled = false;
            }
            cancel.Name = "cancel";
            cancel.RaisesPostBack = true;
            cancel.ToolTip = "Click here to cancel the request.";

            _settingsConsole.Items = new List<IConsoleItem>();
            _settingsConsole.Items.Add(submit);
            _settingsConsole.Items.Add(cancel);
            _settingsConsole.Items.Add(retry);
            refresh = _settingsConsole.AddRefreshLinkToConsole();
            refresh.Enabled = true;

            _settingsConsole.Title = "File Validation";
            return _settingsConsole;
        }

        public override void OnButtonClick(ConsoleButton consoleButton)
        {
            switch (consoleButton.Name)
            {
                case "submit":
                case "retry":
                case "cancel":

                    //Retrieve flag field 
                    FlagFieldFieldArtifactID = RetrieveFieldArtifactID(MainApp.Helper.Constant.FIELD_JOB_MISSING_FILE_INDICATOR_GUID);
                    FlagFieldArtifactID = (int)this.ActiveArtifact.Fields[FlagFieldFieldArtifactID].Value.Value;
                    RetrieveColumnName();
                    break;
            }

            switch (consoleButton.Name)
            {
                case "submit":
                    HandleSubmit();
                    break;
                case "retry":
                    HandleRetry();
                    break;
                case "cancel":
                    HandleCancel();
                    break;
            }
        }

        private void HandleSubmit()
        {
            try
            {
                InsertIntoQueue();
                SetJobToWaiting();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void HandleRetry()
        {
            bool doesRecordExist = false;

            try
            {
                doesRecordExist = DoesRecordExistInQueue();
                if (doesRecordExist == false)
                {
                    InsertIntoQueue();
                }
                else
                {
                    UpdateQueue();
                }

                SetJobToWaiting();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void HandleCancel()
        {
            string sql = null;

            try
            {
                sql = string.Format(Database.Resources.RemoveFromCasesQueue, this.Application.ArtifactID.ToString(), this.ActiveArtifact.ArtifactID.ToString());
                Helper.GetDBContext(-1).ExecuteNonQuerySQLStatement(sql);
                sql = string.Format(Database.Resources.RemoveFromDocumentQueue, this.Application.ArtifactID.ToString(), this.ActiveArtifact.ArtifactID.ToString());
                Helper.GetDBContext(-1).ExecuteNonQuerySQLStatement(sql);
                SetJobToNew();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private bool DoesRecordExistInQueue()
        {
            string sql = null;
            object result = null;

            try
            {
                sql = string.Format(Database.Resources.DoesRecordExist, this.Application.ArtifactID, this.ActiveArtifact.ArtifactID);
                result = Helper.GetDBContext(-1).ExecuteSqlStatementAsScalar(sql);
                if (!System.DBNull.Value.Equals(result))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void UpdateQueue()
        {
            string sql = null;

            try
            {
                sql = string.Format(Database.Resources.UpdateCasesQueue, "0", Application.ArtifactID, ActiveArtifact.ArtifactID);
                Helper.GetDBContext(-1).ExecuteNonQuerySQLStatement(sql);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void InsertIntoQueue()
        {
            string sql = null;

            try
            {
                sql = string.Format(Database.Resources.InsertIntoCasesQueue, Application.ArtifactID.ToString(), ActiveArtifact.ArtifactID.ToString(), FlagFieldColumnName);
                Helper.GetDBContext(-1).ExecuteNonQuerySQLStatement(sql);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void SetJobToWaiting()
        {
            try
            {
                using (IRSAPIClient proxy = Helper.GetServicesManager().CreateProxy<IRSAPIClient>(ExecutionIdentity.System))
                {
                    proxy.APIOptions.WorkspaceID = Helper.GetActiveCaseID();
                    MainApp.RSAPI.JobQuery.UpdateStatus(proxy, ActiveArtifact.ArtifactID, MainApp.Helper.Constant.JOB_STATUS_WAITING);
                    MainApp.RSAPI.JobQuery.UpdateErrorMessage(proxy, ActiveArtifact.ArtifactID, "");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void SetJobToNew()
        {
            try
            {
                using (IRSAPIClient proxy = Helper.GetServicesManager().CreateProxy<IRSAPIClient>(ExecutionIdentity.System))
                {
                    proxy.APIOptions.WorkspaceID = Helper.GetActiveCaseID();
                    MainApp.RSAPI.JobQuery.UpdateStatus(proxy, ActiveArtifact.ArtifactID, MainApp.Helper.Constant.JOB_STATUS_NEW);
                    MainApp.RSAPI.JobQuery.UpdateErrorMessage(proxy, ActiveArtifact.ArtifactID, string.Format("Job cancelled at {0}", DateTime.Now));
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void RetrieveColumnName()
        {
            string sql = null;
            object result = null;

            try
            {
                sql = string.Format(Database.Resources.RetrieveColumnName, FlagFieldArtifactID.ToString());
                result = Helper.GetDBContext(Helper.GetActiveCaseID()).ExecuteSqlStatementAsScalar(sql);
                if ((result != null))
                {
                    if (!System.DBNull.Value.Equals(result))
                    {
                        FlagFieldColumnName = (Convert.ToString(result));
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private int RetrieveFieldArtifactID(string fieldGuid)
        {
            string sql = null;
            object result = null;
            int value = 0;
            try
            {
                sql = string.Format(Database.Resources.RetrieveArtifactIDByGuid, fieldGuid);
                result = Helper.GetDBContext(Helper.GetActiveCaseID()).ExecuteSqlStatementAsScalar(sql);
                if ((result != null))
                {
                    if (!System.DBNull.Value.Equals(result))
                    {
                        value = ((int)result);
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }

            return value;
        }

        private string RetrieveCurrentStatus()
        {
            int statusFieldArtifactID = default(int);
            string value = null;

            try
            {
                statusFieldArtifactID = (int)Helper.GetDBContext(Helper.GetActiveCaseID()).ExecuteSqlStatementAsScalar(string.Format(Database.Resources.RetrieveArtifactIDByGuid, MainApp.Helper.Constant.FIELD_JOB_STATUS_GUID));

                if (!ActiveArtifact.Fields[statusFieldArtifactID].Value.IsNull)
                {
                    value = ActiveArtifact.Fields[statusFieldArtifactID].Value.Value.ToString();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return value;
        }

        public override FieldCollection RequiredFields
        {
            get
            {
                FieldCollection fieldCollection = new FieldCollection();
                fieldCollection.Add(new kCura.EventHandler.Field(MainApp.Helper.Constant.FIELD_JOB_STATUS_GUID));
                fieldCollection.Add(new kCura.EventHandler.Field(MainApp.Helper.Constant.FIELD_JOB_MISSING_FILE_INDICATOR_GUID));
                return fieldCollection;
            }
        }

    }
}
