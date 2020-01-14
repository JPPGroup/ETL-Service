// <copyright file="DeltekProject.cs" company="JPP Consulting">
// Copyright (c) JPP Consulting. All rights reserved.
// </copyright>

using System;
using System.Data;

namespace Jpp.Etl.Service.Projects
{
    internal class DeltekProject
    {
        private static readonly DateTime DefaultDateTime = new DateTime(1900, 1, 1);

        private readonly DataRow row;

        public DeltekProject(DataRow row)
        {
            this.row = row ?? throw new ArgumentNullException(nameof(row));
        }

        public string ImportId => this.GetImportId();

        public string Code => this.GetCode();

        public string Name => this.GetName();

        public string Description => this.GetDescription();

        public string Office => this.GetOffice();

        public string Lead => this.GetLead();

        public string Discipline => this.GetDiscipline();

        public string Status => this.GetStatus();

        public DateTime CreatedDateTime => this.GetCreatedDateTime();

        public DateTime ModifiedDateTime => this.GetModifiedDateTime();

        private string GetDiscipline()
        {
            const string fieldDiscipline = "Project_Category";

            return !DBNull.Value.Equals(this.row[fieldDiscipline])
                ? (string)this.row[fieldDiscipline]
                : string.Empty;
        }

        private string GetStatus()
        {
            const string fieldStatus = "Project_Status";

            return !DBNull.Value.Equals(this.row[fieldStatus])
                ? (string)this.row[fieldStatus]
                : string.Empty;
        }

        private DateTime GetCreatedDateTime()
        {
            const string fieldCreated = "Created_Date";

            return !DBNull.Value.Equals(this.row[fieldCreated])
                ? (DateTime)this.row[fieldCreated]
                : DefaultDateTime;
        }

        private DateTime GetModifiedDateTime()
        {
            const string fieldModified = "Last_Update_Time";

            return !DBNull.Value.Equals(this.row[fieldModified])
                ? (DateTime)this.row[fieldModified]
                : DefaultDateTime;
        }

        private string GetImportId()
        {
            const string fieldExternalId = "Project_ID";

            return !DBNull.Value.Equals(this.row[fieldExternalId])
                ? ((int)this.row[fieldExternalId]).ToString()
                : string.Empty;
        }

        private string GetCode()
        {
            const string fieldCode = "Project_Code";

            return !DBNull.Value.Equals(this.row[fieldCode])
                ? (string)this.row[fieldCode]
                : string.Empty;
        }

        private string GetName()
        {
            const string fieldName = "Name";

            return !DBNull.Value.Equals(this.row[fieldName])
                ? (string)this.row[fieldName]
                : string.Empty;
        }

        private string GetDescription()
        {
            const string fieldDescription = "Description";

            return !DBNull.Value.Equals(this.row[fieldDescription])
                ? (string)this.row[fieldDescription]
                : string.Empty;
        }

        private string GetOffice()
        {
            const string fieldOffice = "Town";

            return !DBNull.Value.Equals(this.row[fieldOffice])
                ? (string)this.row[fieldOffice]
                : string.Empty;
        }

        private string GetLead()
        {
            const string fieldLead = "Form_Of_Contract";

            return !DBNull.Value.Equals(this.row[fieldLead])
                ? (string)this.row[fieldLead]
                : string.Empty;
        }
    }
}
