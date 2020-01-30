// <copyright file="CommonServices.cs" company="JPP Consulting">
// Copyright (c) JPP Consulting. All rights reserved.
// </copyright>

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Jpp.Etl.Service
{
    internal class CommonServices<T>
    {
        public CommonServices(ILogger<T> logger, IConfiguration configuration)
        {
            this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public ILogger<T> Logger { get; }

        public IConfiguration Configuration { get; }
    }
}
