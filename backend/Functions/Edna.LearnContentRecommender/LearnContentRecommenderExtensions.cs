using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Text;

namespace Edna.LearnContentRecommender
{
    public static class LearnContentRecommenderExtensions
    {
        public static string ToRecommenderId(this ITableEntity entity) => $"{entity.PartitionKey}_{entity.RowKey}";

        public static string ToAssignmentId(this string recommenderId)
        {
            if (string.IsNullOrEmpty(recommenderId))
                return "";
            string[] assignmentIdParts = recommenderId.Split("_");
            if (assignmentIdParts.Length != 3)
                return "";

            string assignmentId = assignmentIdParts[0] + "_" + assignmentIdParts[1];
            string level = assignmentIdParts[2];

            return assignmentId;
        }
        public static string ToLevel(this string recommenderId)
        {
            if (string.IsNullOrEmpty(recommenderId))
                return "";
            string[] assignmentIdParts = recommenderId.Split("_");
            if (assignmentIdParts.Length != 3)
                return "";

            string assignmentId = assignmentIdParts[0] + "_" + assignmentIdParts[1];
            string level = assignmentIdParts[2];

            return level;
        }
    }
}
