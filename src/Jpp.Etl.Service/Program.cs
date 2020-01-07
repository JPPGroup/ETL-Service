// <copyright file="Program.cs" company="JPP Consulting">
// Copyright (c) JPP Consulting. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Unity;
using Unity.Lifetime;

namespace Jpp.Etl.Service
{
    internal enum MessageType
    {
        Info,
        Error,
        Success,
        Warn,
    }

    internal class Program
    {
        private static readonly IUnityContainer Container = new UnityContainer();
        private static Options options = new Options();

        public static async Task Main(string[] args)
        {
            try
            {
                Parser.Default.ParseArguments<Options>(args).WithParsed(o => { options = o; });

                Container.RegisterInstance(CreateConfiguration(), new ContainerControlledLifetimeManager());

                Container.RegisterType<DeltekSqlQueries>();
                Container.RegisterInstance(new CancellationTokenSource());
                Container.RegisterInstance(ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None));

                AppDomain.CurrentDomain.ProcessExit += Event_CurrentDomainProcessExit;
                /* TODO: Not required, tasks run as background threads so auto stop when process is terminated.
                 * Also: https://blogs.msdn.microsoft.com/jmstall/2006/11/26/appdomain-processexit-is-not-guaranteed-to-be-called/
                 */

                var types = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(x => x.GetTypes())
                    .Where(x => typeof(IScheduledTask).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract);

                foreach (var task in types)
                {
                    await StartTaskAsync(task);
                }
            }
            catch (Exception e)
            {
                WriteMessage(e.Message, MessageType.Error);
            }
            finally
            {
                Console.ReadLine();
            }
        }

        public static void WriteMessage(string message, MessageType type = MessageType.Info)
        {
            if (!options.Verbose)
            {
                return;
            }

            Console.ForegroundColor = GetTypeConsoleColor(type);
            Console.WriteLine(message);
            Console.ResetColor();
        }

        public static IEnumerable<List<T>> SplitList<T>(List<T> list, int nSize)
        {
            for (var i = 0; i < list.Count; i += nSize)
            {
                yield return list.GetRange(i, Math.Min(nSize, list.Count - i));
            }
        }

        private static ConsoleColor GetTypeConsoleColor(MessageType type) => type switch
            {
                MessageType.Error => ConsoleColor.Red,
                MessageType.Success => ConsoleColor.Green,
                MessageType.Warn => ConsoleColor.Yellow,
                MessageType.Info => ConsoleColor.White,
                _ => ConsoleColor.White
            };

        private static async Task StartTaskAsync(Type type)
        {
            try
            {
                WriteMessage($"Starting {type.FullName}.");

                var instance = (IScheduledTask)Container.Resolve(type);
                await instance.StartAsync();
            }
            catch (OperationCanceledException)
            {
                /* TODO: Why are we throwing an exception?
                 * rather than just gracefully returning from the awaited task ?
                 */
            }
            finally
            {
                if (Container.Resolve<CancellationTokenSource>().IsCancellationRequested)
                {
                    WriteMessage($"Cancelling {type.FullName}.");
                }
            }
        }

        private static void Event_CurrentDomainProcessExit(object? sender, EventArgs e)
        {
            WriteMessage("Shutting down service.");
            Stop();
        }

        private static void Stop()
        {
            // TODO: review cancel request
            var source = Container.Resolve<CancellationTokenSource>();
            if (source == null)
            {
                return;
            }

            source.Token.ThrowIfCancellationRequested();
            source.Cancel();
        }

        private static IConfiguration CreateConfiguration()
        {
            var config = new ConfigurationBuilder();
            config.AddEnvironmentVariables("ETL_");

            return config.Build();
        }
    }
}
