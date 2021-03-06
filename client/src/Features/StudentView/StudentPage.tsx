/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License.
 *--------------------------------------------------------------------------------------------*/

import React from 'react';
import { IThemeOnlyProps, SimpleComponentStyles, IStylesOnly } from '../../Core/Utils/FluentUI/typings.fluent-ui';
import { styled } from '@fluentui/react';
import { themedClassNames } from '../../Core/Utils/FluentUI';
import { StudentViewContent } from './StudentViewContent';
import { useStore } from '../../Stores/Core';
import { useObserver } from 'mobx-react-lite';
import { PageWrapper } from '../../Core/Components/Common/PageWrapper';

type StudentPageStyles = SimpleComponentStyles<'root'>;

const StudentPageInner = ({ styles }: IStylesOnly<StudentPageStyles>): JSX.Element => {
  const assignmentStore = useStore('assignmentStore');

  const classes = themedClassNames(styles);

  return useObserver(() => (
    <div className={classes.root}>
      <PageWrapper title={assignmentStore.assignment?.name || ''}>
        <StudentViewContent requirePublished />
      </PageWrapper>
    </div>
  ));
};

const StudentPageStyles = ({ theme }: IThemeOnlyProps): StudentPageStyles => ({
  root: [
    {
      display: 'flex',
      flexDirection: 'column',
      overflowY: 'auto',
      padding: theme.spacing.l2,
      paddingTop: 0
    }
  ]
});

export const StudentPage = styled(StudentPageInner, StudentPageStyles);
