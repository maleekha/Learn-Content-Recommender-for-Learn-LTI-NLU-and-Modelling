import {
    styled,
    Image,
    ImageFit,
    Link,
    Text,
    FontSizes,
    FontWeights,
    FontIcon,
    TooltipHost,
    TooltipOverflowMode,
    ITooltipHostStyles,
    AnimationClassNames,
    mergeStyles,
    Checkbox,
    mergeStyleSets,
    ICheckboxStyles,
    getTheme,
    classNamesFunction,
    ITheme
  } from '@fluentui/react';
import { Depths } from '@uifabric/fluent-theme';
  
import { useObserver } from "mobx-react-lite";
import React from "react";
import { checkBoxStyle } from '../../Core/Components/Common/Inputs/EdnaInputStyles';
import { themedClassNames } from '../../Core/Utils/FluentUI';
import { IStylesOnly, SimpleComponentStyles } from "../../Core/Utils/FluentUI/typings.fluent-ui";
import { useStore } from "../../Stores/Core/useStore.hook";
import { getTypeText } from '../MicrosoftLearn/MicrosoftLearnItemMappings';
import { getCommonHorizontalSpacer, getCommonSpacingStyle } from '../MicrosoftLearn/MicrosoftLearnStyles';

export type MicrosoftLearnRecommendedContentItemStyles = SimpleComponentStyles<'root' | 'topBar'  | 'itemIcon' | 'tooltipHost' | 'details' | 'info' | 'dot' | 'content' >

interface MicrosoftLearnRecommendedItemProps {
    recommendedContentId: string;
    level: string;
}

type MicrosoftLearnRecommendedItemStyleProps = {
    theme: ITheme;
    level: string;
    isSelected: boolean;

}

const MicrosoftLearnRecommendedContentItemInner = ( {styles, recommendedContentId, level } : MicrosoftLearnRecommendedItemProps & IStylesOnly<MicrosoftLearnRecommendedContentItemStyles>): JSX.Element => {
    const learnStore = useStore('microsoftLearnStore');

    const checkboxClass = mergeStyleSets(
        themedClassNames(checkBoxStyle),
        themedClassNames(microsoftLearnItemCheckBoxStyle)
      );

    return useObserver(() => {
        const item = learnStore.catalog?.contents.get(recommendedContentId);
        const isSelected = !!learnStore.selectedItems?.find(selectedItem => selectedItem.contentUid === item?.uid);

        const classNames = classNamesFunction<MicrosoftLearnRecommendedItemStyleProps, MicrosoftLearnRecommendedContentItemStyles>()(styles, {
            theme: getTheme(),
            level,
            isSelected
        });
        const classes = themedClassNames(classNames);

        if(!item){
            return <></>;
        }
        return (
        <div className={mergeStyles(classes.root, AnimationClassNames.slideUpIn20)} onClick = {(event) => learnStore.toggleItemSelection(item.uid)}>
            <div className={mergeStyles(classes.topBar)}></div>
            <div style={{display: 'flex', flexDirection: 'row'}}>
            <Image src={item.icon_url} className={classes.itemIcon} imageFit={ImageFit.contain} />
            <div className={classes.content}>
              <Link target="_blank" href={item.url}>
                <TooltipHost
                  overflowMode={TooltipOverflowMode.Self}
                  hostClassName={classes.tooltipHost}
                  content={item.title}
                  styles={hostStyles}
                >
                  {item.title}
                </TooltipHost>
              </Link>
              <div className={classes.details}>
                <Text variant="mediumPlus" className={classes.info}>
                  {level.charAt(0).toUpperCase()+level.slice(1)}
                </Text>
                <FontIcon iconName="LocationDot" className={classes.dot} />
                <Text variant="mediumPlus" className={classes.info}>
                  {getTypeText(item.type)}
                </Text> 
              </div>
            </div>
            <Checkbox styles={checkboxClass} checked={isSelected} onClick={event => event.stopPropagation()} />
            </div>
          </div>

        )
    })
}

export const microsoftLearnItemCheckBoxStyle = (): Partial<ICheckboxStyles> => ({
    root: [
      {
        pointerEvents: 'none'
      }
    ]
  });

export const microsoftLearnRecommendedItemStyles = ({ theme, level, isSelected }: MicrosoftLearnRecommendedItemStyleProps): MicrosoftLearnRecommendedContentItemStyles => ({  
    root: [
      mergeStyles(getCommonSpacingStyle(theme), {
        boxSizing: 'border-box',
        width: `calc(100% - ${getCommonHorizontalSpacer(theme)} * 2)`,
        height: `calc(${theme.spacing.l1}*5 + ${theme.spacing.s1})`,
        borderRadius: 3,
        border: '1px solid black',
        borderColor: theme.palette.neutralTertiaryAlt,
        boxShadow: Depths.depth4,
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        padding: theme.spacing.s1,
        margin: theme.spacing.l1
      })
    ],
    itemIcon: [
      {
        marginRight: theme.spacing.s1,
        marginLeft: theme.spacing.s1,
        height: `calc(2*${theme.spacing.l2})`,
        width: `calc(2*${theme.spacing.l2})`
      }
    ],
    content: [
      {
        alignSelf: 'stretch',
        flex: 1,
        padding: theme.spacing.s1,
        overflow: 'hidden'
      }
    ],
    tooltipHost: [
      {
        overflow: 'hidden',
        textOverflow: 'ellipsis',
        whiteSpace: 'nowrap',
        flex: 1,
        fontSize: FontSizes.medium,
        fontWeight: FontWeights.bold,
        color: theme.palette.neutralSecondary,
        marginBottom: theme.spacing.s1
      }
    ],
    details: [
      {
        display: 'flex',
        alignItems: 'center'
      }
    ],
    info: [
      {
        color: theme.palette.neutralSecondaryAlt,
        fontWeight: FontWeights.semibold
      }
    ],
    dot: [
      {
        fontSize: FontSizes.small,
        marginTop: theme.spacing.s2,
        marginRight: theme.spacing.s1,
        marginLeft: theme.spacing.s1,
        color: theme.palette.neutralTertiaryAlt,
        boxSizing: 'border-box'
      }
    ],
    topBar: [
        {
            width: `100%`,
            backgroundColor: level === 'beginner' ? 'green' : level === 'intermediate' ? 'gold' : 'red',
            height: `20px`,
            paddingLeft: theme.spacing.l1,
            borderWidth: `0px`,
            borderStyle: 'solid',
            borderRadius: `${3}px ${3}px 0 0`,
            margin: `-10px 0px 10px 0px`
          }
    ]
  });
  
  const hostStyles: Partial<ITooltipHostStyles> = {
    root: { display: 'block' }
  };
  
export const MicrosoftLearnRecommendedContentItem = styled(MicrosoftLearnRecommendedContentItemInner, microsoftLearnRecommendedItemStyles)