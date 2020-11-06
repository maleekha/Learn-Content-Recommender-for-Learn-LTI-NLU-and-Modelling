import React from 'react';
import { SimpleComponentStyles, IStylesOnly, IThemeOnlyProps } from '../../Core/Utils/FluentUI/typings.fluent-ui';
import {
  styled,
  Text,
  FontWeights,
  FontSizes,
  Spinner,
  SpinnerSize,
  IButtonStyles,
  IButtonProps
} from '@fluentui/react';
import { useStore } from '../../Stores/Core';
import { MicrosoftLearnSelectedItem } from '../MicrosoftLearn/MicrosoftLearnSelectedItem';
import { FIXED_ITEM_WIDTH } from '../MicrosoftLearn/MicrosoftLearnStyles';
import { useObserver } from 'mobx-react-lite';
import { themedClassNames } from '../../Core/Utils/FluentUI';
import { MicrosoftLearnSelectedItemShimmer } from '../MicrosoftLearn/MicrosoftLearnSelectedItemShimmer';

export type RecommendedLearnContentsStyles = SimpleComponentStyles<
  'root' | 'header' | 'title' | 'spinner' | 'list'
>;

const recommendedLearnContentsInner = ({
  styles
}: IStylesOnly<RecommendedLearnContentsStyles>): JSX.Element => {
  const recommendedLearnContentStore = useStore('recommendedLearnContentStore');
  const learnStore = useStore('microsoftLearnStore');
  const beginnerContentUid: string[] = recommendedLearnContentStore.recommendedItems[0].recommendedContentUids.split(',');
  const intermediateContentUid: string[] = recommendedLearnContentStore.recommendedItems[1].recommendedContentUids.split(',');
  const advancedContentUid: string[] = recommendedLearnContentStore.recommendedItems[2].recommendedContentUids.split(',');

  return useObserver(() => {
    const classes = themedClassNames(styles);

    return (
      <div className={classes.root}>
        <div>
          <div className={classes.header}>
            <Text variant="medium" className={classes.title}>
              {`Beginner level recommended content`}
            </Text>
            {(!recommendedLearnContentStore.recommendedItems || learnStore.isLoadingCatalog) && (
              <Spinner size={SpinnerSize.small} className={classes.spinner} />
            )}
          </div>
          {beginnerContentUid.length > 0 && (
          <div className={classes.list}>
              {recommendedLearnContentStore.recommendedItems?.map(item =>
              learnStore.catalog ? (
                <MicrosoftLearnSelectedItem key={item.recommendedContentUids} itemId={item.recommendedContentUids} /> // change this to recommendedLearnCOntentSelectedItem
              ) : (
                <MicrosoftLearnSelectedItemShimmer />
              )
              )}
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
          {intermediateContentUid.length>0 && (
          <div className={classes.list}>
            {learnStore.selectedItems?.map(item =>
              learnStore.catalog ? (
                <MicrosoftLearnSelectedItem key={item.contentUid} itemId={item.contentUid} /> // change this to recommendedLearnCOntentSelectedItem
              ) : (
                <MicrosoftLearnSelectedItemShimmer />
              )
            )}
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
          {advancedContentUid.length>0 && (
          <div className={classes.list}>
            {learnStore.selectedItems?.map(item =>
              learnStore.catalog ? (
                <MicrosoftLearnSelectedItem key={item.contentUid} itemId={item.contentUid} /> // change this to recommendedLearnCOntentSelectedItem
              ) : (
                <MicrosoftLearnSelectedItemShimmer />
              )
            )}
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
      gridRowGap: theme.spacing.m,
      height: 'min-content',
      gridTemplateColumns: `repeat(auto-fill,minmax(${FIXED_ITEM_WIDTH}px, 1fr) )`,
      marginBottom: `calc(${theme.spacing.l2} - ${theme.spacing.s1})`,
      marginTop: theme.spacing.m
    }
  ]
});

export const RecommendedLearnContents = styled(
    recommendedLearnContentsInner,
    recommendedLearnContentsStyles
);
