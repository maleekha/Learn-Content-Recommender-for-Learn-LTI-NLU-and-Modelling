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
        private const string LearnContentEmbeddingsTableName = "LearnContentEmbeddings";
        private const string RecommendedLearnContentTableName = "RecommendedLearnContent";
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
        //one func to get results of similarity bw assignment title and all courses and save in a table as recommender vs recommended courses
        //one func to get saved results from table for a particular assignment id

        [FunctionName(nameof(GetRecommendedLearnContent))]
        public async Task<IActionResult> GetRecommendedLearnContent(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "assignments/{recommenderId}/recommended-learn-content")] HttpRequest req,
            [Table(RecommendedLearnContentTableName)] CloudTable assignmentLearnContentTable,
            string recommenderId)
        {
            _logger.LogInformation($"Fetching all recommended learn content for assignment & level {recommenderId}.");

            List<RecommendedLearnContentEntity> assignmentRecommendedLearnContent = await GetRecommendedLearnContentEntities(assignmentLearnContentTable, recommenderId);

            IEnumerable<RecommendedLearnContentDto> assignmentRecommendedLearnContentDtos = assignmentRecommendedLearnContent
                .OrderBy(entity => entity.Timestamp.Ticks)
                .Select(_mapper.Map<RecommendedLearnContentDto>);

            return new OkObjectResult(assignmentRecommendedLearnContentDtos);
        }

        private async Task<List<RecommendedLearnContentEntity>> GetRecommendedLearnContentEntities(CloudTable learnContentRecommenderTable, string recommenderId)
        {
            TableQuery<RecommendedLearnContentEntity> assignmentRecommendedLearnContentQuery = new TableQuery<RecommendedLearnContentEntity>()
                .Where(
                    TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey), QueryComparisons.Equal, recommenderId)
                );

            List<RecommendedLearnContentEntity> assignmentRecommendedLearnContent = new List<RecommendedLearnContentEntity>();
            TableContinuationToken continuationToken = new TableContinuationToken();
            do
            {
                TableQuerySegment<RecommendedLearnContentEntity> querySegment = await learnContentRecommenderTable.ExecuteQuerySegmentedAsync(assignmentRecommendedLearnContentQuery, continuationToken);
                continuationToken = querySegment.ContinuationToken;
                assignmentRecommendedLearnContent.AddRange(querySegment.Results);
            } while (continuationToken != null);

            return assignmentRecommendedLearnContent;
        }

        private void GetSimilarity(CloudTable learnContentEmbeddings, string assignmentTitleEmbedding)
        {
            
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
