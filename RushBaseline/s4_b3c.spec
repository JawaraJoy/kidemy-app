# Batch 3c: literal kelas Quest* -> const c_ (IL harus identik). Backtick = tanda kutip ganda.
@C Assets/_dev/Scripts/Quest/Quest.cs Quest
+protected const int c_MaxStar = 3;
+private const float c_MusicDuckVolume = 0.4f;
@E Assets/_dev/Scripts/Quest/Quest.cs 148
-music.volume = 0.4f
+music.volume = c_MusicDuckVolume
@E Assets/_dev/Scripts/Quest/Quest.cs 163
-result ? 3 : 1
+result ? c_MaxStar : 1
@C Assets/_dev/Scripts/Quest/QuestColoring.cs QuestColoring
+private const string c_BrushColorProperty = `_BrushColor`;
+private const string c_BrushSizeProperty = `_BrushSize`;
+private const string c_RecalculatePalettesMethod = `RecalculatePalettesContainer`;
+private const int c_CursorOffset = 26;
+private const int c_CursorSize = 32;
+private const double c_ThreeStarAppraisal = 0.85;
@E Assets/_dev/Scripts/Quest/QuestColoring.cs 72
-`RecalculatePalettesContainer`
+c_RecalculatePalettesMethod
@E Assets/_dev/Scripts/Quest/QuestColoring.cs 96
-`_BrushColor`
+c_BrushColorProperty
@E Assets/_dev/Scripts/Quest/QuestColoring.cs 97
-`_BrushSize`
+c_BrushSizeProperty
@E Assets/_dev/Scripts/Quest/QuestColoring.cs 145
-new Rect(mousePosition.x - 26, Screen.height - mousePosition.y - 26, 32, 32)
+new Rect(mousePosition.x - c_CursorOffset, Screen.height - mousePosition.y - c_CursorOffset, c_CursorSize, c_CursorSize)
@E Assets/_dev/Scripts/Quest/QuestColoring.cs 169
-appraisal >= 0.85
+appraisal >= c_ThreeStarAppraisal
@E Assets/_dev/Scripts/Quest/QuestColoring.cs 170
-star = 3;
+star = c_MaxStar;
@C Assets/_dev/Scripts/Quest/Component/QuestColoringField.cs QuestColoringField
+private const string c_CommandBufferName = `UIPainter`;
+private const string c_BrushPosProperty = `_BrushPos`;
+private const int c_SampleSize = 64;
+private const float c_AlphaCutoff = 20f;
+private const int c_WhiteCutoff = 250;
@E Assets/_dev/Scripts/Quest/Component/QuestColoringField.cs 51
-`UIPainter`
+c_CommandBufferName
@E Assets/_dev/Scripts/Quest/Component/QuestColoringField.cs 90
-`_BrushPos`
+c_BrushPosProperty
@E Assets/_dev/Scripts/Quest/Component/QuestColoringField.cs 117
-GetTemporary(64, 64)
+GetTemporary(c_SampleSize, c_SampleSize)
@E Assets/_dev/Scripts/Quest/Component/QuestColoringField.cs 125
-new Texture2D(64, 64,
+new Texture2D(c_SampleSize, c_SampleSize,
@E Assets/_dev/Scripts/Quest/Component/QuestColoringField.cs 127
-new Rect(0, 0, 64, 64)
+new Rect(0, 0, c_SampleSize, c_SampleSize)
@E Assets/_dev/Scripts/Quest/Component/QuestColoringField.cs 138
-.a > 20f
+.a > c_AlphaCutoff
@E Assets/_dev/Scripts/Quest/Component/QuestColoringField.cs 143
-pixels[i].r < 250 || pixels[i].g < 250 || pixels[i].b < 250
+pixels[i].r < c_WhiteCutoff || pixels[i].g < c_WhiteCutoff || pixels[i].b < c_WhiteCutoff
@C Assets/_dev/Scripts/Quest/Component/QuestContainer.cs QuestContainer
+private const string c_ShadowName = `Shadow`;
@E Assets/_dev/Scripts/Quest/Component/QuestContainer.cs 11
-`Shadow`
+c_ShadowName
@C Assets/_dev/Scripts/Quest/Component/QuestDragNDropZone.cs QuestDragNDropZone
+private const float c_ItemScale = 0.75f;
+private const string c_AcceptState = `Accept`;
+private const string c_RejectState = `Reject`;
@E Assets/_dev/Scripts/Quest/Component/QuestDragNDropZone.cs 83
-Scaled(item, 0.75f)
+Scaled(item, c_ItemScale)
@E Assets/_dev/Scripts/Quest/Component/QuestDragNDropZone.cs 187
-Scaled(item, 0.75f)
+Scaled(item, c_ItemScale)
@E Assets/_dev/Scripts/Quest/Component/QuestDragNDropZone.cs 294
-`Accept`
+c_AcceptState
@E Assets/_dev/Scripts/Quest/Component/QuestDragNDropZone.cs 303
-`Reject`
+c_RejectState
@C Assets/_dev/Scripts/Quest/Component/QuestMatchingCard.cs QuestMatchingCard
+private const string c_HideMethod = `Hide`;
@E Assets/_dev/Scripts/Quest/Component/QuestMatchingCard.cs 73
-`Hide`
+c_HideMethod
@C Assets/_dev/Scripts/Quest/Component/QuestPaginator.cs QuestPaginator
+private const string c_PagePrefix = `Page `;
+private const string c_PageOf = ` of `;
@E Assets/_dev/Scripts/Quest/Component/QuestPaginator.cs 95
-`Page `
+c_PagePrefix
@E Assets/_dev/Scripts/Quest/Component/QuestPaginator.cs 95
-` of `
+c_PageOf
@C Assets/_dev/Scripts/Quest/Component/QuestRythimBeat.cs QuestRythimBeat
+private const string c_TapState = `Tap`;
+private const float c_TapFeedbackDelay = 0.1f;
+private const string c_NoteHeadTag = `NoteHead`;
@E Assets/_dev/Scripts/Quest/Component/QuestRythimBeat.cs 50
-`Tap`
+c_TapState
@E Assets/_dev/Scripts/Quest/Component/QuestRythimBeat.cs 60
-WaitForSeconds(0.1f)
+WaitForSeconds(c_TapFeedbackDelay)
@E Assets/_dev/Scripts/Quest/Component/QuestRythimBeat.cs 65
-WaitForSeconds(0.1f)
+WaitForSeconds(c_TapFeedbackDelay)
@E Assets/_dev/Scripts/Quest/Component/QuestRythimBeat.cs 76
-`NoteHead`
+c_NoteHeadTag
@C Assets/_dev/Scripts/Quest/Component/QuestRythimNote.cs QuestRythimNote
+private const int c_SpeedScale = 5000;
+private const string c_NoteEndTag = `NoteEnd`;
@E Assets/_dev/Scripts/Quest/Component/QuestRythimNote.cs 72
-m_Speed * 5000
+m_Speed * c_SpeedScale
@E Assets/_dev/Scripts/Quest/Component/QuestRythimNote.cs 109
-`NoteEnd`
+c_NoteEndTag
@C Assets/_dev/Scripts/Quest/QuestDragNDrop.cs QuestDragNDrop
+private const string c_RecalculateContainerMethod = `RecalculateContainer`;
+private const string c_GroupSegment = `_group_`;
+private const string c_LabelSegment = `_label_`;
+private const string c_QuestionSegment = `_question`;
@E Assets/_dev/Scripts/Quest/QuestDragNDrop.cs 171
-`RecalculateContainer`
+c_RecalculateContainerMethod
@E Assets/_dev/Scripts/Quest/QuestDragNDrop.cs 196
-star = 3;
+star = c_MaxStar;
@E Assets/_dev/Scripts/Quest/QuestDragNDrop.cs 302
-`_group_`
+c_GroupSegment
@E Assets/_dev/Scripts/Quest/QuestDragNDrop.cs 302
-`_label_`
+c_LabelSegment
@E Assets/_dev/Scripts/Quest/QuestDragNDrop.cs 321
-`_group_`
+c_GroupSegment
@E Assets/_dev/Scripts/Quest/QuestDragNDrop.cs 328
-`_question`
+c_QuestionSegment
@E Assets/_dev/Scripts/Quest/QuestDragNDrop.cs 339
-`_group_`
+c_GroupSegment
@E Assets/_dev/Scripts/Quest/QuestDragNDrop.cs 346
-`_question`
+c_QuestionSegment
@C Assets/_dev/Scripts/Quest/QuestMatching.cs QuestMatching
+private const string c_SuccessMethod = `Success`;
+private const string c_CloseAllCardMethod = `CloaseAllCard`;
+private const string c_RecalculateChoiceContainerMethod = `RecalculateChoiceContainer`;
@E Assets/_dev/Scripts/Quest/QuestMatching.cs 90
-`Success`
+c_SuccessMethod
@E Assets/_dev/Scripts/Quest/QuestMatching.cs 97
-`CloaseAllCard`
+c_CloseAllCardMethod
@E Assets/_dev/Scripts/Quest/QuestMatching.cs 154
-`RecalculateChoiceContainer`
+c_RecalculateChoiceContainerMethod
@C Assets/_dev/Scripts/Quest/QuestWordBuilder.cs QuestWordBuilder
+private const string c_RecalculateChoiceContainerMethod = `RecalculateChoiceContainer`;
@E Assets/_dev/Scripts/Quest/QuestWordBuilder.cs 110
-`RecalculateChoiceContainer`
+c_RecalculateChoiceContainerMethod
@E Assets/_dev/Scripts/Quest/QuestMultipleChoices.cs 258
-star = 3;
+star = c_MaxStar;
@C Assets/_dev/Scripts/Quest/QuestMultipleChoicesNarrative.cs QuestMultipleChoicesNarrative
+private const string c_SentenceEnd = `.`;
+private const string c_QuestionSegment = `_question`;
@E Assets/_dev/Scripts/Quest/QuestMultipleChoicesNarrative.cs 51
-`.`
+c_SentenceEnd
@E Assets/_dev/Scripts/Quest/QuestMultipleChoicesNarrative.cs 55
-`_question`
+c_QuestionSegment
@C Assets/_dev/Scripts/Quest/QuestRythimMaker.cs QuestRythimMaker
+private const string c_StartPlayMethod = `StartPlay`;
@E Assets/_dev/Scripts/Quest/QuestRythimMaker.cs 88
-`StartPlay`
+c_StartPlayMethod
@E Assets/_dev/Scripts/Quest/QuestRythimMaker.cs 123
-star = 3;
+star = c_MaxStar;
@E Assets/_dev/Scripts/Quest/QuestRythimMaker.cs 146
-`StartPlay`
+c_StartPlayMethod
@C Assets/_dev/Scripts/Quest/QuestShapeIt.cs QuestShapeIt
+private const int c_RotationStepCount = 4;
+private const int c_QuarterTurn = 90;
+private const float c_AngleEpsilon = 0.01f;
+private const int c_RotateStepDegrees = 3;
+private const int c_FullCircle = 360;
@E Assets/_dev/Scripts/Quest/QuestShapeIt.cs 120
-Random.Range(0, 4) * 90
+Random.Range(0, c_RotationStepCount) * c_QuarterTurn
@E Assets/_dev/Scripts/Quest/QuestShapeIt.cs 151
-z < 0.01f
+z < c_AngleEpsilon
@E Assets/_dev/Scripts/Quest/QuestShapeIt.cs 161
-Submit(3)
+Submit(c_MaxStar)
@E Assets/_dev/Scripts/Quest/QuestShapeIt.cs 176
-rotateValue = 90f;
+rotateValue = c_QuarterTurn;
@E Assets/_dev/Scripts/Quest/QuestShapeIt.cs 188
-eulerAngles.z + 3
+eulerAngles.z + c_RotateStepDegrees
@E Assets/_dev/Scripts/Quest/QuestShapeIt.cs 190
-eulerAngles.z == 360
+eulerAngles.z == c_FullCircle
@E Assets/_dev/Scripts/Quest/QuestShapeIt.cs 194
-rotateValue -= 3;
+rotateValue -= c_RotateStepDegrees;
@C Assets/_dev/Scripts/SO/SO_Quest.cs SO_Quest
+private const string c_QuestionSegment = `_question`;
@E Assets/_dev/Scripts/SO/SO_Quest.cs 60
-`_question`
+c_QuestionSegment
@E Assets/_dev/Scripts/SO/SO_Quest.cs 77
-`_question`
+c_QuestionSegment
@C Assets/_dev/Scripts/SO/SO_QuestMultipleChoice.cs SO_QuestMultipleChoice
+private const string c_ChoiceSegment = `_choice_`;
@E Assets/_dev/Scripts/SO/SO_QuestMultipleChoice.cs 51
-`_choice_`
+c_ChoiceSegment
@E Assets/_dev/Scripts/SO/SO_QuestMultipleChoice.cs 86
-`_choice_`
+c_ChoiceSegment
