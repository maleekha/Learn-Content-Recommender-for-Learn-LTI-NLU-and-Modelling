using System;
using System.Collections.Generic;
using System.Text;

namespace Edna.LearnContentRecommender
{
    public class LearnContentEmbeddingDto
    {
        public string ContentUid { get; set; }
        public string Embedding { get; set; }
        public string Level { get; set; }
    }
}
