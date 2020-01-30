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

        private bool TryGetRowValue(string field, out object rowValue)
        {
            rowValue = this.row[field];
            return !DBNull.Value.Equals(rowValue);
        }

        private string GetDiscipline()
        {
            return this.TryGetRowValue("Project_Category", out var rowValue) ? (string)rowValue : string.Empty;
        }

        private string GetStatus()
        {
            return this.TryGetRowValue("Project_Status", out var rowValue) ? (string)rowValue : string.Empty;
        }

        private DateTime GetCreatedDateTime()
        {
            return this.TryGetRowValue("Created_Date", out var rowValue) ? (DateTime)rowValue : DefaultDateTime;
        }

        private DateTime GetModifiedDateTime()
        {
            return this.TryGetRowValue("Last_Update_Time", out var rowValue) ? (DateTime)rowValue : DefaultDateTime;
        }

        private string GetImportId()
        {
            return this.TryGetRowValue("Project_ID", out var rowValue) ? ((int)rowValue).ToString() : string.Empty;
        }

        private string GetCode()
        {
            return this.TryGetRowValue("Project_Code", out var rowValue) ? (string)rowValue : string.Empty;
        }

        private string GetName()
        {
            return this.TryGetRowValue("Name", out var rowValue) ? (string)rowValue : string.Empty;
        }

        private string GetDescription()
        {
            return this.TryGetRowValue("Description", out var rowValue) ? (string)rowValue : string.Empty;
        }

        private string GetOffice()
        {
            return this.TryGetRowValue("Town", out var rowValue) ? (string)rowValue : string.Empty;
        }

        private string GetLead()
        {
            return this.TryGetRowValue("Form_Of_Contract", out var rowValue) ? (string)rowValue : string.Empty;
        }
    }
}
