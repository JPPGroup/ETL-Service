// <copyright file="DeltekProjectImporter.cs" company="JPP Consulting">
// Copyright (c) JPP Consulting. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Security.Authentication;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Jpp.Etl.Service.Projects
{
    internal class DeltekProjectImporter
    {
        private readonly ILogger<DeltekProjectImporter> logger;
        private readonly DeltekProjectService service;

        private DateTimeOffset? lastCompletedImport;

        public DeltekProjectImporter(ILogger<DeltekProjectImporter> logger, DeltekProjectService service)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public async Task DoScanAndImportAsync()
        {
            var started = DateTimeOffset.Now;

            await this.SetLastImportAsync();

            this.logger.LogInformation($"Finding projects modified since {this.lastCompletedImport}.");
            var projects = await this.GetProjectsAsync();

            if (projects.Count > 0)
            {
                this.logger.LogInformation($"Projects found : {projects.Count}. Attempting to import.");
                await this.ImportProjectsAsync(projects, started);
            }
            else
            {
                this.logger.LogInformation("No projects found.");
            }
        }

        private async Task SetLastImportAsync()
        {
            if (!this.lastCompletedImport.HasValue)
            {
                this.lastCompletedImport = await this.TryGetLastImportAsync();
                if (!this.lastCompletedImport.HasValue)
                {
                    this.logger.LogWarning("No last import date time set.");
                }
            }
        }

        private async Task<DateTime?> TryGetLastImportAsync()
        {
            try
            {
                return await this.service.GetLastImportAsync();
            }
            catch (AuthenticationException authException)
            {
                this.logger.LogError(authException, "Failed Auth GetLastImportAsync.");
                return null;
            }
        }

        private async Task<List<DeltekProject>> GetProjectsAsync()
        {
            if (this.lastCompletedImport.HasValue)
            {
                return await this.TryGetProjectsAsync(this.lastCompletedImport.Value);
            }

            this.logger.LogWarning("Last run not set.");
            return new List<DeltekProject>();
        }

        private async Task<List<DeltekProject>> TryGetProjectsAsync(DateTimeOffset modifiedDate)
        {
            var list = new List<DeltekProject>();
            try
            {
                list = await this.service.GetProjectsAsync(modifiedDate);
            }
            catch (SqlException ex)
            {
                this.logger.LogError(ex, "Unable to get projects.");
            }

            return list;
        }

        private async Task ImportProjectsAsync(List<DeltekProject> projects, DateTimeOffset started)
        {
            var result = await this.TryImportProjectsAsync(projects, started);

            if (result)
            {
                this.logger.LogInformation("Projects import successful.");
                this.lastCompletedImport = started;
            }
            else
            {
                this.logger.LogWarning("Projects import failed.");
            }
        }

        private async Task<bool> TryImportProjectsAsync(List<DeltekProject> projects, DateTimeOffset started)
        {
            try
            {
                return await this.service.ImportProjectsAsync(projects, started);
            }
            catch (AuthenticationException authException)
            {
                this.logger.LogError(authException, "Failed Auth GetLastImportAsync.");
                return false;
            }
        }
    }
}
