// <copyright file="DeltekSqlQueries.cs" company="JPP Consulting">
// Copyright (c) JPP Consulting. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Jpp.Etl.Service.Models;
using Microsoft.Extensions.Configuration;

namespace Jpp.Etl.Service
{
    internal class DeltekSqlQueries
    {
        private readonly IConfiguration configuration;

        public DeltekSqlQueries(IConfiguration config)
        {
            this.configuration = config;
        }

        public List<Project> ProjectsModifiedSince(DateTime dateTime)
        {
            var connStr = this.configuration["CONNECTION_STRING"];
            if (string.IsNullOrEmpty(connStr))
            {
                throw new ArgumentNullException(nameof(connStr));
            }

            using var connection = new SqlConnection(connStr);
            var command = new SqlCommand { Connection = connection };

            var dataSet = new DataSet("Projects");
            var list = new List<Project>();

            command.Parameters.Add("@minimum", SqlDbType.DateTime).Value = Project.MinimumDateTime.ToString("s");
            command.Parameters.Add("@modified", SqlDbType.DateTime).Value = dateTime.ToString("s");
            command.CommandText = "SELECT * FROM EXVW_Project_Data WHERE Project_Code LIKE '[0-9]%' AND ISNULL(Last_Update_Time, @minimum) >= @modified";

            var adapter = new SqlDataAdapter { SelectCommand = command };
            adapter.Fill(dataSet);

            foreach (DataRow? row in dataSet.Tables[0].Rows)
            {
                if (row is null)
                {
                    continue;
                }

                list.Add(new Project(row));
            }

            return list;
        }
    }
}
