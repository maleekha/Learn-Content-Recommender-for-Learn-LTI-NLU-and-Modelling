using Microsoft.WindowsAzure.Storage.Table;

namespace Edna.LearnContentRecommender
{
    public class RecommendedLearnContentEntity : TableEntity
    {
        public string RecommendedContentUids { get; set; }
    }
}