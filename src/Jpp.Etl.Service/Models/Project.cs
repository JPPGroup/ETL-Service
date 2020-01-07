using System;
using System.Data;

namespace Jpp.Etl.Service.Models
{
    internal class Project
    {
        private const string FIELD_NAME_EXTERNAL_ID = "Project_ID";
        private const string FIELD_NAME_CODE = "Project_Code";
        private const string FIELD_NAME_NAME = "Name";
        private const string FIELD_NAME_DESCRIPTION = "Description";
        private const string FIELD_NAME_OFFICE = "Town";
        private const string FIELD_NAME_LEAD = "Form_Of_Contract";
        private const string FIELD_NAME_DISCIPLINE = "Project_Category";
        private const string FIELD_NAME_CREATED = "Created_Date";
        private const string FIELD_NAME_MODIFIED = "Last_Update_Time";
        private const string FIELD_NAME_STATUS = "Project_Status";

        private readonly DataRow _row;

        public static readonly DateTime MinimumDateTime = new DateTime(1900, 1, 1);

        public string ImportId => !DBNull.Value.Equals(_row[FIELD_NAME_EXTERNAL_ID]) ? ((int)_row[FIELD_NAME_EXTERNAL_ID]).ToString() : string.Empty;
        public string Code => !DBNull.Value.Equals(_row[FIELD_NAME_CODE]) ? (string)_row[FIELD_NAME_CODE] : string.Empty;
        public string Name => !DBNull.Value.Equals(_row[FIELD_NAME_NAME]) ? (string)_row[FIELD_NAME_NAME] : string.Empty;
        public string Description => !DBNull.Value.Equals(_row[FIELD_NAME_DESCRIPTION])? (string)_row[FIELD_NAME_DESCRIPTION] : string.Empty;
        public string Office => !DBNull.Value.Equals(_row[FIELD_NAME_OFFICE]) ? (string)_row[FIELD_NAME_OFFICE] : string.Empty;
        public string Lead => !DBNull.Value.Equals(_row[FIELD_NAME_LEAD]) ? (string)_row[FIELD_NAME_LEAD] : string.Empty;
        public string Discipline => !DBNull.Value.Equals(_row[FIELD_NAME_DISCIPLINE]) ? (string)_row[FIELD_NAME_DISCIPLINE] : string.Empty;
        public string Status => !DBNull.Value.Equals(_row[FIELD_NAME_STATUS]) ? (string)_row[FIELD_NAME_STATUS] : string.Empty;
        public DateTime CreatedDateTime => !DBNull.Value.Equals(_row[FIELD_NAME_CREATED]) ? (DateTime)_row[FIELD_NAME_CREATED] : MinimumDateTime;
        public DateTime ModifiedDateTime => !DBNull.Value.Equals(_row[FIELD_NAME_MODIFIED]) ? (DateTime)_row[FIELD_NAME_MODIFIED] : MinimumDateTime;

        public Project (DataRow row)
        {
            _row = row;
        }
    }
}
