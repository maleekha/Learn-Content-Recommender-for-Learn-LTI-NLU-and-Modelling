using Microsoft.WindowsAzure.Storage.Table;

namespace Edna.LearnContentRecommender
{
    public class Profile : AutoMapper.Profile
    {
        public Profile()
        {
            CreateMap<LearnContentRecommenderDto, LearnContentRecommenderEntity>()
                .ForMember(entity => entity.RowKey, expression => expression.MapFrom(dto => dto.ContentUid))
                .ReverseMap();
        }
    }
}