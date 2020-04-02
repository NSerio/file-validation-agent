using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using kCura.Relativity.Client;
namespace NSerio.FileValidation.MainApp.RSAPI
{
    public class JobQuery
    {
        public static void UpdateStatus(IRSAPIClient connection, int artifactID, string status)
        {
            kCura.Relativity.Client.DTOs.RDO job = new kCura.Relativity.Client.DTOs.RDO(new Guid(Helper.Constant.OBJECT_TYPE_FILE_VALIDATION_GUID), artifactID);
            job.Fields.Add(new kCura.Relativity.Client.DTOs.FieldValue(new Guid(Helper.Constant.FIELD_JOB_STATUS_GUID), status));
            kCura.Relativity.Client.DTOs.ResultSet<kCura.Relativity.Client.DTOs.RDO> updateResults = new kCura.Relativity.Client.DTOs.ResultSet<kCura.Relativity.Client.DTOs.RDO>();

            try
            {
                updateResults = connection.Repositories.RDO.Update(job);
                if (!updateResults.Success)
                {
                    throw new Exception(FormatError(updateResults));
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public static void UpdateErrorMessage(IRSAPIClient connection, int artifactID, string errorMessage)
        {
            kCura.Relativity.Client.DTOs.RDO job = new kCura.Relativity.Client.DTOs.RDO(new Guid(Helper.Constant.OBJECT_TYPE_FILE_VALIDATION_GUID), artifactID);
            job.Fields.Add(new kCura.Relativity.Client.DTOs.FieldValue(new Guid(Helper.Constant.FIELD_JOB_ERROR_MESSAGE_GUID), errorMessage));
            kCura.Relativity.Client.DTOs.ResultSet<kCura.Relativity.Client.DTOs.RDO> updateResults = new kCura.Relativity.Client.DTOs.ResultSet<kCura.Relativity.Client.DTOs.RDO>();

            try
            {
                updateResults = connection.Repositories.RDO.Update(job);
                if (!updateResults.Success)
                {
                    throw new Exception(FormatError(updateResults));
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public static kCura.Relativity.Client.DTOs.RDO RetrieveJob(IRSAPIClient connection, int artifactID)
        {
            List<kCura.Relativity.Client.DTOs.FieldValue> fields = new List<kCura.Relativity.Client.DTOs.FieldValue>();
            fields.Add(new kCura.Relativity.Client.DTOs.FieldValue(new Guid(Helper.Constant.FIELD_JOB_MISSING_FILE_INDICATOR_GUID)));
            fields.Add(new kCura.Relativity.Client.DTOs.FieldValue(new Guid(Helper.Constant.FIELD_JOB_ERROR_MESSAGE_GUID)));

            WholeNumberCondition condition = null;
            condition = new WholeNumberCondition(artifactID, NumericConditionEnum.EqualTo);

            kCura.Relativity.Client.DTOs.Query<kCura.Relativity.Client.DTOs.RDO> q = new kCura.Relativity.Client.DTOs.Query<kCura.Relativity.Client.DTOs.RDO>
            {
                ArtifactTypeGuid = new Guid(Helper.Constant.OBJECT_TYPE_FILE_VALIDATION_GUID),
                Fields = fields
            };
            kCura.Relativity.Client.DTOs.QueryResultSet<kCura.Relativity.Client.DTOs.RDO> qrs = new kCura.Relativity.Client.DTOs.QueryResultSet<kCura.Relativity.Client.DTOs.RDO>();

            try
            {
                qrs = connection.Repositories.RDO.Query(q);
                if (qrs.Success)
                {
                    if (qrs.Results.Count == 0)
                    {
                        throw new Exception(Helper.Constant.EM_NO_CONFIGURATION);
                    }
                    else
                    {
                        return qrs.Results[0].Artifact;
                    }
                }
                else
                {
                    throw new Exception(FormatError(qrs));
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private static string FormatError(kCura.Relativity.Client.DTOs.ResultSet<kCura.Relativity.Client.DTOs.RDO> resultSet)
        {
            string retVal = string.Empty;

            if (resultSet.Results.Count == 0)
            {
                retVal = resultSet.Message;
            }
            else
            {
                foreach (var result in resultSet.Results)
                {
                    retVal += result.Message + "<br />";
                }
            }

            return retVal;
        }

    }
}
