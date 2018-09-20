using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Timesheets
{
    public class ProjectClient : IProjectClient
    {
        private readonly HttpClient _client;
        private readonly ILogger<ProjectClient> _logger;
        
        private Dictionary<long, ProjectInfo> _cache;
        
        private Func<Task<string>> _fetchFunction;

        public ProjectClient(HttpClient client, ILogger<ProjectClient> logger, Func<Task<string>> fetchFunction)
        {
            _client = client;
            _logger = logger;
            _cache = new Dictionary<long, ProjectInfo>();
            _fetchFunction = fetchFunction;

        }
        
        public async Task<ProjectInfo> Get(long projectId)
        {
            return await new GetProjectCommand(DoGet, DoGetFromCache, projectId).ExecuteAsync();
        }        

        public async Task<ProjectInfo> DoGet(long projectId)
        {
            var token = await _fetchFunction();

            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var streamTask = _client.GetStreamAsync($"project?projectId={projectId}");

            _logger.LogInformation($"Attempting to fetch projectId: {projectId}");
            
            var serializer = new DataContractJsonSerializer(typeof(ProjectInfo));
            var project = serializer.ReadObject(await streamTask) as ProjectInfo;
            
            _cache.Add(projectId, project);
            _logger.LogInformation($"Caching projectId: {projectId}");
            
            return project;
        }
        
        public Task<ProjectInfo> DoGetFromCache(long projectId)
        {
            _logger.LogInformation($"Retrieving from cache projectId: {projectId}");
            return Task.FromResult(_cache[projectId]);
        }        
    }
}