import React from 'react';
import { SimpleComponentStyles, IStylesOnly, IThemeOnlyProps } from '../../Core/Utils/FluentUI/typings.fluent-ui';
import {
  styled,
  Text,
  FontWeights,
  FontSizes,
  Spinner,
  SpinnerSize,
} from '@fluentui/react';
import { useStore } from '../../Stores/Core';
import { FIXED_ITEM_WIDTH } from '../MicrosoftLearn/MicrosoftLearnStyles';
import { useObserver } from 'mobx-react-lite';
import { themedClassNames } from '../../Core/Utils/FluentUI';
import { MicrosoftLearnRecommendedContentItem } from './MicrosoftLearnRecommendedContentItem';

export type RecommendedLearnContentsStyles = SimpleComponentStyles<
  'root' | 'header' | 'title' | 'spinner' | 'list'
>;

const RecommendedLearnContentsInner = ({
  styles
}: IStylesOnly<RecommendedLearnContentsStyles>): JSX.Element => {
  const recommendedLearnContentStore = useStore('recommendedLearnContentStore');
  const learnStore = useStore('microsoftLearnStore');
  const beginnerContentUid: string[] = recommendedLearnContentStore.recommendedItems[0]?.recommendedContentUids.split(',');
  const intermediateContentUid: string[] = recommendedLearnContentStore.recommendedItems[1]?.recommendedContentUids.split(',');
  const advancedContentUid: string[] = recommendedLearnContentStore.recommendedItems[2]?.recommendedContentUids.split(',');

  // console.log(beginnerContentUid, intermediateContentUid, advancedContentUid, "contentuids");
 // const beginnerContentUid : string[] = ["learn.azure.move-azure-resources-to-another-resource-group", "learn.azure.move-azure-resources-to-another-resource-group" ];
 // const intermediateContentUid : string[] = ["learn.azure.move-azure-resources-to-another-resource-group", "learn.azure.move-azure-resources-to-another-resource-group" ];
 // const advancedContentUid : string[] = ["learn.azure.move-azure-resources-to-another-resource-group", "learn.azure.move-azure-resources-to-another-resource-group" ];
  return useObserver(() => {
    const classes = themedClassNames(styles);

    return (
      <div className={classes.root}>
        <div>
          <div className={classes.header}>
            <Text variant="medium" className={classes.title}>
              {`Beginner level recommended content`}
            </Text>
            {( !recommendedLearnContentStore.recommendedItems || learnStore.isLoadingCatalog) && (
              <Spinner size={SpinnerSize.small} className={classes.spinner} />
            )}
          </div>
          { beginnerContentUid && beginnerContentUid.length > 0 && !beginnerContentUid.includes("") ? (
          <div className={classes.list}>
            {beginnerContentUid.map( contentUid =>
              <MicrosoftLearnRecommendedContentItem recommendedContentId = {contentUid} level = "beginner" />
            )}
          </div>
          ): (
            <div>
            <Text variant = "small">
              {`Sorry, No beginner level content recommendation available.`}
            </Text>
            </div>
          )}
        </div>
        <div>
          <div className={classes.header}>
            <Text variant="medium" className={classes.title}>
              {`Intermediate level recommended content`}
            </Text>
            {(!recommendedLearnContentStore.recommendedItems || learnStore.isLoadingCatalog) && (
              <Spinner size={SpinnerSize.small} className={classes.spinner} />
            )}
          </div>
          {intermediateContentUid && intermediateContentUid.length > 0 && !intermediateContentUid.includes("") ? (
          <div className={classes.list}>
            {intermediateContentUid.map( contentUid =>
              <MicrosoftLearnRecommendedContentItem recommendedContentId = {contentUid} level = "intermediate" />
            )}
          </div>
          ):(
            <div>
            <Text variant = "small">
              {`Sorry, No intermediate level content recommendation available.`}
            </Text>
            </div>
          )}
        </div>
        <div>
          <div className={classes.header}>
            <Text variant="medium" className={classes.title}>
              {`Advanced level recommended content`}
            </Text>
            {(!recommendedLearnContentStore.recommendedItems || learnStore.isLoadingCatalog) && (
              <Spinner size={SpinnerSize.small} className={classes.spinner} />
            )}
          </div>
          {advancedContentUid && advancedContentUid.length > 0 && !advancedContentUid.includes("") ? (
          <div className={classes.list}>
            {advancedContentUid.map( contentUid =>
              <MicrosoftLearnRecommendedContentItem recommendedContentId = {contentUid} level = "advanced" />
            )}
          </div>
          ) : (
            <div>
            <Text variant = "small">
              {`Sorry, No advanced level content recommendation available.`}
            </Text>
            </div>
          )}
        </div>
      </div>

    );
  });
};

const recommendedLearnContentsStyles = ({ theme }: IThemeOnlyProps): RecommendedLearnContentsStyles => ({
  root: [
    {
      marginRight: theme.spacing.s1
    }
  ],
  header: [
    {
      display: 'flex',
      boxSizing: 'border-box',
      alignItems: 'center',
      flexWrap: 'wrap',
      paddingTop: theme.spacing.l1
    }
  ],
  title: [
    {
      color: theme.palette.neutralPrimary,
      fontWeight: FontWeights.semibold,
      lineHeight: FontSizes.xLargePlus
    }
  ],
  spinner: [
    {
      marginLeft: theme.spacing.s1
    }
  ],
  list: [
    {
      display: 'grid',
      overflowY: 'hidden',
      overflowX: 'hidden',
      gridRowGap: theme.spacing.m,
      height: 'min-content',
      gridTemplateColumns: `repeat(auto-fill,minmax(${FIXED_ITEM_WIDTH}px, 1fr) )`,
      marginBottom: `calc(${theme.spacing.l2} - ${theme.spacing.s1})`,
      marginTop: theme.spacing.m
    }
  ]
});

export const RecommendedLearnContents = styled(
    RecommendedLearnContentsInner,
    recommendedLearnContentsStyles
);
