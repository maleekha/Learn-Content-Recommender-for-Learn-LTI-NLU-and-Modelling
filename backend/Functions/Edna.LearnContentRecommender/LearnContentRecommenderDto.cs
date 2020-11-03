namespace Edna.LearnContentRecommender
{
    public class LearnContentRecommenderDto
    {
        public string RecommenderId { get; set; } // assignmentId_level
        public string AssignmentId { get; set; }
        public Level Level { get; set; }
        public string RecommendedContentUids { get; set; }
    }
}