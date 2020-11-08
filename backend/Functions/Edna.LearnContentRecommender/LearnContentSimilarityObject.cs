using System.Collections.Generic;

namespace Edna.LearnContentRecommender
{
    internal class LearnContentSimilarityObject
    {
        public string ContentUid;
        public string Level;
        public double Similarity;
    }

    internal class LearnContentDict
    {
        public List<LearnContentSimilarityObject> SimilarityObjects { get; set; }
    }
}