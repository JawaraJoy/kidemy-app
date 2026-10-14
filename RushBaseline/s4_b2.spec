# Batch 2: URL runtime dan fallback session -> [SerializeField] (nilai awal identik)
@E Assets/_dev/Scripts/Manager/APIManager.cs 14
-private string m_ApiAIUrl
+[SerializeField] private string m_ApiAIUrl
@E Assets/_dev/Scripts/Manager/GameManager.cs 140
-private string m_APIResultURL
+[SerializeField] private string m_APIResultURL
@I Assets/_dev/Scripts/Manager/GameManager.cs 141
+        [SerializeField] private string m_FallbackSessionId = `67e05cdd-c077-4d84-8c49-15767bf13ef2`;
@E Assets/_dev/Scripts/Manager/GameManager.cs 236
-`67e05cdd-c077-4d84-8c49-15767bf13ef2`
+m_FallbackSessionId
@E Assets/_dev/Scripts/Manager/GameManager.cs 239
-`67e05cdd-c077-4d84-8c49-15767bf13ef2`
+m_FallbackSessionId
