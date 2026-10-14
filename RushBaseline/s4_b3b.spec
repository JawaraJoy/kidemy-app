# Batch 3b: literal runtime non-Quest -> const c_ (IL harus identik). Backtick = tanda kutip ganda.
@C Assets/_dev/Scripts/Manager/GameManager.cs GameManager
+private const string c_ParamRedirectUrl = `redirect_url`;
+private const string c_ParamBgmVolume = `bgm_volume`;
+private const string c_ParamSessionId = `session_id`;
+private const float c_BgmVolumeScale = 10f;
+private const int c_SecondsPerMinute = 60;
+private const string c_TimerSeparator = `:`;
+private const int c_TimerMajorTick = 10;
+private const int c_TimerMinorTick = 5;
+private const int c_MaxStar = 3;
+private const string c_ColorTheIslandKey = `ColortheIsland`;
+private const string c_DialogCorrect = `Yaaay! Amazing!`;
+private const string c_DialogWrong = `Oh no!`;
@E Assets/_dev/Scripts/Manager/GameManager.cs 196
-`redirect_url`
+c_ParamRedirectUrl
@E Assets/_dev/Scripts/Manager/GameManager.cs 198
-`redirect_url`
+c_ParamRedirectUrl
@E Assets/_dev/Scripts/Manager/GameManager.cs 201
-`bgm_volume`
+c_ParamBgmVolume
@E Assets/_dev/Scripts/Manager/GameManager.cs 203
-`bgm_volume`
+c_ParamBgmVolume
@E Assets/_dev/Scripts/Manager/GameManager.cs 204
-volume / 10f
+volume / c_BgmVolumeScale
@E Assets/_dev/Scripts/Manager/GameManager.cs 205
-volume / 10f
+volume / c_BgmVolumeScale
@E Assets/_dev/Scripts/Manager/GameManager.cs 220
-`session_id`
+c_ParamSessionId
@E Assets/_dev/Scripts/Manager/GameManager.cs 221
-`session_id`
+c_ParamSessionId
@E Assets/_dev/Scripts/Manager/GameManager.cs 340
-elapsedTime / 60
+elapsedTime / c_SecondsPerMinute
@E Assets/_dev/Scripts/Manager/GameManager.cs 341
-elapsedTime % 60
+elapsedTime % c_SecondsPerMinute
@E Assets/_dev/Scripts/Manager/GameManager.cs 343
-`:`
+c_TimerSeparator
@E Assets/_dev/Scripts/Manager/GameManager.cs 345
-s % 10 == 0
+s % c_TimerMajorTick == 0
@E Assets/_dev/Scripts/Manager/GameManager.cs 347
-s % 5 == 0
+s % c_TimerMinorTick == 0
@E Assets/_dev/Scripts/Manager/GameManager.cs 521
-star > 3
+star > c_MaxStar
@E Assets/_dev/Scripts/Manager/GameManager.cs 522
-star = 3;
+star = c_MaxStar;
@E Assets/_dev/Scripts/Manager/GameManager.cs 524
-star == 3
+star == c_MaxStar
@E Assets/_dev/Scripts/Manager/GameManager.cs 578
-`ColortheIsland`
+c_ColorTheIslandKey
@E Assets/_dev/Scripts/Manager/GameManager.cs 611
-`Yaaay! Amazing!`
+c_DialogCorrect
@E Assets/_dev/Scripts/Manager/GameManager.cs 634
-`Oh no!`
+c_DialogWrong
@C Assets/_dev/Scripts/Manager/ContentManager.cs ContentManager
+private const int c_RefResExtraHeight = 200;
@E Assets/_dev/Scripts/Manager/ContentManager.cs 25
-Vector2.up * 200
+Vector2.up * c_RefResExtraHeight
@C Assets/_dev/Scripts/Global.cs Global
+private const int c_FalseVectorScale = 100000;
@E Assets/_dev/Scripts/Global.cs 7
-Vector2.one * 100000
+Vector2.one * c_FalseVectorScale
@C Assets/_dev/Scripts/APIWrapper/VoiceRequest.cs VoiceRequest
+private const string c_DefaultFormat = `wav`;
@E Assets/_dev/Scripts/APIWrapper/VoiceRequest.cs 12
-`wav`
+c_DefaultFormat
@C Assets/_dev/Scripts/Util/StringHelper.cs StringHelper
+private const string c_HexFormat = `x2`;
+private const int c_MinSegmentCount = 5;
+private const string c_LabelSegment = `label`;
+private const string c_GroupSegment = `group`;
@E Assets/_dev/Scripts/Util/StringHelper.cs 25
-`x2`
+c_HexFormat
@E Assets/_dev/Scripts/Util/StringHelper.cs 60
-length < 5
+length < c_MinSegmentCount
@E Assets/_dev/Scripts/Util/StringHelper.cs 63
-`label`
+c_LabelSegment
@E Assets/_dev/Scripts/Util/StringHelper.cs 63
-`group`
+c_GroupSegment
@C Assets/_dev/Scripts/Util/URIHelper.cs URIHelper
+private const string c_QueryMark = `?`;
+private const string c_PairSeparator = `&`;
+private const string c_KeyValueSeparator = `=`;
+private const string c_SchemeSeparator = `://`;
+private const string c_PathSeparator = `/`;
@E Assets/_dev/Scripts/Util/URIHelper.cs 14
-`?`
+c_QueryMark
@E Assets/_dev/Scripts/Util/URIHelper.cs 19
-`&`
+c_PairSeparator
@E Assets/_dev/Scripts/Util/URIHelper.cs 25
-`=`
+c_KeyValueSeparator
@E Assets/_dev/Scripts/Util/URIHelper.cs 47
-`://`
+c_SchemeSeparator
@E Assets/_dev/Scripts/Util/URIHelper.cs 62
-`/`
+c_PathSeparator
@E Assets/_dev/Scripts/Util/URIHelper.cs 110
-`?`
+c_QueryMark
@E Assets/_dev/Scripts/Util/URIHelper.cs 110
-`&`
+c_PairSeparator
@C Assets/_dev2/Addon/TextAnimation/Scripts/WordSetting.cs WordSetting
+private const string c_WordSeparator = ` `;
@E Assets/_dev2/Addon/TextAnimation/Scripts/WordSetting.cs 42
-` `
+c_WordSeparator
@E Assets/_dev2/Addon/TextAnimation/Scripts/WordSetting.cs 48
-` `
+c_WordSeparator
@C Assets/_dev2/Quest/Scripts/AnimationHandler.cs AnimationHandler
+private const float c_ResultDelay = 1.5f;
+private const float c_MusicDuckVolume = 0.4f;
@E Assets/_dev2/Quest/Scripts/AnimationHandler.cs 108
-WaitForSeconds(1.5f)
+WaitForSeconds(c_ResultDelay)
@E Assets/_dev2/Quest/Scripts/AnimationHandler.cs 111
-music.volume = 0.4f
+music.volume = c_MusicDuckVolume
@C Assets/_dev2/Quest/Scripts/ResultPanel.cs ResultPanel
+private const float c_Percent = 100f;
@E Assets/_dev2/Quest/Scripts/ResultPanel.cs 62
-GetRate() * 100f
+GetRate() * c_Percent
@C Assets/_dev2/Quest/Scripts/StarPresenter.cs StarPresenter
+private const string c_ReactionZeroStar = `Oh nooo`;
+private const string c_ReactionOneStar = `Good Job!`;
+private const string c_ReactionTwoStar = `Excellent!!`;
+private const string c_ReactionThreeStar = `Smart Kid!!!`;
+private const float c_StarRevealDelay = 0.2f;
@E Assets/_dev2/Quest/Scripts/StarPresenter.cs 65
-`Oh nooo`
+c_ReactionZeroStar
@E Assets/_dev2/Quest/Scripts/StarPresenter.cs 69
-`Good Job!`
+c_ReactionOneStar
@E Assets/_dev2/Quest/Scripts/StarPresenter.cs 74
-`Excellent!!`
+c_ReactionTwoStar
@E Assets/_dev2/Quest/Scripts/StarPresenter.cs 79
-`Smart Kid!!!`
+c_ReactionThreeStar
@E Assets/_dev2/Quest/Scripts/StarPresenter.cs 92
-WaitForSeconds(0.2f)
+WaitForSeconds(c_StarRevealDelay)
@C Assets/_dev2/Scripts/TweenHandler.cs TweenField
+private const int c_RandomIdMin = 1000;
+private const int c_RandomIdMax = 9999;
+private const float c_ColorFadeFrom = 0.1f;
@E Assets/_dev2/Scripts/TweenHandler.cs 265
-Range(1000, 9999)
+Range(c_RandomIdMin, c_RandomIdMax)
@E Assets/_dev2/Scripts/TweenHandler.cs 437
-m_Content, 0.1f,
+m_Content, c_ColorFadeFrom,
@C Assets/_dev/Scripts/Util/FeedbackItem.cs FeedbackItem
+private const string c_PlayMethod = `ExecPlay`;
+private const string c_StopMethod = `ExecStop`;
@E Assets/_dev/Scripts/Util/FeedbackItem.cs 14
-`ExecPlay`
+c_PlayMethod
@E Assets/_dev/Scripts/Util/FeedbackItem.cs 22
-`ExecStop`
+c_StopMethod
@C Assets/_dev/Scripts/Util/FeedbackItemAnimation.cs FeedbackItemAnimation
+private const string c_IdleState = `Idle`;
@E Assets/_dev/Scripts/Util/FeedbackItemAnimation.cs 30
-`Idle`
+c_IdleState
@C Assets/_dev/Scripts/Util/HandTutorial.cs HandTutorial
+private const string c_TrailOriginName = `TrailOrigin`;
+private const float c_ClickWaitSeconds = 1f;
+private const float c_DefaultWaitSeconds = 0.2f;
@E Assets/_dev/Scripts/Util/HandTutorial.cs 73
-`TrailOrigin`
+c_TrailOriginName
@E Assets/_dev/Scripts/Util/HandTutorial.cs 165
-1f : 0.2f
+c_ClickWaitSeconds : c_DefaultWaitSeconds
