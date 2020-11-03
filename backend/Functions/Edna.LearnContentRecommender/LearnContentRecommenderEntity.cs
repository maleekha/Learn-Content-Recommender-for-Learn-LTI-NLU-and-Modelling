using Microsoft.WindowsAzure.Storage.Table;

namespace Edna.LearnContentRecommender
{
    public class LearnContentRecommenderEntity : TableEntity
    {
        public string RecommendedContentUids { get; set; }
    }
}