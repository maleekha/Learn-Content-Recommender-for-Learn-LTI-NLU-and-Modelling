using Microsoft.WindowsAzure.Storage.Table;

namespace Edna.LearnContentRecommender
{
    public class Profile : AutoMapper.Profile
    {
        public Profile()
        {
            CreateMap<LearnContentRecommenderDto, LearnContentRecommenderEntity>()
                .ForMember(entity => entity.PartitionKey, expression => expression.MapFrom(dto => dto.RecommenderId))
                .ForMember(entity => entity.RowKey, expression => expression.MapFrom(dto => dto.RecommendedContentUids))
                .ReverseMap()
                .ForMember(dto => dto.AssignmentId, expression => expression.MapFrom(entity => entity.PartitionKey.ToAssignmentId()))
                .ForMember(dto => dto.Level, expression => expression.MapFrom(entity => entity.PartitionKey.ToLevel()));
        }
    }
}