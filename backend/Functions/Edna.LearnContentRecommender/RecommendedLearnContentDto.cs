namespace Edna.LearnContentRecommender
{
    public class RecommendedLearnContentDto
    {
        public string RecommenderId { get; set; } // assignmentId_level
        public string AssignmentId { get; set; }
        public string Level { get; set; }
        public string RecommendedContentUids { get; set; }
    }
}