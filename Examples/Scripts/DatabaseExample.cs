using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SupabaseBridge.Runtime;

namespace SupabaseBridge.Examples
{
    public class DatabaseExample : MonoBehaviour
    {
        [Header("Supabase Configuration")]
        [SerializeField] private string supabaseUrl;
        [SerializeField] private string supabaseKey;
        
        [Header("UI References - High Scores")]
        [SerializeField] private InputField playerNameInput;
        [SerializeField] private InputField scoreInput;
        [SerializeField] private InputField levelInput;
        [SerializeField] private Button saveScoreButton;
        [SerializeField] private Button getScoresButton;
        [SerializeField] private Text scoresListText;
        [SerializeField] private Dropdown sortByDropdown;
        [SerializeField] private Toggle ascendingToggle;
        [SerializeField] private InputField limitInput;
        
        [Header("UI References - Status")]
        [SerializeField] private Text statusText;
        [SerializeField] private Text errorText;
        
        private SupabaseClient client;
        private SupabaseDatabaseService dbService;
        private SupabaseAuthService authService;
        
        private void Start()
        {
            // 오류 메시지 초기화
            errorText.text = "";
            
            // 버튼 이벤트 설정
            saveScoreButton.onClick.AddListener(OnSaveScoreClicked);
            getScoresButton.onClick.AddListener(OnGetScoresClicked);
            
            // Supabase 초기화
            InitializeSupabase();
        }
        
        private void InitializeSupabase()
        {
            try
            {
                // 로깅 시스템 초기화
                SupabaseLogger.Initialize(LogLevel.Debug);
                SupabaseLogger.Info("Initializing Supabase Bridge...");
                
                // 오류 처리 시스템 초기화
                SupabaseErrorHandler.Initialize();
                SupabaseErrorHandler.SetErrorCallback(HandleError);
                
                // Supabase 클라이언트 초기화
                client = new SupabaseClient(supabaseUrl, supabaseKey);
                
                // 서비스 초기화
                dbService = new SupabaseDatabaseService(client);
                authService = new SupabaseAuthService(client);
                
                statusText.text = "Supabase Bridge initialized successfully!";
                
                // 초기 데이터 로드
                OnGetScoresClicked();
            }
            catch (SupabaseException ex)
            {
                Debug.LogError($"Failed to initialize Supabase: {ex.Message}");
                statusText.text = "Failed to initialize Supabase. Check console for details.";
            }
        }
        
        private async void OnSaveScoreClicked()
        {
            try
            {
                // 입력값 검증
                string playerName = playerNameInput.text.Trim();
                
                if (string.IsNullOrEmpty(playerName))
                {
                    errorText.text = "Player name is required";
                    return;
                }
                
                if (!int.TryParse(scoreInput.text, out int score))
                {
                    errorText.text = "Score must be a valid number";
                    return;
                }
                
                if (!int.TryParse(levelInput.text, out int level))
                {
                    errorText.text = "Level must be a valid number";
                    return;
                }
                
                // 상태 메시지 업데이트
                statusText.text = "Saving score...";
                errorText.text = "";
                
                // 삽입할 데이터 준비
                var scoreData = new Dictionary<string, object>
                {
                    { "player_name", playerName },
                    { "score", score },
                    { "level", level },
                    { "created_at", System.DateTime.UtcNow.ToString("o") }
                };
                
                // 현재 사용자가 인증되어 있으면 사용자 ID 추가
                if (authService.IsAuthenticated)
                {
                    scoreData["user_id"] = authService.CurrentUser.Id;
                }
                
                // 데이터 삽입
                var result = await dbService.Insert("high_scores", scoreData);
                
                Debug.Log($"High score saved with ID: {result["id"]}");
                statusText.text = "Score saved successfully!";
                
                // 점수 목록 갱신
                OnGetScoresClicked();
                
                // 입력 필드 초기화
                scoreInput.text = "";
                levelInput.text = "";
            }
            catch (SupabaseException ex)
            {
                Debug.LogError($"Save failed: {ex.Message}");
                errorText.text = ex.GetUserFriendlyMessage();
                statusText.text = "Failed to save score";
            }
        }
        
        private async void OnGetScoresClicked()
        {
            try
            {
                // 상태 메시지 업데이트
                statusText.text = "Loading scores...";
                errorText.text = "";
                
                // 정렬 필드 결정
                string orderBy;
                switch (sortByDropdown.value)
                {
                    case 0:
                        orderBy = "score";
                        break;
                    case 1:
                        orderBy = "level";
                        break;
                    case 2:
                        orderBy = "player_name";
                        break;
                    case 3:
                        orderBy = "created_at";
                        break;
                    default:
                        orderBy = "score";
                        break;
                }
                
                // 제한 수 파싱
                if (!int.TryParse(limitInput.text, out int limit) || limit <= 0)
                {
                    limit = 10; // 기본값
                }
                
                // 쿼리 옵션 설정
                var options = new QueryOptions
                {
                    Select = new List<string> { "id", "player_name", "score", "level", "created_at" },
                    OrderBy = orderBy,
                    Ascending = ascendingToggle.isOn,
                    Limit = limit
                };
                
                // 테이블 쿼리
                var highScores = await dbService.QueryTable("high_scores", options);
                
                Debug.Log($"Found {highScores.Count} high scores");
                statusText.text = $"Loaded {highScores.Count} scores";
                
                // 결과를 텍스트로 표시
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine("High Scores:");
                sb.AppendLine("------------------------------");
                
                foreach (var score in highScores)
                {
                    string playerName = score["player_name"].ToString();
                    string scoreValue = score["score"].ToString();
                    string levelValue = score["level"].ToString();
                    string createdAt = score["created_at"].ToString();
                    
                    // 날짜 형식 변환 시도
                    if (System.DateTime.TryParse(createdAt, out System.DateTime dateTime))
                    {
                        createdAt = dateTime.ToString("yyyy-MM-dd HH:mm");
                    }
                    
                    sb.AppendLine($"{playerName}: {scoreValue} pts (Level {levelValue}) - {createdAt}");
                }
                
                scoresListText.text = sb.ToString();
            }
            catch (SupabaseException ex)
            {
                Debug.LogError($"Query failed: {ex.Message}");
                errorText.text = ex.GetUserFriendlyMessage();
                statusText.text = "Failed to load scores";
            }
        }
        
        public async void DeleteScore(string id)
        {
            try
            {
                // 상태 메시지 업데이트
                statusText.text = "Deleting score...";
                errorText.text = "";
                
                // 데이터 삭제
                var deletedScores = await dbService.Delete("high_scores", $"id=eq.{id}");
                
                Debug.Log($"Deleted score with ID: {id}");
                statusText.text = "Score deleted successfully!";
                
                // 점수 목록 갱신
                OnGetScoresClicked();
            }
            catch (SupabaseException ex)
            {
                Debug.LogError($"Delete failed: {ex.Message}");
                errorText.text = ex.GetUserFriendlyMessage();
                statusText.text = "Failed to delete score";
            }
        }
        
        public async void UpdateScore(string id, int newScore)
        {
            try
            {
                // 상태 메시지 업데이트
                statusText.text = "Updating score...";
                errorText.text = "";
                
                // 업데이트할 데이터 준비
                var scoreData = new Dictionary<string, object>
                {
                    { "score", newScore }
                };
                
                // 데이터 업데이트
                var results = await dbService.Update("high_scores", scoreData, $"id=eq.{id}");
                
                if (results.Count > 0)
                {
                    Debug.Log($"Updated score with ID: {id}");
                    statusText.text = "Score updated successfully!";
                    
                    // 점수 목록 갱신
                    OnGetScoresClicked();
                }
                else
                {
                    Debug.LogWarning($"No score found with ID: {id}");
                    statusText.text = "No score found with the specified ID";
                }
            }
            catch (SupabaseException ex)
            {
                Debug.LogError($"Update failed: {ex.Message}");
                errorText.text = ex.GetUserFriendlyMessage();
                statusText.text = "Failed to update score";
            }
        }
        
        private void HandleError(string message, ErrorCategory category)
        {
            errorText.text = message;
            Debug.LogError($"[{category}] {message}");
        }
    }
} 