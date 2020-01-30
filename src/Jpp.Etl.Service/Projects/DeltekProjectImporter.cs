// <copyright file="DeltekProjectImporter.cs" company="JPP Consulting">
// Copyright (c) JPP Consulting. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Jpp.Etl.Service.Projects
{
    internal class DeltekProjectImporter : CommonBase<DeltekProjectImporter>
    {
        private readonly DeltekProjectService service;

        private DateTimeOffset? lastCompletedImport;

        public DeltekProjectImporter(CommonServices<DeltekProjectImporter> commonServices, DeltekProjectService service)
            : base(commonServices)
        {
            this.service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public async Task DoScanAndImportAsync(CancellationToken cancellationToken)
        {
            var started = DateTimeOffset.Now;

            var isDateSet = await this.SetLastImportAsync();
            if (!isDateSet || cancellationToken.IsCancellationRequested)
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
                this.LogInformation($"Projects found : {projects.Count}. Attempting to import.");
                await this.ImportProjectsAsync(projects, started);
            }
            else
            {
                this.LogInformation("No projects found.");
            }
        }

        private async Task<bool> SetLastImportAsync()
        {
            if (!this.lastCompletedImport.HasValue)
            {
                this.lastCompletedImport = await this.service.GetLastImportAsync();
                if (!this.lastCompletedImport.HasValue)
                {
                    this.LogWarning("No last import date time set.");
                    return false;
                }
            }

            return true;
        }

        private async Task<List<DeltekProject>> GetProjectsAsync()
        {
            if (this.lastCompletedImport.HasValue)
            {
                var lastRun = this.lastCompletedImport.Value;
                this.LogInformation($"Finding projects modified since {lastRun}.");
                return await this.service.GetProjectsAsync(lastRun);
            }

            this.LogWarning("Last run not set.");
            return new List<DeltekProject>();
        }

        private async Task ImportProjectsAsync(List<DeltekProject> projects, DateTimeOffset started)
        {
            var result = await this.service.ImportProjectsAsync(projects, started);

            if (result)
            {
                this.LogInformation("Projects import successful.");
                this.lastCompletedImport = started;
            }
            else
            {
                this.LogWarning("Projects import failed.");
            }
        }
    }
}
