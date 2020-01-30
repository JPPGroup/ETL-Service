// <copyright file="Program.cs" company="JPP Consulting">
// Copyright (c) JPP Consulting. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Unity;
using Unity.Microsoft.Logging;

namespace Jpp.Etl.Service
{
    /// <summary>
    /// Console program from executing long running/schduled tasks.
    ///
    /// NB: currently intentionally not multi thread. Any scheduled task that aren't fully asynchronous will block.
    /// As and when multi threading is implemented, manual stop/start/restart of tasks needs so to be implemented.
    /// </summary>
    internal class Program
    {
        private static readonly IUnityContainer Container = new UnityContainer();

        public static void Main(string[] args)
        {
            var loggerFactory = CreateLoggerFactory(args);

            Container.RegisterInstance(CreateConfiguration());
            Container.AddExtension(new LoggingExtension(loggerFactory));

            var tasks = Assembly.GetExecutingAssembly().GetTypes()
                .Where(x => typeof(IScheduledTask).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract);

            StartTasks(tasks);

            Console.ReadLine();
        }

        private static void StartTasks(IEnumerable<Type> tasks)
        {
            var tokenSource = new CancellationTokenSource();
            foreach (var task in tasks)
            {
                StartTask(task, tokenSource.Token);
            }
        }

        private static void StartTask(Type type, CancellationToken cancellationToken)
        {
            var instance = (IScheduledTask)Container.Resolve(type);
            instance.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        }

        private static IConfiguration CreateConfiguration()
        {
            var config = new ConfigurationBuilder();
            config.AddEnvironmentVariables("ETL_");

            return config.Build();
        }

        private static ILoggerFactory CreateLoggerFactory(string[] args)
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                Parser.Default.ParseArguments<Options>(args).WithParsed(o =>
                {
                    if (!o.Verbose)
                    {
                        builder.SetMinimumLevel(LogLevel.Warning);
                    }
                });
                builder.AddConsole();
            });
            return loggerFactory;
        }
    }
}
