// <copyright file="ProjectsImport.cs" company="JPP Consulting">
// Copyright (c) JPP Consulting. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IdentityModel.Client;
using Jpp.Etl.Service.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Jpp.Etl.Service.Tasks
{
    internal class ProjectsImport : IScheduledTask
    {
        private const string DateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fff'Z'";
        private const bool UseAuth = true;

        private readonly IConfiguration configuration;
        private readonly CancellationTokenSource tokenSource;
        private readonly DeltekSqlQueries queries;

        public ProjectsImport(CancellationTokenSource cancellationTokenSource, IConfiguration config, DeltekSqlQueries sqlQueries)
        {
            this.configuration = config;
            this.queries = sqlQueries;
            this.tokenSource = cancellationTokenSource;
        }

        public async Task Start()
        {
            var intervalMinutes = this.GetIntervalMinutes();

            await Task.Run(
                async () =>
                {
                    var lastDateTime = await this.GetLastImportDateTime();

                    while (true)
                    {
                        var started = DateTime.Now;
                        this.tokenSource.Token.ThrowIfCancellationRequested();
                        Program.WriteMessage($"Finding projects modified since {lastDateTime}.");

                        var projects = this.queries.ProjectsModifiedSince(lastDateTime);

                        if (projects.Count > 0)
                        {
                            Program.WriteMessage($"Projects found : {projects.Count}. Attempting to import.", MessageType.Warn);

                            var result = await this.ImportProjects(projects, started);
                            if (result)
                            {
                                lastDateTime = started;
                                Program.WriteMessage($"Projects import successful. Scan due in {intervalMinutes} minutes.", MessageType.Success);
                            }
                            else
                            {
                                Program.WriteMessage($"Projects import failed. Scan due in {intervalMinutes} minutes.", MessageType.Error);
                            }
                        }
                        else
                        {
                            lastDateTime = started;

                            Program.WriteMessage($"No projects found. Scan due in {intervalMinutes} minutes.");
                        }

                        await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), this.tokenSource.Token);
                    }

                    // ReSharper disable once FunctionNeverReturns
                }, this.tokenSource.Token);
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

        private async Task<DateTime> GetLastImportDateTime()
        {
            try
            {
                var client = await this.CreateHttpClient();
                var builder = this.GetLastImportUriBuilder();

                var message = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = builder.Uri,
                };
                var response = await client.SendAsync(message);

                var result = await response.Content.ReadAsStringAsync();
                return !response.IsSuccessStatusCode ? throw new ArgumentException(result) : JsonConvert.DeserializeObject<DateTime>(result);
            }
            catch (Exception e)
            {
                Program.WriteMessage(e.Message, MessageType.Error);
                return Project.MinimumDateTime;
            }
        }

        private async Task<bool> ImportProjects(IEnumerable<Project> projects, DateTimeOffset startedDateTime)
        {
            HttpClient client;
            UriBuilder builder;
            IEnumerable<List<Project>> chunks;

            try
            {
                client = await this.CreateHttpClient();
                builder = this.GetImportUriBuilder(startedDateTime);
                chunks = Program.SplitList(projects.ToList(), 30);
            }
            catch (Exception e)
            {
                Program.WriteMessage(e.Message, MessageType.Error);
                return false;
            }

            if (client == null || builder == null)
            {
                return false;
            }

            var succeed = true;

            foreach (var chuck in chunks)
            {
                try
                {
                    var json = JsonConvert.SerializeObject(chuck);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var message = new HttpRequestMessage
                    {
                        Method = HttpMethod.Post,
                        RequestUri = builder.Uri,
                        Content = content,
                    };

                    var response = await client.SendAsync(message);
                    var result = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new ArgumentException(result);
                    }
                }
                catch (Exception e)
                {
                    Program.WriteMessage(e.Message, MessageType.Error);
                    succeed = false;
                }
            }

            return succeed;
        }

        private async Task<HttpClient> CreateHttpClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            // Flag to disable auth when running locally
            if (UseAuth)
            {
                var builder = this.GetTokenUriBuilder();

                var response = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
                {
                    Address = builder.Uri.ToString(),
                    ClientId = this.configuration["CLIENT_ID"],
                    ClientSecret = this.configuration["CLIENT_SECRET"],
                });

                if (response.IsError)
                {
                    throw new Exception(response.Error);
                }

                var token = response.AccessToken;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return client;
        }

        private UriBuilder GetLastImportUriBuilder()
        {
            return this.GetUriBuilder(this.configuration["API_END_POINT_PROJECT_IMPORT"]);
        }

        private UriBuilder GetTokenUriBuilder()
        {
            return this.GetUriBuilder(this.configuration["API_END_POINT_TOKEN"]);
        }

        private UriBuilder GetImportUriBuilder(DateTimeOffset startedDateTime)
        {
            return this.GetUriBuilder(this.configuration["API_END_POINT_PROJECT_IMPORT"], $"Started={startedDateTime.UtcDateTime.ToString(DateTimeFormat)}");
        }

        private UriBuilder GetUriBuilder(string path, string? query = null)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            var builder = new UriBuilder
            {
                Scheme = this.configuration["API_BASE_URI_SCHEME"],
                Host = this.configuration["API_BASE_URI_HOST"],
                Port = int.Parse(this.configuration["API_BASE_URI_PORT"]),
                Path = path,
            };

            if (!string.IsNullOrWhiteSpace(query))
            {
                builder.Query = query;
            }

            return builder;
        }
    }
}
