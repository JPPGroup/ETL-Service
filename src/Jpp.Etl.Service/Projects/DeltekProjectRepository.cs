// <copyright file="DeltekProjectRepository.cs" company="JPP Consulting">
// Copyright (c) JPP Consulting. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Jpp.Etl.Service.Projects
{
    internal class DeltekProjectRepository
    {
        private readonly IConfiguration configuration;

        public DeltekProjectRepository(IConfiguration configuration)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public List<DeltekProject> GetProjectList(DateTimeOffset modifiedSince)
        {
            var connStr = this.configuration["DELTEK_CONN_STR"];

            using var connection = new SqlConnection(connStr);
            var command = CreateProjectListSqlCommand(modifiedSince);
            command.Connection = connection;

            var dataSet = new DataSet("Projects");

            var adapter = new SqlDataAdapter { SelectCommand = command };
            adapter.Fill(dataSet);

            return BuildProjectList(dataSet);
        }

        private static SqlCommand CreateProjectListSqlCommand(DateTimeOffset modifiedSince)
        {
            var defaultDate = new DateTime(1900, 1, 1);
            var command = new SqlCommand();
            command.Parameters.Add("@default", SqlDbType.DateTime).Value = defaultDate.ToString("s");
            command.Parameters.Add("@modified", SqlDbType.DateTime).Value = modifiedSince.ToString("s");
            command.CommandText = "SELECT * FROM EXVW_Project_Data WHERE Project_Code LIKE '[0-9]%' AND ISNULL(Last_Update_Time, @default) >= @modified";
            return command;
        }

        private static List<DeltekProject> BuildProjectList(DataSet dataSet)
        {
            var list = new List<DeltekProject>();
            foreach (DataRow? row in dataSet.Tables[0].Rows)
            {
                if (row is null)
                {
                    continue;
                }

                list.Add(new DeltekProject(row));
            }

            return list;
        }
    }
}
