// <copyright file="DeltekProjectTask.cs" company="JPP Consulting">
// Copyright (c) JPP Consulting. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jpp.Etl.Service.Projects
{
    internal class DeltekProjectTask : CommonBase<DeltekProjectTask>, IScheduledTask
    {
        private readonly DeltekProjectImporter importer;

        public DeltekProjectTask(CommonServices<DeltekProjectTask> commonServices, DeltekProjectImporter importer)
            : base(commonServices)
        {
            this.importer = importer ?? throw new ArgumentNullException(nameof(importer));
        }

        public int IntervalMinutes => this.GetIntervalMinutes();

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await this.importer.DoScanAndImportAsync(cancellationToken);
                await Task.Delay(TimeSpan.FromMinutes(this.IntervalMinutes), cancellationToken);
            }
        }

        private int GetIntervalMinutes()
        {
            var intervalMinutes = 5;
            if (int.TryParse(this.GetConfiguration("PROJECTS_INTERVAL_MINUTES"), out var parsedResult))
            {
                intervalMinutes = parsedResult;
            }

            return intervalMinutes;
        }
    }
}
