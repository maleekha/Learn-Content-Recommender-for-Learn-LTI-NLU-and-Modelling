import { ChildStore } from './Core';
import { observable, action } from 'mobx';
import { RecommendedLearnContent } from '../Models/RecommendedLearnContent.model';
import { toObservable } from '../Core/Utils/Mobx/toObservable';
import _ from 'lodash';
import { switchMap, filter, map } from 'rxjs/operators';
import { RecommendedLearnContentService } from '../Services/RecommendedLearnContent.service';
import { RecommendedLearnContentDto } from '../Dtos/RecommendedLearnContent.dto';

export class RecommendedLearnContentStore extends ChildStore {
  @observable recommendedItems: RecommendedLearnContent[] = [];

  initialize(): void {      
    toObservable(() => this.root.assignmentStore.assignment)
      .pipe(
        filter(assignment => !!assignment),
        map(assignment => assignment!.id),
        switchMap(assignmentId => RecommendedLearnContentService.getRecommendedLearnContent(assignmentId)),
        filter(assignmentLearnContent => !assignmentLearnContent.error),
        map(assignmentLearnContent => assignmentLearnContent as RecommendedLearnContentDto[])
      )
      .subscribe(recommendedItems => {
        this.recommendedItems = recommendedItems;
      });
  }

  // @action
  // selectRecommendedCourses(level: string) {
  //   const selectMicrosoftLearnContent = (contentUids: string[]) => contentUids.forEach(uid => this.root.microsoftLearnStore.toggleItemSelection(uid));
  //   selectMicrosoftLearnContent(this.recommendedItems.filter(item => item.level===level)[0].recommendedContentUids.split(','));
  // }
}