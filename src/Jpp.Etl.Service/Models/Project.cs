// <copyright file="Project.cs" company="JPP Consulting">
// Copyright (c) JPP Consulting. All rights reserved.
// </copyright>

using System;
using System.Data;

namespace Jpp.Etl.Service.Models
{
    internal class Project
    {
        public static readonly DateTime MinimumDateTime = new DateTime(1900, 1, 1);

        private const string FieldExternalId = "Project_ID";
        private const string FieldCode = "Project_Code";
        private const string FieldName = "Name";
        private const string FieldDescription = "Description";
        private const string FieldOffice = "Town";
        private const string FieldLead = "Form_Of_Contract";
        private const string FieldDiscipline = "Project_Category";
        private const string FieldCreated = "Created_Date";
        private const string FieldModified = "Last_Update_Time";
        private const string FieldStatus = "Project_Status";

        private readonly DataRow row;

        public Project(DataRow row)
        {
            this.row = row;
        }

        public string ImportId => !DBNull.Value.Equals(this.row[FieldExternalId]) ? ((int)this.row[FieldExternalId]).ToString() : string.Empty;

        public string Code => !DBNull.Value.Equals(this.row[FieldCode]) ? (string)this.row[FieldCode] : string.Empty;

        public string Name => !DBNull.Value.Equals(this.row[FieldName]) ? (string)this.row[FieldName] : string.Empty;

        public string Description => !DBNull.Value.Equals(this.row[FieldDescription]) ? (string)this.row[FieldDescription] : string.Empty;

        public string Office => !DBNull.Value.Equals(this.row[FieldOffice]) ? (string)this.row[FieldOffice] : string.Empty;

        public string Lead => !DBNull.Value.Equals(this.row[FieldLead]) ? (string)this.row[FieldLead] : string.Empty;

        public string Discipline => !DBNull.Value.Equals(this.row[FieldDiscipline]) ? (string)this.row[FieldDiscipline] : string.Empty;

        public string Status => !DBNull.Value.Equals(this.row[FieldStatus]) ? (string)this.row[FieldStatus] : string.Empty;

        public DateTime CreatedDateTime => !DBNull.Value.Equals(this.row[FieldCreated]) ? (DateTime)this.row[FieldCreated] : MinimumDateTime;

        public DateTime ModifiedDateTime => !DBNull.Value.Equals(this.row[FieldModified]) ? (DateTime)this.row[FieldModified] : MinimumDateTime;
    }
}
