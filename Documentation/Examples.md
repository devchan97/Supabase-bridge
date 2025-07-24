# Supabase Bridge 사용 예제

이 문서에서는 Supabase Bridge를 사용하여 Unity 프로젝트에서 Supabase 서비스를 활용하는 방법에 대한 예제를 제공합니다.

## 목차

1. [초기 설정](#초기-설정)
2. [인증 예제](#인증-예제)
3. [데이터베이스 예제](#데이터베이스-예제)
4. [스토리지 예제](#스토리지-예제)
5. [오류 처리 예제](#오류-처리-예제)
6. [로깅 예제](#로깅-예제)
7. [고급 예제](#고급-예제)

## 초기 설정

### 1. Supabase Bridge 초기화

```csharp
using UnityEngine;
using SupabaseBridge.Runtime;

public class SupabaseInitializer : MonoBehaviour
{
    [SerializeField] private string supabaseUrl;
    [SerializeField] private string supabaseKey;
    
    private SupabaseClient client;
    private SupabaseAuthService authService;
    private SupabaseDatabaseService dbService;
    private SupabaseStorageService storageService;
    
    private void Awake()
    {
        // 로깅 시스템 초기화
        SupabaseLogger.Initialize(LogLevel.Debug);
        
        // 오류 처리 시스템 초기화
        SupabaseErrorHandler.Initialize();
        SupabaseErrorHandler.SetErrorCallback(HandleError);
        
        // Supabase 클라이언트 초기화
        client = new SupabaseClient(supabaseUrl, supabaseKey);
        
        // 서비스 초기화
        authService = new SupabaseAuthService(client);
        dbService = new SupabaseDatabaseService(client);
        storageService = new SupabaseStorageService(client);
        
        Debug.Log("Supabase Bridge initialized successfully!");
    }
    
    private void HandleError(string message, ErrorCategory category)
    {
        Debug.LogError($"[{category}] {message}");
        // 여기에 UI 오류 메시지 표시 로직 추가
    }
}
```

### 2. 설정 파일 사용

```csharp
using UnityEngine;
using SupabaseBridge.Runtime;

public class SupabaseConfigExample : MonoBehaviour
{
    private SupabaseClient client;
    
    private void Awake()
    {
        // 설정 파일에서 자동으로 URL과 키를 로드
        client = new SupabaseClient();
        
        // 현재 URL 확인
        Debug.Log($"Using Supabase URL: {client.GetBaseUrl()}");
    }
}
```

## 인증 예제

### 1. 회원가입 및 로그인

```csharp
using System.Collections.Generic;
using UnityEngine;
using SupabaseBridge.Runtime;

public class AuthExample : MonoBehaviour
{
    private SupabaseClient client;
    private SupabaseAuthService authService;
    
    private void Start()
    {
        client = new SupabaseClient("YOUR_SUPABASE_URL", "YOUR_SUPABASE_KEY");
        authService = new SupabaseAuthService(client);
    }
    
    public async void SignUp(string email, string password)
    {
        try
        {
            // 추가 사용자 데이터 설정 (선택 사항)
            var userData = new Dictionary<string, object>
            {
                { "display_name", "New Player" },
                { "avatar_url", "" }
            };
            
            // 회원가입
            User user = await authService.SignUp(email, password, userData);
            
            Debug.Log($"User registered: {user.Email} with ID: {user.Id}");
        }
        catch (SupabaseException ex)
        {
            Debug.LogError($"Sign up failed: {ex.GetUserFriendlyMessage()}");
        }
    }
    
    public async void SignIn(string email, string password)
    {
        try
        {
            // 로그인
            User user = await authService.SignIn(email, password);
            
            Debug.Log($"User signed in: {user.Email}");
        }
        catch (SupabaseException ex)
        {
            Debug.LogError($"Sign in failed: {ex.GetUserFriendlyMessage()}");
        }
    }
    
    public async void SignOut()
    {
        try
        {
            // 로그아웃
            await authService.SignOut();
            
            Debug.Log("User signed out");
        }
        catch (SupabaseException ex)
        {
            Debug.LogError($"Sign out failed: {ex.GetUserFriendlyMessage()}");
        }
    }
    
    public void CheckAuthStatus()
    {
        if (authService.IsAuthenticated)
        {
            Debug.Log($"User is authenticated: {authService.CurrentUser.Email}");
        }
        else
        {
            Debug.Log("No user is authenticated");
        }
    }
}
```

### 2. 토큰 갱신

```csharp
using UnityEngine;
using SupabaseBridge.Runtime;

public class TokenRefreshExample : MonoBehaviour
{
    private SupabaseClient client;
    private SupabaseAuthService authService;
    
    private void Start()
    {
        client = new SupabaseClient("YOUR_SUPABASE_URL", "YOUR_SUPABASE_KEY");
        authService = new SupabaseAuthService(client);
    }
    
    public async void RefreshToken()
    {
        try
        {
            if (authService.IsAuthenticated)
            {
                User user = await authService.RefreshToken();
                Debug.Log("Token refreshed successfully");
            }
            else
            {
                Debug.LogWarning("No authenticated user to refresh token");
            }
        }
        catch (SupabaseException ex)
        {
            Debug.LogError($"Token refresh failed: {ex.GetUserFriendlyMessage()}");
        }
    }
}
```

## 데이터베이스 예제

### 1. 데이터 쿼리

```csharp
using System.Collections.Generic;
using UnityEngine;
using SupabaseBridge.Runtime;

public class DatabaseQueryExample : MonoBehaviour
{
    private SupabaseClient client;
    private SupabaseDatabaseService dbService;
    
    private void Start()
    {
        client = new SupabaseClient("YOUR_SUPABASE_URL", "YOUR_SUPABASE_KEY");
        dbService = new SupabaseDatabaseService(client);
    }
    
    public async void QueryHighScores()
    {
        try
        {
            // 쿼리 옵션 설정
            var options = new QueryOptions
            {
                Select = new List<string> { "id", "player_name", "score", "level" },
                OrderBy = "score",
                Ascending = false,
                Limit = 10
            };
            
            // 테이블 쿼리
            var highScores = await dbService.QueryTable("high_scores", options);
            
            Debug.Log($"Found {highScores.Count} high scores");
            
            // 결과 처리
            foreach (var score in highScores)
            {
                Debug.Log($"Player: {score["player_name"]}, Score: {score["score"]}, Level: {score["level"]}");
            }
        }
        catch (SupabaseException ex)
        {
            Debug.LogError($"Query failed: {ex.GetUserFriendlyMessage()}");
        }
    }
}
```

### 2. 데이터 삽입 및 업데이트

```csharp
using System.Collections.Generic;
using UnityEngine;
using SupabaseBridge.Runtime;

public class DatabaseCrudExample : MonoBehaviour
{
    private SupabaseClient client;
    private SupabaseDatabaseService dbService;
    
    private void Start()
    {
        client = new SupabaseClient("YOUR_SUPABASE_URL", "YOUR_SUPABASE_KEY");
        dbService = new SupabaseDatabaseService(client);
    }
    
    public async void SaveHighScore(string playerName, int score, int level)
    {
        try
        {
            // 삽입할 데이터 준비
            var scoreData = new Dictionary<string, object>
            {
                { "player_name", playerName },
                { "score", score },
                { "level", level },
                { "created_at", System.DateTime.UtcNow.ToString("o") }
            };
            
            // 데이터 삽입
            var result = await dbService.Insert("high_scores", scoreData);
            
            Debug.Log($"High score saved with ID: {result["id"]}");
        }
        catch (SupabaseException ex)
        {
            Debug.LogError($"Save failed: {ex.GetUserFriendlyMessage()}");
        }
    }
    
    public async void UpdatePlayerProfile(string playerId, string newName, int newLevel)
    {
        try
        {
            // 업데이트할 데이터 준비
            var profileData = new Dictionary<string, object>
            {
                { "display_name", newName },
                { "level", newLevel },
                { "updated_at", System.DateTime.UtcNow.ToString("o") }
            };
            
            // 데이터 업데이트
            var results = await dbService.Update("player_profiles", profileData, $"id=eq.{playerId}");
            
            if (results.Count > 0)
            {
                Debug.Log("Player profile updated successfully");
            }
            else
            {
                Debug.LogWarning("No player profile was updated");
            }
        }
        catch (SupabaseException ex)
        {
            Debug.LogError($"Update failed: {ex.GetUserFriendlyMessage()}");
        }
    }
    
    public async void DeleteOldScores(int daysOld)
    {
        try
        {
            // 특정 날짜보다 오래된 기록 삭제
            string cutoffDate = System.DateTime.UtcNow.AddDays(-daysOld).ToString("o");
            string filter = $"created_at=lt.{cutoffDate}";
            
            // 데이터 삭제
            var deletedScores = await dbService.Delete("high_scores", filter);
            
            Debug.Log($"Deleted {deletedScores.Count} old high scores");
        }
        catch (SupabaseException ex)
        {
            Debug.LogError($"Delete failed: {ex.GetUserFriendlyMessage()}");
        }
    }
}
```

## 스토리지 예제

### 1. 파일 업로드 및 다운로드

```csharp
using UnityEngine;
using SupabaseBridge.Runtime;

public class StorageExample : MonoBehaviour
{
    private SupabaseClient client;
    private SupabaseStorageService storageService;
    
    private void Start()
    {
        client = new SupabaseClient("YOUR_SUPABASE_URL", "YOUR_SUPABASE_KEY");
        storageService = new SupabaseStorageService(client);
    }
    
    public async void UploadScreenshot()
    {
        try
        {
            // 스크린샷 캡처
            Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
            byte[] pngData = screenshot.EncodeToPNG();
            Destroy(screenshot);
            
            // 버킷이 존재하는지 확인하고 없으면 생성
            string bucketName = "screenshots";
            if (!await storageService.BucketExists(bucketName))
            {
                await storageService.CreateBucket(bucketName, true);
                Debug.Log($"Created new bucket: {bucketName}");
            }
            
            // 파일 이름 생성
            string fileName = $"screenshot_{System.DateTime.UtcNow.ToString("yyyyMMdd_HHmmss")}.png";
            string filePath = $"user_{authService.CurrentUser?.Id ?? "anonymous"}/{fileName}";
            
            // 파일 업로드
            var fileObject = await storageService.UploadFile(bucketName, pngData, fileName, filePath, "image/png");
            
            Debug.Log($"Screenshot uploaded: {fileObject.Name}");
            
            // 파일 URL 가져오기
            string fileUrl = await storageService.GetFileUrl(bucketName, filePath);
            Debug.Log($"Screenshot URL: {fileUrl}");
        }
        catch (SupabaseException ex)
        {
            Debug.LogError($"Upload failed: {ex.GetUserFriendlyMessage()}");
        }
    }
    
    public async void DownloadUserAvatar(string userId)
    {
        try
        {
            string bucketName = "avatars";
            string filePath = $"user_{userId}/avatar.png";
            
            // 파일 다운로드
            byte[] imageData = await storageService.DownloadFile(bucketName, filePath);
            
            // 텍스처로 변환
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(imageData);
            
            Debug.Log($"Avatar downloaded: {texture.width}x{texture.height}");
            
            // 여기서 텍스처를 UI 이미지 등에 적용
        }
        catch (SupabaseException ex)
        {
            Debug.LogError($"Download failed: {ex.GetUserFriendlyMessage()}");
        }
    }
    
    public async void ListUserFiles(string userId)
    {
        try
        {
            string bucketName = "user_files";
            string path = $"user_{userId}/";
            
            // 파일 목록 가져오기
            var files = await storageService.ListFiles(bucketName, path);
            
            Debug.Log($"Found {files.Count} files for user {userId}");
            
            foreach (var file in files)
            {
                Debug.Log($"File: {file.Name}, Size: {file.Size} bytes, Type: {file.ContentType}");
            }
        }
        catch (SupabaseException ex)
        {
            Debug.LogError($"List files failed: {ex.GetUserFriendlyMessage()}");
        }
    }
}
```

## 오류 처리 예제

```csharp
using UnityEngine;
using UnityEngine.UI;
using SupabaseBridge.Runtime;

public class ErrorHandlingExample : MonoBehaviour
{
    [SerializeField] private Text errorMessageText;
    [SerializeField] private GameObject errorPanel;
    
    private void Start()
    {
        // 오류 처리 시스템 초기화
        SupabaseErrorHandler.Initialize();
        
        // 오류 콜백 설정
        SupabaseErrorHandler.SetErrorCallback(DisplayErrorToUser);
    }
    
    private void DisplayErrorToUser(string message, ErrorCategory category)
    {
        // 카테고리별 처리
        string prefix = "";
        switch (category)
        {
            case ErrorCategory.Authentication:
                prefix = "인증 오류: ";
                break;
            case ErrorCategory.Database:
                prefix = "데이터베이스 오류: ";
                break;
            case ErrorCategory.Storage:
                prefix = "스토리지 오류: ";
                break;
            case ErrorCategory.Network:
                prefix = "네트워크 오류: ";
                break;
            default:
                prefix = "오류: ";
                break;
        }
        
        // UI에 오류 메시지 표시
        errorMessageText.text = prefix + message;
        errorPanel.SetActive(true);
        
        // 로그에도 기록
        Debug.LogError($"[{category}] {message}");
    }
    
    public void CloseErrorPanel()
    {
        errorPanel.SetActive(false);
    }
    
    // 오류 테스트 메서드
    public void TestError()
    {
        try
        {
            throw new SupabaseException("테스트 오류 메시지", 401, "AUTH_ERROR");
        }
        catch (SupabaseException ex)
        {
            SupabaseErrorHandler.HandleException(ex);
        }
    }
}
```

## 로깅 예제

```csharp
using UnityEngine;
using UnityEngine.UI;
using SupabaseBridge.Runtime;

public class LoggingExample : MonoBehaviour
{
    [SerializeField] private Text logText;
    [SerializeField] private Dropdown logLevelDropdown;
    
    private void Start()
    {
        // 로깅 시스템 초기화
        SupabaseLogger.Initialize(LogLevel.Debug, true, "Logs/supabase.log");
        
        // 로그 레벨 드롭다운 설정
        logLevelDropdown.onValueChanged.AddListener(OnLogLevelChanged);
    }
    
    private void OnLogLevelChanged(int index)
    {
        LogLevel level = (LogLevel)index;
        SupabaseLogger.LogLevel = level;
        Debug.Log($"Log level changed to: {level}");
    }
    
    public void WriteDebugLog()
    {
        SupabaseLogger.LogDebug("디버그 메시지 예제", "LoggingExample");
        UpdateLogDisplay();
    }
    
    public void WriteInfoLog()
    {
        SupabaseLogger.Info("정보 메시지 예제", "LoggingExample");
        UpdateLogDisplay();
    }
    
    public void WriteWarningLog()
    {
        SupabaseLogger.Warning("경고 메시지 예제", "LoggingExample");
        UpdateLogDisplay();
    }
    
    public void WriteErrorLog()
    {
        SupabaseLogger.Error("오류 메시지 예제", "LoggingExample");
        UpdateLogDisplay();
    }
    
    public void ClearLogs()
    {
        SupabaseLogger.ClearLogHistory();
        UpdateLogDisplay();
    }
    
    private void UpdateLogDisplay()
    {
        // 로그 이력을 UI에 표시
        logText.text = SupabaseLogger.GetLogHistoryAsString();
    }
}
```

## 고급 예제

### 1. 인증 상태 관리

```csharp
using UnityEngine;
using SupabaseBridge.Runtime;

public class AuthStateManager : MonoBehaviour
{
    private SupabaseClient client;
    private SupabaseAuthService authService;
    
    // 싱글톤 인스턴스
    public static AuthStateManager Instance { get; private set; }
    
    private void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // 초기화
        client = new SupabaseClient();
        authService = new SupabaseAuthService(client);
        
        // 인증 상태 변경 이벤트 구독
        authService.OnAuthStateChanged += HandleAuthStateChanged;
    }
    
    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (authService != null)
        {
            authService.OnAuthStateChanged -= HandleAuthStateChanged;
        }
    }
    
    private void HandleAuthStateChanged(User user)
    {
        if (user != null)
        {
            Debug.Log($"User authenticated: {user.Email}");
            // 로그인 UI 숨기기, 게임 UI 표시 등의 작업
        }
        else
        {
            Debug.Log("User signed out");
            // 로그인 UI 표시, 게임 UI 숨기기 등의 작업
        }
    }
    
    private void Start()
    {
        // 앱 시작 시 인증 상태 확인
        CheckAuthState();
    }
    
    private async void CheckAuthState()
    {
        try
        {
            // 현재 사용자 정보 가져오기
            User user = await authService.GetCurrentUser();
            
            if (user != null)
            {
                Debug.Log($"User already authenticated: {user.Email}");
                // 이미 인증된 사용자가 있으면 게임 화면으로 이동
            }
            else
            {
                Debug.Log("No authenticated user");
                // 인증된 사용자가 없으면 로그인 화면으로 이동
            }
        }
        catch (SupabaseException ex)
        {
            Debug.LogError($"Auth state check failed: {ex.GetUserFriendlyMessage()}");
            // 오류 발생 시 로그인 화면으로 이동
        }
    }
    
    // 다른 스크립트에서 접근할 수 있는 공개 메서드
    public async void SignIn(string email, string password)
    {
        try
        {
            await authService.SignIn(email, password);
        }
        catch (SupabaseException ex)
        {
            SupabaseErrorHandler.HandleException(ex);
        }
    }
    
    public async void SignOut()
    {
        try
        {
            await authService.SignOut();
        }
        catch (SupabaseException ex)
        {
            SupabaseErrorHandler.HandleException(ex);
        }
    }
    
    public User GetCurrentUser()
    {
        return authService.CurrentUser;
    }
    
    public bool IsAuthenticated()
    {
        return authService.IsAuthenticated;
    }
}
```

### 2. 데이터 동기화 관리자

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using SupabaseBridge.Runtime;

public class DataSyncManager : MonoBehaviour
{
    private SupabaseClient client;
    private SupabaseDatabaseService dbService;
    
    // 동기화 간격 (초)
    [SerializeField] private float syncInterval = 60f;
    
    // 마지막 동기화 시간
    private DateTime lastSyncTime;
    
    // 로컬 데이터 캐시
    private Dictionary<string, object> playerDataCache;
    
    private void Start()
    {
        client = new SupabaseClient();
        dbService = new SupabaseDatabaseService(client);
        
        // 초기 데이터 로드
        LoadPlayerData();
        
        // 주기적 동기화 시작
        InvokeRepeating("SyncPlayerData", syncInterval, syncInterval);
    }
    
    private async void LoadPlayerData()
    {
        try
        {
            // 인증된 사용자가 있는지 확인
            var authService = new SupabaseAuthService(client);
            if (!authService.IsAuthenticated)
            {
                Debug.LogWarning("Cannot load player data: No authenticated user");
                return;
            }
            
            string userId = authService.CurrentUser.Id;
            
            // 플레이어 데이터 쿼리
            var options = new QueryOptions
            {
                Select = new List<string> { "*" }
            };
            
            var results = await dbService.QueryTable("player_data", options);
            
            if (results.Count > 0)
            {
                playerDataCache = results[0];
                Debug.Log("Player data loaded from server");
                lastSyncTime = DateTime.UtcNow;
                
                // 데이터 로드 이벤트 발생
                OnPlayerDataLoaded?.Invoke(playerDataCache);
            }
            else
            {
                Debug.Log("No player data found, creating new data");
                await CreateNewPlayerData(userId);
            }
        }
        catch (SupabaseException ex)
        {
            Debug.LogError($"Failed to load player data: {ex.GetUserFriendlyMessage()}");
        }
    }
    
    private async Task CreateNewPlayerData(string userId)
    {
        try
        {
            // 기본 플레이어 데이터 생성
            playerDataCache = new Dictionary<string, object>
            {
                { "user_id", userId },
                { "level", 1 },
                { "experience", 0 },
                { "coins", 100 },
                { "items", "[]" },
                { "last_login", DateTime.UtcNow.ToString("o") }
            };
            
            // 서버에 데이터 저장
            await dbService.Insert("player_data", playerDataCache);
            
            Debug.Log("New player data created");
            lastSyncTime = DateTime.UtcNow;
            
            // 데이터 로드 이벤트 발생
            OnPlayerDataLoaded?.Invoke(playerDataCache);
        }
        catch (SupabaseException ex)
        {
            Debug.LogError($"Failed to create player data: {ex.GetUserFriendlyMessage()}");
        }
    }
    
    private async void SyncPlayerData()
    {
        try
        {
            if (playerDataCache == null)
            {
                Debug.LogWarning("Cannot sync: Player data not loaded");
                return;
            }
            
            // 마지막 로그인 시간 업데이트
            playerDataCache["last_login"] = DateTime.UtcNow.ToString("o");
            
            // 서버에 데이터 업데이트
            var authService = new SupabaseAuthService(client);
            string userId = authService.CurrentUser.Id;
            
            await dbService.Update("player_data", playerDataCache, $"user_id=eq.{userId}");
            
            Debug.Log("Player data synced with server");
            lastSyncTime = DateTime.UtcNow;
            
            // 동기화 이벤트 발생
            OnPlayerDataSynced?.Invoke();
        }
        catch (SupabaseException ex)
        {
            Debug.LogError($"Failed to sync player data: {ex.GetUserFriendlyMessage()}");
        }
    }
    
    // 플레이어 데이터 업데이트
    public void UpdatePlayerData(string key, object value)
    {
        if (playerDataCache != null)
        {
            playerDataCache[key] = value;
            Debug.Log($"Updated local player data: {key} = {value}");
        }
    }
    
    // 플레이어 데이터 가져오기
    public T GetPlayerData<T>(string key, T defaultValue = default)
    {
        if (playerDataCache != null && playerDataCache.ContainsKey(key))
        {
            try
            {
                return (T)Convert.ChangeType(playerDataCache[key], typeof(T));
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to convert player data: {ex.Message}");
            }
        }
        
        return defaultValue;
    }
    
    // 수동 동기화 요청
    public void RequestSync()
    {
        SyncPlayerData();
    }
    
    // 이벤트
    public delegate void PlayerDataEvent(Dictionary<string, object> data);
    public event PlayerDataEvent OnPlayerDataLoaded;
    
    public delegate void SyncEvent();
    public event SyncEvent OnPlayerDataSynced;
}
```

이러한 예제들을 통해 Supabase Bridge를 Unity 프로젝트에서 효과적으로 활용하는 방법을 배울 수 있습니다. 더 자세한 정보는 API 참조 문서를 확인하세요. 