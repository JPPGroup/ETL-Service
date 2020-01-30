// <copyright file="CommonBase.cs" company="JPP Consulting">
// Copyright (c) JPP Consulting. All rights reserved.
// </copyright>

using System;
using Microsoft.Extensions.Logging;

namespace Jpp.Etl.Service
{
    internal abstract class CommonBase<T>
    {
        private readonly CommonServices<T> commonServices;

        protected CommonBase(CommonServices<T> commonServices)
        {
            this.commonServices = commonServices ?? throw new ArgumentNullException(nameof(commonServices));
        }

        protected string GetConfiguration(string item) => this.commonServices.Configuration[item];

        protected void LogInformation(string message) => this.commonServices.Logger.LogInformation(message);

        protected void LogWarning(string message) => this.commonServices.Logger.LogWarning(message);

        protected void LogError(string message) => this.commonServices.Logger.LogError(message);

        protected void LogError(Exception exception, string message) => this.commonServices.Logger.LogError(exception, message);
    }
}
