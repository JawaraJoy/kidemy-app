# Batch 4: literal kode Editor (polos) -> const c_. Fragmen dalam string interpolasi sengaja tidak diubah. Backtick = tanda kutip ganda.
@C Assets/_dev/Scripts/Util/BatchScenesBuilder.cs BatchScenesBuilder
+private const string c_BuildOutputFolder = `../Builds/HTML5`;
@E Assets/_dev/Scripts/Util/BatchScenesBuilder.cs 21
-`../Builds/HTML5`
+c_BuildOutputFolder
@C Assets/_dev/Scripts/Util/Editor/MissingScriptFinder.cs MissingScriptFinder
+private const string c_PrefabFilter = `t:Prefab`;
@E Assets/_dev/Scripts/Util/Editor/MissingScriptFinder.cs 28
-`t:Prefab`
+c_PrefabFilter
@C Assets/_dev/Scripts/Util/Editor/QuestConverter.cs QuestConverter
+private const string c_PrefabExtension = `.prefab`;
+private const string c_ScriptProperty = `m_Script`;
@E Assets/_dev/Scripts/Util/Editor/QuestConverter.cs 26
-`.prefab`
+c_PrefabExtension
@E Assets/_dev/Scripts/Util/Editor/QuestConverter.cs 42
-`m_Script`
+c_ScriptProperty
@E Assets/_dev/Scripts/Util/Editor/QuestConverter.cs 90
-`.prefab`
+c_PrefabExtension
@C Assets/_dev/Scripts/Util/Editor/UpdateMultipleChoiceToNarrative.cs UpdateMultipleChoiceToNarrative
+private const string c_ScriptProperty = `m_Script`;
@E Assets/_dev/Scripts/Util/Editor/UpdateMultipleChoiceToNarrative.cs 67
-`m_Script`
+c_ScriptProperty
@C Assets/_dev/Scripts/Util/QuestAudioPopulatorWindow.cs QuestAudioPopulatorWindow
+private const string c_WindowTitle = `Quest Audio Populator`;
+private const string c_HeaderLabel = `Quest Audio Batch Populator`;
+private const string c_QuestAssetsLabel = `Quest Assets`;
+private const string c_QuestAssetsHelp = `Add the SO_Quest assets that should have their audio populated.`;
+private const string c_ForceRedownloadLabel = `Force Redownload Existing`;
+private const string c_PopulateButton = `Populate Selected Quest Audio`;
+private const int c_SpaceSmall = 5;
+private const int c_SpaceLarge = 10;
+private const int c_ButtonHeight = 35;
@E Assets/_dev/Scripts/Util/QuestAudioPopulatorWindow.cs 20
-`Quest Audio Populator`
+c_WindowTitle
@E Assets/_dev/Scripts/Util/QuestAudioPopulatorWindow.cs 25
-`Quest Audio Batch Populator`
+c_HeaderLabel
@E Assets/_dev/Scripts/Util/QuestAudioPopulatorWindow.cs 26
-Space(5)
+Space(c_SpaceSmall)
@E Assets/_dev/Scripts/Util/QuestAudioPopulatorWindow.cs 32
-`Quest Assets`
+c_QuestAssetsLabel
@E Assets/_dev/Scripts/Util/QuestAudioPopulatorWindow.cs 37
-`Add the SO_Quest assets that should have their audio populated.`
+c_QuestAssetsHelp
@E Assets/_dev/Scripts/Util/QuestAudioPopulatorWindow.cs 40
-`Force Redownload Existing`
+c_ForceRedownloadLabel
@E Assets/_dev/Scripts/Util/QuestAudioPopulatorWindow.cs 42
-Space(10)
+Space(c_SpaceLarge)
@E Assets/_dev/Scripts/Util/QuestAudioPopulatorWindow.cs 45
-`Populate Selected Quest Audio`
+c_PopulateButton
@E Assets/_dev/Scripts/Util/QuestAudioPopulatorWindow.cs 45
-Height(35)
+Height(c_ButtonHeight)
@I Assets/_dev/Scripts/Util/SceneButtonGenerator.cs 27
+    private const string c_NoisePattern = @`(rewrite|otherdev|dev|_|\d+|\[\d+\])`;
+    private const string c_CamelBoundaryPattern = @`(?<=[a-z])(?=[A-Z])`;
+    private const string c_WhitespacePattern = @`\s+`;
+    private const string c_Space = ` `;
+    private const string c_ButtonPrefix = `Btn_`;
+    private const string c_UndoGenerateButton = `Generate Scene Button`;
+    private const string c_EventSystemName = `EventSystem`;
+    private const string c_UndoCreateEventSystem = `Create EventSystem`;
+
@E Assets/_dev/Scripts/Util/SceneButtonGenerator.cs 62
-@`(rewrite|otherdev|dev|_|\d+|\[\d+\])`
+c_NoisePattern
@E Assets/_dev/Scripts/Util/SceneButtonGenerator.cs 64
-@`(?<=[a-z])(?=[A-Z])`
+c_CamelBoundaryPattern
@E Assets/_dev/Scripts/Util/SceneButtonGenerator.cs 64
-` `
+c_Space
@E Assets/_dev/Scripts/Util/SceneButtonGenerator.cs 65
-@`\s+`
+c_WhitespacePattern
@E Assets/_dev/Scripts/Util/SceneButtonGenerator.cs 65
-` `
+c_Space
@E Assets/_dev/Scripts/Util/SceneButtonGenerator.cs 84
-`Btn_`
+c_ButtonPrefix
@E Assets/_dev/Scripts/Util/SceneButtonGenerator.cs 132
-`Generate Scene Button`
+c_UndoGenerateButton
@E Assets/_dev/Scripts/Util/SceneButtonGenerator.cs 143
-`EventSystem`
+c_EventSystemName
@E Assets/_dev/Scripts/Util/SceneButtonGenerator.cs 146
-`Create EventSystem`
+c_UndoCreateEventSystem
@I Assets/_dev2/GenerateVoice/VoiceAPI.cs 24
+        private const string c_ErrorTextEmpty = `Text is empty.`;
+        private const string c_ErrorTokenEmpty = `API Token is empty.`;
+        private const string c_HeaderAuthorization = `Authorization`;
+        private const string c_BearerPrefix = `Bearer `;
+        private const string c_HeaderContentType = `Content-Type`;
+        private const string c_HeaderAccept = `Accept`;
+        private const string c_MimeJson = `application/json`;
+        private const string c_ErrorParse = `API response could not be parsed.`;
+        private const string c_ErrorNoAudioUrl = `API response does not contain audio_url.\n`;
+
@E Assets/_dev2/GenerateVoice/VoiceAPI.cs 34
-`Text is empty.`
+c_ErrorTextEmpty
@E Assets/_dev2/GenerateVoice/VoiceAPI.cs 40
-`API Token is empty.`
+c_ErrorTokenEmpty
@E Assets/_dev2/GenerateVoice/VoiceAPI.cs 106
-`Authorization`
+c_HeaderAuthorization
@E Assets/_dev2/GenerateVoice/VoiceAPI.cs 107
-`Bearer `
+c_BearerPrefix
@E Assets/_dev2/GenerateVoice/VoiceAPI.cs 111
-`Content-Type`
+c_HeaderContentType
@E Assets/_dev2/GenerateVoice/VoiceAPI.cs 112
-`application/json`
+c_MimeJson
@E Assets/_dev2/GenerateVoice/VoiceAPI.cs 116
-`Accept`
+c_HeaderAccept
@E Assets/_dev2/GenerateVoice/VoiceAPI.cs 117
-`application/json`
+c_MimeJson
@E Assets/_dev2/GenerateVoice/VoiceAPI.cs 188
-`API response could not be parsed.`
+c_ErrorParse
@E Assets/_dev2/GenerateVoice/VoiceAPI.cs 201
-`API response does not contain audio_url.\n`
+c_ErrorNoAudioUrl
@I Assets/_dev2/GenerateVoice/VoiceAsset.cs 83
+        private const string c_WindowsSeparator = `\\`;
+        private const string c_UnitySeparator = `/`;
+        private const string c_UndoGenerate = `Generate Voice Audio`;
+        private const string c_InvalidCharReplacement = `_`;
+
@E Assets/_dev2/GenerateVoice/VoiceAsset.cs 344
-`\\`
+c_WindowsSeparator
@E Assets/_dev2/GenerateVoice/VoiceAsset.cs 345
-`/`
+c_UnitySeparator
@E Assets/_dev2/GenerateVoice/VoiceAsset.cs 380
-`Generate Voice Audio`
+c_UndoGenerate
@E Assets/_dev2/GenerateVoice/VoiceAsset.cs 437
-`_`
+c_InvalidCharReplacement
@C Assets/_dev2/GenerateVoice/VoiceAsset.cs VoiceAssetEditor
+private const string c_NameVoiceProperty = `m_NameVoice`;
+private const string c_TextProperty = `m_Text`;
+private const string c_VoiceIdProperty = `m_VoiceId`;
+private const string c_AudioClipProperty = `m_AudioClip`;
+private const string c_ApiTokenProperty = `m_ApiToken`;
+private const string c_IsGeneratingProperty = `m_IsGenerating`;
+private const string c_VoiceHeader = `Voice`;
+private const string c_NameVoiceLabel = `Name Voice`;
+private const string c_TextLabel = `Text`;
+private const string c_VoiceIdLabel = `Voice ID`;
+private const string c_AudioClipLabel = `Audio Clip`;
+private const string c_ApiHeader = `API`;
+private const string c_ApiTokenLabel = `API Token`;
+private const string c_GeneratingButton = `Generating...`;
+private const string c_GenerateButton = `Generate Audio`;
+private const string c_HelpNameEmpty = `Name Voice is empty.`;
+private const string c_HelpTextEmpty = `Text is empty.`;
+private const string c_HelpTokenEmpty = `API Token is empty.`;
+private const string c_HelpGenerating = `Generating audio...`;
+private const string c_OutputHeader = `Output`;
+private const int c_SpaceSmall = 3;
+private const int c_SpaceLarge = 10;
+private const int c_ButtonHeight = 40;
+private const int c_PathHeight = 18;
@E Assets/_dev2/GenerateVoice/VoiceAsset.cs 473
-`m_NameVoice`
+c_NameVoiceProperty
@E Assets/_dev2/GenerateVoice/VoiceAsset.cs 478
-`m_Text`
+c_TextProperty
@E Assets/_dev2/GenerateVoice/VoiceAsset.cs 483
-`m_VoiceId`
+c_VoiceIdProperty
@E Assets/_dev2/GenerateVoice/VoiceAsset.cs 488
-`m_AudioClip`
+c_AudioClipProperty
@E Assets/_dev2/GenerateVoice/VoiceAsset.cs 493
-`m_ApiToken`
+c_ApiTokenProperty
@E Assets/_dev2/GenerateVoice/VoiceAsset.cs 498
-`m_IsGenerating`
+c_IsGeneratingProperty
@E Assets/_dev2/GenerateVoice/VoiceAsset.cs 514
-`Voice`
+c_VoiceHeader
@E Assets/_dev2/GenerateVoice/VoiceAsset.cs 518
-Space(3)
+Space(c_SpaceSmall)
@E Assets/_dev2/GenerateVoice/VoiceAsset.cs 526
-`Name Voice`
+c_NameVoiceLabel
@E Assets/_dev2/GenerateVoice/VoiceAsset.cs 535
-`Text`
+c_TextLabel
@E Assets/_dev2/GenerateVoice/VoiceAsset.cs 544
-`Voice ID`
+c_VoiceIdLabel
@E Assets/_dev2/GenerateVoice/VoiceAsset.cs 553
-`Audio Clip`
+c_AudioClipLabel
@E Assets/_dev2/GenerateVoice/VoiceAsset.cs 556
-Space(10)
+Space(c_SpaceLarge)
@E Assets/_dev2/GenerateVoice/VoiceAsset.cs 563
-`API`
+c_ApiHeader
@E Assets/_dev2/GenerateVoice/VoiceAsset.cs 567
-Space(3)
+Space(c_SpaceSmall)
@E Assets/_dev2/GenerateVoice/VoiceAsset.cs 571
-`API Token`
+c_ApiTokenLabel
@E Assets/_dev2/GenerateVoice/VoiceAsset.cs 574
-Space(10)
+Space(c_SpaceLarge)
@E Assets/_dev2/GenerateVoice/VoiceAsset.cs 610
-`Generating...`
+c_GeneratingButton
@E Assets/_dev2/GenerateVoice/VoiceAsset.cs 611
-`Generate Audio`
+c_GenerateButton
@E Assets/_dev2/GenerateVoice/VoiceAsset.cs 612
-Height(40)
+Height(c_ButtonHeight)
@E Assets/_dev2/GenerateVoice/VoiceAsset.cs 628
-`Name Voice is empty.`
+c_HelpNameEmpty
@E Assets/_dev2/GenerateVoice/VoiceAsset.cs 636
-`Text is empty.`
+c_HelpTextEmpty
@E Assets/_dev2/GenerateVoice/VoiceAsset.cs 644
-`API Token is empty.`
+c_HelpTokenEmpty
@E Assets/_dev2/GenerateVoice/VoiceAsset.cs 652
-`Generating audio...`
+c_HelpGenerating
@E Assets/_dev2/GenerateVoice/VoiceAsset.cs 661
-Space(10)
+Space(c_SpaceLarge)
@E Assets/_dev2/GenerateVoice/VoiceAsset.cs 664
-`Output`
+c_OutputHeader
@E Assets/_dev2/GenerateVoice/VoiceAsset.cs 671
-Height(18)
+Height(c_PathHeight)
@C Assets/_dev2/Quest/Scripts/AnimationStateBehaviourEditor.cs AnimationStateBehaviourEditor
+private const string c_ClipConfigProperty = `m_ClipConfig`;
+private const string c_StateHashProperty = `m_StateHash`;
+private const string c_ClipLengthProperty = `m_ClipLength`;
+private const string c_RefreshButton = `Refresh`;
+private const string c_StateNotDetected = `Animator State could not be detected.`;
+private const string c_StateLabel = `State`;
+private const string c_StatePathLabel = `State Path`;
+private const string c_StateHashLabel = `State Hash`;
+private const string c_ClipLengthLabel = `Clip Length`;
@E Assets/_dev2/Quest/Scripts/AnimationStateBehaviourEditor.cs 32
-`m_ClipConfig`
+c_ClipConfigProperty
@E Assets/_dev2/Quest/Scripts/AnimationStateBehaviourEditor.cs 35
-`m_StateHash`
+c_StateHashProperty
@E Assets/_dev2/Quest/Scripts/AnimationStateBehaviourEditor.cs 38
-`m_ClipLength`
+c_ClipLengthProperty
@E Assets/_dev2/Quest/Scripts/AnimationStateBehaviourEditor.cs 57
-`Refresh`
+c_RefreshButton
@E Assets/_dev2/Quest/Scripts/AnimationStateBehaviourEditor.cs 70
-`Animator State could not be detected.`
+c_StateNotDetected
@E Assets/_dev2/Quest/Scripts/AnimationStateBehaviourEditor.cs 78
-`State`
+c_StateLabel
@E Assets/_dev2/Quest/Scripts/AnimationStateBehaviourEditor.cs 83
-`State Path`
+c_StatePathLabel
@E Assets/_dev2/Quest/Scripts/AnimationStateBehaviourEditor.cs 88
-`State Hash`
+c_StateHashLabel
@E Assets/_dev2/Quest/Scripts/AnimationStateBehaviourEditor.cs 93
-`Clip Length`
+c_ClipLengthLabel
