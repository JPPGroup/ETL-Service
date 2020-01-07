// <copyright file="IScheduledTask.cs" company="JPP Consulting">
// Copyright (c) JPP Consulting. All rights reserved.
// </copyright>

using System.Threading.Tasks;

namespace Jpp.Etl.Service
{
    /// <summary>
    /// Schedule Task.
    /// </summary>
    internal interface IScheduledTask
    {
        /// <summary>
        /// Start the task.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task StartAsync();
    }
}
