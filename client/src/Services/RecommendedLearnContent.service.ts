import axios from 'axios';
import { safeData, WithError } from '../Core/Utils/Axios/safeData';
import { RecommendedLearnContentDto } from '../Dtos/RecommendedLearnContent.dto';

class RecommendedLearnContentServiceClass {

    public async getRecommendedLearnContent(assignmentId: string): Promise<WithError<RecommendedLearnContentDto[]>> {
        const recommendedLearnContentResponse = await axios.get<RecommendedLearnContentDto[]>(
          `${process.env.REACT_APP_EDNA_RECOMMENDED_LEARN_CONTENT}/assignments/${assignmentId}/recommended-learn-content`
        );
    
        return safeData(recommendedLearnContentResponse);
      }

}

export const RecommendedLearnContentService = new RecommendedLearnContentServiceClass();