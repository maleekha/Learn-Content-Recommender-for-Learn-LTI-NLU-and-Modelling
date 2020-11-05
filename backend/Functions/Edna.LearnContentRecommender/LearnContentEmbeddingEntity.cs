using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace Edna.LearnContentRecommender
{
    class LearnContentEmbeddingEntity : TableEntity
    {
        public string ContentUid;

        public string Level;

        public string Embedding;

    }
}
