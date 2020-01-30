// <copyright file="DeltekProjectService.cs" company="JPP Consulting">
// Copyright (c) JPP Consulting. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using IdentityModel.Client;
using Newtonsoft.Json;

namespace Jpp.Etl.Service.Projects
{
    internal class DeltekProjectService : CommonBase<DeltekProjectService>
    {
        private readonly DeltekProjectRepository repository;

        public DeltekProjectService(CommonServices<DeltekProjectService> commonServices, DeltekProjectRepository repository)
            : base(commonServices)
        {
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public int PartitionSize => this.GetPartitionSize();

        public bool UseAuth => this.GetUseAuth();

        public async Task<DateTime?> GetLastImportAsync()
        {
            var client = await this.CreateAuthenticateHttpClientAsync();
            if (client == null)
            {
                return null;
            }

            var response = await client.SendAsync(this.GetLastImportRequestMessage());

            var result = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<DateTime>(result);
            }

            return null;
        }

        public async Task<List<DeltekProject>> GetProjectsAsync(DateTimeOffset modifiedSince)
        {
            return await Task.Run(() => this.repository.GetProjectList(modifiedSince));
        }

        public async Task<bool> ImportProjectsAsync(List<DeltekProject> projects, DateTimeOffset started)
        {
            var client = await this.CreateAuthenticateHttpClientAsync();
            if (client == null)
            {
                return false;
            }

            var partitions = this.SplitList(projects).ToList();

            if (this.ImportProjectPartitions(client, partitions))
            {
                return await this.SetImportSuccessfulAsync(client, started);
            }

            return false;
        }

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        private static HttpRequestMessage CreatePostRequestMessage(string json, Uri uri)
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            return new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = uri,
                Content = content,
            };
        }

        private async Task<HttpClient?> CreateAuthenticateHttpClientAsync()
        {
            var client = CreateHttpClient();

            if (this.UseAuth)
            {
                var response = await client.RequestClientCredentialsTokenAsync(this.GetClientTokenRequest());

                if (response.IsError)
                {
                    this.LogError("Failed to create authenticated client.");
                    return null;
                }

                var token = response.AccessToken;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return client;
        }

        private bool ImportProjectPartitions(HttpClient client, IEnumerable<List<DeltekProject>> partitions)
        {
            var results = partitions.AsParallel().Select(p =>
            {
                var message = this.GetImportProjectsRequestMessage(p);
                var response = client.SendAsync(message).Result;
                return response.IsSuccessStatusCode;
            });

            return results.All(result => result == true);
        }

        private async Task<bool> SetImportSuccessfulAsync(HttpClient client, DateTimeOffset started)
        {
            var message = this.GetImportSuccessRequestMessage(started);
            var response = await client.SendAsync(message);
            return response.IsSuccessStatusCode;
        }

        private IEnumerable<List<T>> SplitList<T>(List<T> list)
        {
            for (var i = 0; i < list.Count; i += this.PartitionSize)
            {
                yield return list.GetRange(i, Math.Min(this.PartitionSize, list.Count - i));
            }
        }

        private HttpRequestMessage GetLastImportRequestMessage()
        {
            var builder = this.GetUriBuilder(this.GetConfiguration("API_END_POINT_PROJECT_IMPORT_DELTEK"));

            return new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = builder.Uri,
            };
        }

        private HttpRequestMessage GetImportSuccessRequestMessage(DateTimeOffset started)
        {
            var builder = this.GetUriBuilder(this.GetConfiguration("API_END_POINT_PROJECT_IMPORT_DELTEK"));
            var json = JsonConvert.SerializeObject(started.UtcDateTime);
            return CreatePostRequestMessage(json, builder.Uri);
        }

        private HttpRequestMessage GetImportProjectsRequestMessage(List<DeltekProject> list)
        {
            var builder = this.GetUriBuilder(this.GetConfiguration("API_END_POINT_PROJECT_IMPORT"));
            var json = JsonConvert.SerializeObject(list);
            return CreatePostRequestMessage(json, builder.Uri);
        }

        private ClientCredentialsTokenRequest GetClientTokenRequest()
        {
            var builder = this.GetUriBuilder(this.GetConfiguration("API_END_POINT_TOKEN"));
            return new ClientCredentialsTokenRequest
            {
                Address = builder.Uri.ToString(),
                ClientId = this.GetConfiguration("CLIENT_ID"),
                ClientSecret = this.GetConfiguration("CLIENT_SECRET"),
            };
        }

        private UriBuilder GetUriBuilder(string path, string? query = null)
        {
            var builder = new UriBuilder
            {
                Scheme = this.GetConfiguration("API_BASE_URI_SCHEME"),
                Host = this.GetConfiguration("API_BASE_URI_HOST"),
                Port = int.Parse(this.GetConfiguration("API_BASE_URI_PORT")),
                Path = path,
            };

            if (!string.IsNullOrWhiteSpace(query))
            {
                builder.Query = query;
            }

            return builder;
        }

        private int GetPartitionSize()
        {
            var partitionSize = 5;
            if (int.TryParse(this.GetConfiguration("PROJECTS_PARTITION_SIZE"), out var parsedResult))
            {
                partitionSize = parsedResult;
            }

            return partitionSize;
        }

        private bool GetUseAuth()
        {
            var useAuth = true;
            if (bool.TryParse(this.GetConfiguration("PROJECTS_USE_AUTHENTICATION"), out var parsedResult))
            {
                useAuth = parsedResult;
            }

            return useAuth;
        }
    }
}
