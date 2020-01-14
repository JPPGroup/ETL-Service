// <copyright file="DeltekProjectTask.cs" company="JPP Consulting">
// Copyright (c) JPP Consulting. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Jpp.Etl.Service.Projects
{
    internal class DeltekProjectTask : IScheduledTask
    {
        private readonly IConfiguration configuration;
        private readonly DeltekProjectImporter importer;

        public DeltekProjectTask(IConfiguration configuration, DeltekProjectImporter importer)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.importer = importer ?? throw new ArgumentNullException(nameof(importer));
        }

        public int IntervalMinutes => this.GetIntervalMinutes();

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await this.importer.DoScanAndImportAsync();
                await Task.Delay(TimeSpan.FromMinutes(this.IntervalMinutes), cancellationToken);
            }
        }

        private int GetIntervalMinutes()
        {
            var intervalMinutes = 5;
            if (int.TryParse(this.configuration["PROJECTS_INTERVAL_MINUTES"], out var parsedResult))
            {
                intervalMinutes = parsedResult;
            }

            return intervalMinutes;
        }
    }
}
