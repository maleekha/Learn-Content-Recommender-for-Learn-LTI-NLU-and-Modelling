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
using Edna.Bindings.Assignment.Attributes;
using Edna.Bindings.Assignment.Models;
using System.Text;

namespace Edna.LearnContentRecommender
{
    public class LearnContentRecommenderApi
    {
        private const string LearnContentEmbeddingsTableName = "LearnContentEmbeddings";
        private const string RecommendedLearnContentTableName = "RecommendedLearnContent";
        private const string LearnContentUrlIdentifierKey = "WT.mc_id";
        private const string LearnContentUrlIdentifierValue = "Edna";
        private const string NLU_MODEL = "http://2160021e-2abf-4fc7-ad5f-690b721272f9.eastus.azurecontainer.io/score";
        private const double THRESHOLD = 0.70;

        private readonly ILogger<LearnContentRecommenderApi> _logger;
        private readonly IMapper _mapper;
        private readonly IHttpClientFactory _httpClientFactory;

        public LearnContentRecommenderApi(ILogger<LearnContentRecommenderApi> logger, IMapper mapper, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _mapper = mapper;
            _httpClientFactory = httpClientFactory;
        }

        [FunctionName(nameof(InitializeEmbeddingsTable))]
        public async void InitializeEmbeddingsTable(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "init-embeddings")] HttpRequest req,
            [Table(LearnContentEmbeddingsTableName)] CloudTable learnContentEmbeddingsTable)
        {
            using HttpClient client = _httpClientFactory.CreateClient();
            string catalogString = await client.GetStringAsync($"https://docs.microsoft.com/api/learn/catalog?clientId={LearnContentUrlIdentifierValue}");

            JObject catalogJObject = JsonConvert.DeserializeObject<JObject>(catalogString);
            catalogJObject["modules"].ForEach(ChangeUrlQueryToEdnaIdentifier);
            catalogJObject["learningPaths"].ForEach(ChangeUrlQueryToEdnaIdentifier);

            List<string> title_desc = new List<string>();
            List<string> contentUids = new List<string>();
            List<string> levels = new List<string>();

            foreach (var contentJToken in catalogJObject["modules"])
            {
                title_desc.Add(contentJToken["title"].ToString() + " " + contentJToken["summary"].ToString());
                contentUids.Add(contentJToken["uid"].ToString());
                levels.Add(contentJToken["levels"][0].ToString());
            }
            foreach (var contentJToken in catalogJObject["learningPaths"])
            {
                title_desc.Add(contentJToken["title"].ToString() + " " + contentJToken["summary"].ToString());
                contentUids.Add(contentJToken["uid"].ToString());
                levels.Add(contentJToken["levels"][0].ToString());
            }

            List<string> learnContentEmbeddings = new List<string>();
            HttpClient client2 = _httpClientFactory.CreateClient();
            client2.Timeout = TimeSpan.FromMinutes(5);

            const int num = 50;
            for(int i=0; i<title_desc.Count; i+=num)
            {
                var obj = new
                {
                    data = title_desc.Skip(i).Take(num)
                };

                var a = new StringContent(JsonConvert.SerializeObject(obj));
                HttpResponseMessage resp = await client2.PostAsync(NLU_MODEL, new StringContent(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json"));
                string respString = await resp.Content.ReadAsStringAsync();
                learnContentEmbeddings.AddRange(CustomStringParser(respString));
            }            

            List<LearnContentEmbeddingEntity> learnContentEmbeddingEntities = new List<LearnContentEmbeddingEntity>();
            for(int i=0; i< contentUids.Count; i++) 
            {
                LearnContentEmbeddingEntity temp = new LearnContentEmbeddingEntity { PartitionKey = contentUids[i], RowKey = levels[i], Embedding = learnContentEmbeddings[i], ETag="*"};
                TableOperation insertEmbedding = TableOperation.Insert(temp);
                await learnContentEmbeddingsTable.ExecuteAsync(insertEmbedding);
            }
        }

        private string[] CustomStringParser(string resp)
        {            
            string str = new string(resp.Replace(@"\", "").Replace("\"", "")
                        .ToCharArray().Where(ch => !Char.IsWhiteSpace(ch)).ToArray())
                        .Replace("[[", "")
                        .Replace("]]", "");

            return new string(str.ToCharArray().Where(ch => !Char.IsWhiteSpace(ch)).ToArray()).Split("],[");           
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

        [FunctionName(nameof(GetRecommendedLearnContent))]
        public async Task<IActionResult> GetRecommendedLearnContent(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "assignments/{assignmentId}/recommended-learn-content")] HttpRequest req,
            [Table(RecommendedLearnContentTableName)] CloudTable recommendedLearnContentTable,
            [Table(LearnContentEmbeddingsTableName)] CloudTable learnContentEmbeddingsTable,
            [Assignment(AssignmentId = "{assignmentId}")] Assignment assignment,
            string assignmentId)
        {
            _logger.LogInformation($"Fetching all recommended learn content for assignment {assignmentId}.");

            List<RecommendedLearnContentEntity> assignmentRecommendedLearnContent = await GetAllRecommendedLearnContentEntities(recommendedLearnContentTable, learnContentEmbeddingsTable, assignmentId, assignment.Name);

            IEnumerable<RecommendedLearnContentDto> assignmentRecommendedLearnContentDtos = assignmentRecommendedLearnContent.Select(_mapper.Map<RecommendedLearnContentDto>);

            return new OkObjectResult(assignmentRecommendedLearnContentDtos);
        }

        private async Task<List<RecommendedLearnContentEntity>> GetAllRecommendedLearnContentEntities(CloudTable recommendedLearnContentTable, CloudTable learnContentEmbeddingsTableName, string assignmentId, string assignmentTitle)
        {
            List<RecommendedLearnContentEntity> assignmentRecommendedLearnContent = new List<RecommendedLearnContentEntity>();

            //beginner
            RecommendedLearnContentEntity recommendedBeginnerLearnContentEntity = await GetRecommendedLearnContentEntities(recommendedLearnContentTable, assignmentId, "beginner");
            
            //intermediate
            RecommendedLearnContentEntity recommendedIntermediateLearnContentEntity = await GetRecommendedLearnContentEntities(recommendedLearnContentTable, assignmentId, "intermediate");
                        
            //advanced
            RecommendedLearnContentEntity recommendedAdvancedLearnContentEntity = await GetRecommendedLearnContentEntities(recommendedLearnContentTable, assignmentId, "advanced");

            if (recommendedBeginnerLearnContentEntity is null && recommendedIntermediateLearnContentEntity is null && recommendedAdvancedLearnContentEntity is null)
            {
                // first time call
                var recCourses = await GetRecommendedLearnContentFromAssignmentTitle_V1(learnContentEmbeddingsTableName, assignmentTitle);

                RecommendedLearnContentEntity beginnerEntity = new RecommendedLearnContentEntity { PartitionKey = assignmentId, RowKey = "beginner", RecommendedContentUids = recCourses[0] , ETag="*" };
                TableOperation insertBeginnerOp = TableOperation.Insert(beginnerEntity);
                await recommendedLearnContentTable.ExecuteAsync(insertBeginnerOp);
                assignmentRecommendedLearnContent.Add(beginnerEntity);

                RecommendedLearnContentEntity intermediateEntity = new RecommendedLearnContentEntity { PartitionKey = assignmentId, RowKey = "intermediate", RecommendedContentUids = recCourses[1], ETag = "*" };
                TableOperation insertIntermediateOp = TableOperation.Insert(intermediateEntity);
                await recommendedLearnContentTable.ExecuteAsync(insertIntermediateOp);
                assignmentRecommendedLearnContent.Add(intermediateEntity);

                RecommendedLearnContentEntity advancedEntity = new RecommendedLearnContentEntity { PartitionKey = assignmentId, RowKey = "advanced", RecommendedContentUids = recCourses[2], ETag = "*" };
                TableOperation insertAdvancedOp = TableOperation.Insert(advancedEntity);
                await recommendedLearnContentTable.ExecuteAsync(insertAdvancedOp);
                assignmentRecommendedLearnContent.Add(advancedEntity);

                return assignmentRecommendedLearnContent;
            }

            else
            {
                assignmentRecommendedLearnContent.Add(recommendedBeginnerLearnContentEntity);
                assignmentRecommendedLearnContent.Add(recommendedIntermediateLearnContentEntity);
                assignmentRecommendedLearnContent.Add(recommendedAdvancedLearnContentEntity);
            }
            return assignmentRecommendedLearnContent;
        }

        private async Task<RecommendedLearnContentEntity> GetRecommendedLearnContentEntities(CloudTable recommendedLearnContentTable, string partitionKey, string rowKey)
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<RecommendedLearnContentEntity>(partitionKey, rowKey);

            TableResult retrieveResult = await recommendedLearnContentTable.ExecuteAsync(retrieveOperation);
            if (retrieveResult.Result == null || !(retrieveResult.Result is RecommendedLearnContentEntity assignmentEntity))
                return null;
            return assignmentEntity;
        }

        private async Task<List<string>> GetRecommendedLearnContentFromAssignmentTitle_V2(string assignmentTitle)
        {
            List<string> data = new List<string>() { assignmentTitle };
            var jsonContent = JsonConvert.SerializeObject(data);
            var obj = new
            {
                data = assignmentTitle
            };
            HttpClient client = _httpClientFactory.CreateClient();
            HttpResponseMessage resp = await client.PostAsJsonAsync(NLU_MODEL, obj);
            string catalog_similarity = await resp.Content.ReadAsStringAsync();

            List<LearnContentSimilarityObject> similarityList = JsonConvert.DeserializeObject<List<LearnContentSimilarityObject>>(catalog_similarity);

            IEnumerable<LearnContentSimilarityObject> beginnerLevelCourses = similarityList.Where(e => e.Level.CompareTo(Level.Beginner) == 0 && e.Similarity > THRESHOLD).Take(3);
            IEnumerable<LearnContentSimilarityObject> intermediateLevelCourses = similarityList.Where(e => e.Level.CompareTo(Level.Intermediate) == 0 && e.Similarity > THRESHOLD).Take(3);
            IEnumerable<LearnContentSimilarityObject> advancedLevelCourses = similarityList.Where(e => e.Level.CompareTo(Level.Advanced) == 0 && e.Similarity > THRESHOLD).Take(3);

            string beginnerLevelContentUids = string.Join(",", beginnerLevelCourses.Select(e => e.ContentUid));
            string intermediateLevelContentUids = string.Join(",", intermediateLevelCourses.Select(e => e.ContentUid));
            string advancedLevelContentUids = string.Join(",", advancedLevelCourses.Select(e => e.ContentUid));
            
            return new List<string> { beginnerLevelContentUids, intermediateLevelContentUids, advancedLevelContentUids };            
        }

        private async Task<List<string>> GetRecommendedLearnContentFromAssignmentTitle_V1(CloudTable learnContentEmbeddingsTable, string assignmentTitle)
        {
            TableQuery<LearnContentEmbeddingEntity> q = new TableQuery<LearnContentEmbeddingEntity>();
            TableContinuationToken continuationToken = new TableContinuationToken();

            List<LearnContentEmbeddingEntity> learnContentEmbeddings = new List<LearnContentEmbeddingEntity>();
            do
            {
                TableQuerySegment<LearnContentEmbeddingEntity> queryResultSegment = await learnContentEmbeddingsTable.ExecuteQuerySegmentedAsync(q, continuationToken);
                continuationToken = queryResultSegment.ContinuationToken;
                learnContentEmbeddings.AddRange(queryResultSegment.Results);

            } while (continuationToken != null);

            List<string> title = new List<string>() { assignmentTitle };
            var obj = new
            {
                data = title
            };

            HttpClient client = _httpClientFactory.CreateClient();
            HttpResponseMessage resp = await client.PostAsJsonAsync(NLU_MODEL, obj);
            string respString = await resp.Content.ReadAsStringAsync();
            string parsed_string = CustomStringParser(respString)[0];
            double[] assignmentTitleEmbedding = parsed_string.Split(',').Where(x => x.Trim().Length != 0).Select(b => double.Parse(b.Trim())).ToArray();

            List<LearnContentSimilarityObject> simi = new List<LearnContentSimilarityObject>();

            foreach(LearnContentEmbeddingEntity e in learnContentEmbeddings)
            {
                double[] emb = e.Embedding.Split(',').Where(x => x.Trim().Length != 0).Select(b => double.Parse(b.Trim())).ToArray();
                double similarity = CosineSimilarity(emb, assignmentTitleEmbedding);
                simi.Add(new LearnContentSimilarityObject { ContentUid = e.PartitionKey, Level = e.RowKey, Similarity = similarity });
            }

            simi.Sort(comparer);

            IEnumerable<LearnContentSimilarityObject> beginnerSimi = simi.Where(e => e.Level == "beginner" && e.Similarity > THRESHOLD).Take(3);
            IEnumerable<LearnContentSimilarityObject> intermediateSimi = simi.Where(e => e.Level == "intermediate" && e.Similarity > THRESHOLD).Take(3);
            IEnumerable<LearnContentSimilarityObject> advancedSimi = simi.Where(e => e.Level == "advanced" && e.Similarity > THRESHOLD).Take(3);

            string beginnerLevelContentUids = string.Join(",", beginnerSimi.Select(e => e.ContentUid));
            string intermediateLevelContentUids = string.Join(",", intermediateSimi.Select(e => e.ContentUid));
            string advancedLevelContentUids = string.Join(",", advancedSimi.Select(e => e.ContentUid));

            return new List<string> { beginnerLevelContentUids, intermediateLevelContentUids, advancedLevelContentUids };
        }

        private int comparer(LearnContentSimilarityObject a, LearnContentSimilarityObject b)
        {
            return a.Similarity.CompareTo(b.Similarity);
        }
        
        private double CosineSimilarity(double[] a, double[] b)
        {
            double aa = 0.0;
            double bb = 0.0;
            double ab = 0.0;

            for(int i=0; i<a.Length; i++)
            {
                aa += a[i] * a[i];
                bb += b[i] * b[i];
                ab += a[i] * b[i];
            }

            // do not forget degenerated cases: all-zeroes vectors 
            if (aa == 0)
                return bb == 0 ? 1.0 : 0.0;
            else if (bb == 0)
                return 0.0;
            else
                return ab / (Math.Sqrt(aa) * Math.Sqrt(bb));
        }

    }
}
