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
using System.Numerics;

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
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "assignments/{assignmentId}/recommended-learn-content")] HttpRequest req,
            [Table(RecommendedLearnContentTableName)] CloudTable recommendedLearnContentTable,
            string assignmentId)
        {
            _logger.LogInformation($"Fetching all recommended learn content for assignment {assignmentId}.");

            List<RecommendedLearnContentEntity> assignmentRecommendedLearnContent = await GetAllRecommendedLearnContentEntities(recommendedLearnContentTable, assignmentId);

            IEnumerable<RecommendedLearnContentDto> assignmentRecommendedLearnContentDtos = assignmentRecommendedLearnContent.Select(_mapper.Map<RecommendedLearnContentDto>);

            return new OkObjectResult(assignmentRecommendedLearnContentDtos);
        }

        private async Task<List<RecommendedLearnContentEntity>> GetAllRecommendedLearnContentEntities(CloudTable recommendedLearnContentTable, string assignmentId)
        {
            List<RecommendedLearnContentEntity> assignmentRecommendedLearnContent = new List<RecommendedLearnContentEntity>();

            //beginner
            RecommendedLearnContentEntity recommendedBeginnerLearnContentEntity = await GetRecommendedLearnContentEntities(recommendedLearnContentTable, assignmentId, "beginner");
            assignmentRecommendedLearnContent.Add(recommendedBeginnerLearnContentEntity);

            //intermediate
            RecommendedLearnContentEntity recommendedIntermediateLearnContentEntity = await GetRecommendedLearnContentEntities(recommendedLearnContentTable, assignmentId, "intermediate");
            assignmentRecommendedLearnContent.Add(recommendedIntermediateLearnContentEntity);
            
            //advanced
            RecommendedLearnContentEntity recommendedAdvancedLearnContentEntity = await GetRecommendedLearnContentEntities(recommendedLearnContentTable, assignmentId, "advanced");
            assignmentRecommendedLearnContent.Add(recommendedAdvancedLearnContentEntity);

            return assignmentRecommendedLearnContent;

            //beginner
            //TableQuery<RecommendedLearnContentEntity> beginnerRecommendedLearnContentQuery = new TableQuery<RecommendedLearnContentEntity>()
            //    .Where(TableQuery.CombineFilters(
            //        TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey), QueryComparisons.Equal, assignmentId),
            //        TableOperators.And,
            //        TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey), QueryComparisons.Equal, "beginner")
            //    ));

            //RecommendedLearnContentEntity beginnerRecommendedLearnContent = new RecommendedLearnContentEntity();
            //TableContinuationToken beginnerContinuationToken = new TableContinuationToken();
            //do
            //{
            //    TableQuerySegment<RecommendedLearnContentEntity> querySegment = await recommendedLearnContentTable.ExecuteQuerySegmentedAsync(beginnerRecommendedLearnContentQuery, beginnerContinuationToken);
            //    beginnerContinuationToken = querySegment.ContinuationToken;
            //    beginnerRecommendedLearnContent.AddRange(querySegment.Results);
            //} while (beginnerContinuationToken != null);

            //intermediate
            //TableQuery<RecommendedLearnContentEntity> intermediateRecommendedLearnContentQuery = new TableQuery<RecommendedLearnContentEntity>()
            //    .Where(TableQuery.CombineFilters(
            //        TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey), QueryComparisons.Equal, assignmentId),
            //        TableOperators.And,
            //        TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey), QueryComparisons.Equal, "intermediate")
            //    ));

            //List<RecommendedLearnContentEntity> intermediateRecommendedLearnContent = new List<RecommendedLearnContentEntity>();
            //TableContinuationToken intermediateContinuationToken = new TableContinuationToken();
            //do
            //{
            //    TableQuerySegment<RecommendedLearnContentEntity> querySegment = await recommendedLearnContentTable.ExecuteQuerySegmentedAsync(intermediateRecommendedLearnContentQuery, intermediateContinuationToken);
            //    intermediateContinuationToken = querySegment.ContinuationToken;
            //    intermediateRecommendedLearnContent.AddRange(querySegment.Results);
            //} while (intermediateContinuationToken != null);

            ////advanced
            //TableQuery<RecommendedLearnContentEntity> advancedRecommendedLearnContentQuery = new TableQuery<RecommendedLearnContentEntity>()
            //    .Where(TableQuery.CombineFilters(
            //        TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey), QueryComparisons.Equal, assignmentId),
            //        TableOperators.And,
            //        TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey), QueryComparisons.Equal, "advanced")
            //    ));

            //List<RecommendedLearnContentEntity> advancedRecommendedLearnContent = new List<RecommendedLearnContentEntity>();
            //TableContinuationToken advancedContinuationToken = new TableContinuationToken();
            //do
            //{
            //    TableQuerySegment<RecommendedLearnContentEntity> querySegment = await recommendedLearnContentTable.ExecuteQuerySegmentedAsync(advancedRecommendedLearnContentQuery, advancedContinuationToken);
            //    advancedContinuationToken = querySegment.ContinuationToken;
            //    advancedRecommendedLearnContent.AddRange(querySegment.Results);
            //} while (advancedContinuationToken != null);

            //List<RecommendedLearnContentEntity> assignmentRecommendedLearnContent = new List<RecommendedLearnContentEntity>();
            //assignmentRecommendedLearnContent.Add(beginnerRecommendedLearnContent);
            //assignmentRecommendedLearnContent.Add(intermediateRecommendedLearnContent);
            //assignmentRecommendedLearnContent.Add(advancedRecommendedLearnContent);

        }


        private async Task<RecommendedLearnContentEntity> GetRecommendedLearnContentEntities(CloudTable recommendedLearnContentTable, string partitionKey, string rowKey)
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<RecommendedLearnContentEntity>(partitionKey, rowKey);

            TableResult retrieveResult = await recommendedLearnContentTable.ExecuteAsync(retrieveOperation);

            if (retrieveResult.Result == null || !(retrieveResult.Result is RecommendedLearnContentEntity assignmentEntity))
                return null;

            return assignmentEntity;

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
