using IdentityModel.Client;
using Jpp.Etl.Service.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jpp.Etl.Service.Tasks
{
    internal class ProjectsImport : IScheduledTask
    {
        private const string DATE_TIME_FORMAT = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fff'Z'";
        private const bool USE_AUTH = true;

        private readonly IConfiguration configuration;
        private readonly CancellationTokenSource _tokenSource;
        private readonly DeltekSqlQueries _queries;

        public ProjectsImport(CancellationTokenSource tokenSource, IConfiguration config, DeltekSqlQueries queries)
        {
            configuration = config;
            _queries = queries;
            _tokenSource = tokenSource;
        }

        public async Task Start()
        {
            var intervalMinutes = GetIntervalMinutes();

            await Task.Run(async () => {

                var lastDateTime = await GetLastImportDateTime();
                
                while (true)
                {
                    var started = DateTime.Now;
                    _tokenSource.Token.ThrowIfCancellationRequested();
                    Program.WriteMessage($"Finding projects modified since {lastDateTime}.");

                    var projects = _queries.ProjectsModifiedSince(lastDateTime);

                    if (projects.Count > 0)
                    {
                        Program.WriteMessage($"Projects found : {projects.Count}. Attempting to import.", Program.MessageType.Warn);

                        var result = await ImportProjects(projects, started);
                        if (result)
                        {
                            lastDateTime = started;
                            Program.WriteMessage($"Projects import successful. Scan due in {intervalMinutes} minutes.", Program.MessageType.Success);
                        }
                        else
                        {
                            Program.WriteMessage($"Projects import failed. Scan due in {intervalMinutes} minutes.", Program.MessageType.Error);
                        }
                    }
                    else
                    {
                        lastDateTime = started;

                        Program.WriteMessage($"No projects found. Scan due in {intervalMinutes} minutes.");
                    }
                    
                    await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), _tokenSource.Token);
                }
                
                // ReSharper disable once FunctionNeverReturns
            }, _tokenSource.Token);
        }

        private int GetIntervalMinutes()
        {
            var intervalMinutes = 5;
            if (int.TryParse(configuration["PROJECTS_INTERVAL_MINUTES"], out var parsedResult))
            {
                intervalMinutes = parsedResult;
            }

            return intervalMinutes;
        }


        private async Task<DateTime> GetLastImportDateTime()
        {
            try
            {
                var client = await CreateHttpClient();
                var builder = GetUriBuilder(configuration["API_END_POINT_PROJECT_IMPORT"]);

                var message = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = builder.Uri
                };
                var response = await client.SendAsync(message);

                var result = await response.Content.ReadAsStringAsync();
                return !response.IsSuccessStatusCode ? throw new ArgumentException(result) : JsonConvert.DeserializeObject<DateTime>(result);
            }
            catch (Exception e)
            {
                Program.WriteMessage(e.Message, Program.MessageType.Error);
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
                client = await CreateHttpClient();
                builder = GetUriBuilder(configuration["API_END_POINT_PROJECT_IMPORT"], $"Started={startedDateTime.UtcDateTime.ToString(DATE_TIME_FORMAT)}");
                chunks = Program.SplitList(projects.ToList(), 30);
            }
            catch (Exception e)
            {
                Program.WriteMessage(e.Message, Program.MessageType.Error);
                return false;
            }

            if (client == null || builder == null) return false;

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
                        Content = content
                    };

                    var response = await client.SendAsync(message);
                    var result = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode) throw new ArgumentException(result);
                }
                catch (Exception e)
                {
                    Program.WriteMessage(e.Message, Program.MessageType.Error);
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
            if (USE_AUTH)
            {
                var builder = GetUriBuilder(configuration["API_END_POINT_TOKEN"]);

                var response = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
                {
                    Address = builder.Uri.ToString(),
                    ClientId = configuration["CLIENT_ID"],
                    ClientSecret = configuration["CLIENT_SECRET"]
                });

                if (response.IsError) throw new Exception(response.Error);

                var token = response.AccessToken;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return client;

        }

        private UriBuilder GetUriBuilder(string path, string? query = null)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));

            var builder = new UriBuilder
            {
                Scheme = configuration["API_BASE_URI_SCHEME"],
                Host = configuration["API_BASE_URI_HOST"],
                Port = int.Parse(configuration["API_BASE_URI_PORT"]),
                Path = path
            };

            if (!string.IsNullOrWhiteSpace(query)) builder.Query = query;

            return builder;
        }
    }
}
