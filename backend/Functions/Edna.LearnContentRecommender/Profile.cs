using Microsoft.WindowsAzure.Storage.Table;

namespace Edna.LearnContentRecommender
{
    public class Profile : AutoMapper.Profile
    {
        public Profile()
        {
            CreateMap<LearnContentRecommenderDto, LearnContentRecommenderEntity>()
                .ForMember(entity => entity.PartitionKey, expression => expression.MapFrom(dto => dto.AssignmentId))
                .ForMember(entity => entity.RowKey, expression => expression.MapFrom(dto => dto.Level))
                .ReverseMap()
                .ForMember(dto => dto.RecommenderId, expression => expression.MapFrom(entity => entity.ToRecommenderId()));
        }
    }
}