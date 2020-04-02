using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
namespace NSerio.FileValidation.MainApp.Helper
{
	public class Constant
	{

        #region " Object type guids "
        public const string OBJECT_TYPE_FILE_VALIDATION_GUID = "3E6E3FAB-04BF-428F-B745-B6979840E480";
        public const string OBJECT_TYPE_FILE_VALIDATIOM_RESULT_GUID = "41D525ED-C2AE-4C0D-8D4A-716439837038";
        #endregion

        #region " File Validation Job - field guids "
        public const string FIELD_JOB_STATUS_GUID = "4FE5F197-2983-4160-9569-4E63947D8DBE";
		public const string FIELD_JOB_ERROR_MESSAGE_GUID = "98CA34F2-96C1-4F8F-A32A-9219300F9220";
        public const string FIELD_DOCUMENT_TYPE = "70E285C9-035F-495E-A078-29042E7021CD";
        public const string FIELD_SAVED_SEARCH = "217B4D23-EDD2-4FD5-8C28-A63624D2A1D9";
        public const string FIELD_JOB_MISSING_FILE_INDICATOR_GUID = "FE7E78D0-AEE8-4612-9CDB-F2893D31FC97";
        #endregion

        #region " File Validation Result - field guids "
        public const string FIELD_FR_DOCUMENT = "1179CC65-AD37-40E2-B38E-717F88A38A19";
        public const string FIELD_FR_VALIDATION_DATE = "0C81BA07-C800-419D-9D8D-7269CBA3D9C1";
        public const string FIELD_FR_EXIST = "DA974919-9038-4A4C-9156-AD5954A013AA";
        public const string FIELD_FR_NAME = "32BB747A-459B-47CC-996D-CED07E059899";
        public const string FIELD_FR_TYPE = "8B051D93-48CB-4D3F-B991-FEDA48D951C9";

        #endregion

        #region " Status names "
        public const string JOB_STATUS_NEW = "New";
		public const string JOB_STATUS_WAITING = "Waiting";
		public const string JOB_STATUS_IN_PROGRESS = "In Progress";
        //Public Const JOB_STATUS_COMPLETE = "Complete"
        public const string JOB_STATUS_ERROR = "Error";
        #endregion

        #region " Error Messages "
        public const string EM_NO_CONFIGURATION = "No job exists";
        public const string EM_JOB_ALREADY_EXISTS = "A file validation job already exists";
        #endregion

        #region " File Type "
        public const string FT_NATIVE = "Native";
        public const string FT_IMAGE = "Image";
        public const string FT_PROD_IMAGE = "Produced Image";
        #endregion
        #region " Other "
        public const string DOCUMENT_QUEUE_TABLE_NAME = "eddsdbo.KCD_1035966_FileValidator_DocumentQueue";
        public const string SAVED_SEARCH_NAME = "FILEVALIDATION_SAVEDSEARCH_SELECTION";
        #endregion
    }

}

