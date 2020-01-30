// <copyright file="DeltekProjectImporter.cs" company="JPP Consulting">
// Copyright (c) JPP Consulting. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Jpp.Etl.Service.Projects
{
    internal class DeltekProjectImporter
    {
        private readonly ILogger logger;
        private readonly DeltekProjectService service;

        private DateTimeOffset? lastCompletedImport;

        public DeltekProjectImporter(ILogger logger, DeltekProjectService service)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public async Task DoScanAndImportAsync(CancellationToken cancellationToken)
        {
            var started = DateTimeOffset.Now;

            await this.SetLastImportAsync();
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var projects = await this.GetProjectsAsync();
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await this.DoProjectsImportAsync(projects, started);
        }

        private async Task DoProjectsImportAsync(List<DeltekProject> projects, DateTimeOffset started)
        {
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
                this.lastCompletedImport = await this.service.GetLastImportAsync();
                if (!this.lastCompletedImport.HasValue)
                {
                    this.logger.LogWarning("No last import date time set.");
                }
            }
        }

        private async Task<List<DeltekProject>> GetProjectsAsync()
        {
            if (this.lastCompletedImport.HasValue)
            {
                this.logger.LogInformation($"Finding projects modified since {this.lastCompletedImport}.");
                return await this.service.GetProjectsAsync(this.lastCompletedImport.Value);
            }

            this.logger.LogWarning("Last run not set.");
            return new List<DeltekProject>();
        }

        private async Task ImportProjectsAsync(List<DeltekProject> projects, DateTimeOffset started)
        {
            var result = await this.service.ImportProjectsAsync(projects, started);

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
    }
}
