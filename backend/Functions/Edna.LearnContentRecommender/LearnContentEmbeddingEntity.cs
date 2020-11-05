using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace Edna.LearnContentRecommender
{
    public class LearnContentEmbeddingEntity : TableEntity
    {
        public string Embedding { get; set; }
    }
}
