using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using AutoMapper;
using Edna.Utils.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Edna.Utils.Http;
using System.IO;

namespace Edna.LearnContentRecommender
{
    public class LearnContentRecommenderApi
    {
        private const string LearnContentRecommenderTableName = "LearnContentRecommender";
        private const string LearnContentUrlIdentifierKey = "WT.mc_id";
        private const string LearnContentUrlIdentifierValue = "Edna";

        private readonly ILogger<LearnContentRecommenderApi> _logger;
        private readonly IMapper _mapper;
        private readonly IHttpClientFactory _httpClientFactory;

        public LearnContentRecommenderApi(ILogger<LearnContentRecommenderApi> logger, IMapper mapper, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _mapper = mapper;
            _httpClientFactory = httpClientFactory;
        }

        [FunctionName(nameof(GetLearnCatalog))]
        public async Task<IActionResult> GetLearnCatalog(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "learn-catalog")] HttpRequest req)
        {
            using HttpClient client = _httpClientFactory.CreateClient();
            string catalogString = await client.GetStringAsync($"https://docs.microsoft.com/api/learn/catalog?clientId={LearnContentUrlIdentifierValue}");

            JObject catalogJObject = JsonConvert.DeserializeObject<JObject>(catalogString);
            catalogJObject["modules"].ForEach(ChangeUrlQueryToEdnaIdentifier);
            catalogJObject["learningPaths"].ForEach(ChangeUrlQueryToEdnaIdentifier);

            return new OkObjectResult(JsonConvert.SerializeObject(catalogJObject));
        }

        private void ChangeUrlQueryToEdnaIdentifier(JToken contentJToken)
        {
            string url = contentJToken["url"]?.ToString();
            if (string.IsNullOrEmpty(url))
                return;

            Uri previousUri = new Uri(url);
            NameValueCollection queryParams = previousUri.ParseQueryString();
            queryParams[LearnContentUrlIdentifierKey] = LearnContentUrlIdentifierValue;

            UriBuilder newUriBuilder = new UriBuilder(url) { Query = queryParams.ToString() };

            contentJToken["url"] = newUriBuilder.Uri.ToString();
        }


        //one func to get results from model and save results in a table 
        //one func to get results of similarity bw assignment title and all courses and save in a table as assignmentID vs recommended courses
        //one func to get saved results from table for a particular assignment id

        [FunctionName(nameof(GetAllLearnContentRecommender))]
        public async Task<IActionResult> GetAllLearnContentRecommender(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "assignments/{assignmentId}/recommended-learn-content")] HttpRequest req,
            [Table(LearnContentRecommenderTableName)] CloudTable assignmentLearnContentTable,
            string assignmentId)
        {
            _logger.LogInformation($"Fetching all recommended learn content for assignment {assignmentId}.");

            List<LearnContentRecommenderEntity> assignmentRecommendedLearnContent = await GetAllLearnContentRecommenderEntities(assignmentLearnContentTable, assignmentId);

            IEnumerable<LearnContentRecommenderDto> assignmentRecommendedLearnContentDtos = assignmentRecommendedLearnContent
                .OrderBy(entity => entity.Timestamp.Ticks)
                .Select(_mapper.Map<LearnContentRecommenderDto>);

            return new OkObjectResult(assignmentRecommendedLearnContentDtos);
        }

        private async Task<List<LearnContentRecommenderEntity>> GetAllLearnContentRecommenderEntities(CloudTable learnContentRecommenderTable, string assignmentId)
        {
            TableQuery<LearnContentRecommenderEntity> assignmentSelectedLearnContentQuery = new TableQuery<LearnContentRecommenderEntity>()
                .Where(
                    TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey), QueryComparisons.Equal, assignmentId)
                );

            List<LearnContentRecommenderEntity> assignmentSelectedLearnContent = new List<LearnContentRecommenderEntity>();
            TableContinuationToken continuationToken = new TableContinuationToken();
            do
            {
                TableQuerySegment<LearnContentRecommenderEntity> querySegment = await learnContentRecommenderTable.ExecuteQuerySegmentedAsync(assignmentSelectedLearnContentQuery, continuationToken);
                continuationToken = querySegment.ContinuationToken;
                assignmentSelectedLearnContent.AddRange(querySegment.Results);
            } while (continuationToken != null);

            return assignmentSelectedLearnContent;
        }

        //[FunctionName("LearnContentRecommenderApi")]
        //public static async Task<IActionResult> Run(
        //    [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
        //    ILogger log)
        //{
        //    log.LogInformation("C# HTTP trigger function processed a request.");

        //    string name = req.Query["name"];

        //    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        //    dynamic data = JsonConvert.DeserializeObject(requestBody);
        //    name = name ?? data?.name;

        //    string responseMessage = string.IsNullOrEmpty(name)
        //        ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
        //        : $"Hello, {name}. This HTTP triggered function executed successfully.";

        //    return new OkObjectResult(responseMessage);
        //}


    }
}
