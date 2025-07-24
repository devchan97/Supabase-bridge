# Supabase Bridge API 참조 문서

## 목차

1. [SupabaseClient](#supabseclient)
2. [SupabaseAuthService](#supabaseauthservice)
3. [SupabaseDatabaseService](#supabasedatabaseservice)
4. [SupabaseStorageService](#supabasestorageservice)
5. [SupabaseLogger](#supabaselogger)
6. [SupabaseErrorHandler](#supabaseerrorhandler)
7. [예외 처리](#예외-처리)

## SupabaseClient

`SupabaseClient` 클래스는 Supabase API와의 통신을 담당합니다.

### 생성자

```csharp
// 기본 설정 파일을 사용하여 초기화
public SupabaseClient()

// 사용자 지정 URL과 키를 사용하여 초기화
public SupabaseClient(string url, string key)
```

### 속성

없음

### 메서드

```csharp
// 인증 토큰 설정
public void SetAccessToken(string token)

// 기본 URL 가져오기
public string GetBaseUrl()

// GET 요청 보내기
public async Task<string> Get(string endpoint, Dictionary<string, string> queryParams = null)

// POST 요청 보내기
public async Task<string> Post(string endpoint, string jsonBody, Dictionary<string, string> queryParams = null)

// PATCH 요청 보내기
public async Task<string> Patch(string endpoint, string jsonBody, Dictionary<string, string> queryParams = null)

// DELETE 요청 보내기
public async Task<string> Delete(string endpoint, Dictionary<string, string> queryParams = null)

// 파일 업로드
public async Task<string> UploadFile(string endpoint, byte[] fileData, string fileName, string contentType, Dictionary<string, string> queryParams = null)

// 파일 다운로드
public async Task<byte[]> DownloadFile(string url)
```

### 사용 예제

```csharp
// 클라이언트 초기화
var client = new SupabaseClient("https://your-project.supabase.co", "your-api-key");

// GET 요청 보내기
string response = await client.Get("rest/v1/your_table", new Dictionary<string, string> {
    { "select", "*" }
});

// POST 요청 보내기
string jsonBody = "{\"name\": \"John\", \"age\": 30}";
string response = await client.Post("rest/v1/your_table", jsonBody);
```

## SupabaseAuthService

`SupabaseAuthService` 클래스는 사용자 인증 기능을 제공합니다.

### 생성자

```csharp
public SupabaseAuthService(SupabaseClient client)
```

### 속성

```csharp
// 현재 인증된 사용자
public User CurrentUser { get; }

// 사용자가 인증되었는지 여부
public bool IsAuthenticated { get; }

// 인증 상태 변경 이벤트
public event Action<User> OnAuthStateChanged;
```

### 메서드

```csharp
// 회원가입
public async Task<User> SignUp(string email, string password, Dictionary<string, object> userData = null)

// 로그인
public async Task<User> SignIn(string email, string password)

// 로그아웃
public async Task SignOut()

// 현재 사용자 정보 가져오기
public async Task<User> GetCurrentUser()

// 토큰 갱신
public async Task<User> RefreshToken()
```

### 사용 예제

```csharp
// 인증 서비스 초기화
var client = new SupabaseClient("https://your-project.supabase.co", "your-api-key");
var authService = new SupabaseAuthService(client);

// 회원가입
User newUser = await authService.SignUp("user@example.com", "securePassword");

// 로그인
User user = await authService.SignIn("user@example.com", "securePassword");

// 현재 사용자 확인
if (authService.IsAuthenticated)
{
    Debug.Log($"Logged in as: {authService.CurrentUser.Email}");
}

// 로그아웃
await authService.SignOut();
```

## SupabaseDatabaseService

`SupabaseDatabaseService` 클래스는 데이터베이스 작업을 위한 기능을 제공합니다.

### 생성자

```csharp
public SupabaseDatabaseService(SupabaseClient client)
```

### 속성

없음

### 메서드

```csharp
// 테이블 쿼리
public async Task<List<Dictionary<string, object>>> QueryTable(string tableName, QueryOptions options = null)

// 데이터 삽입
public async Task<Dictionary<string, object>> Insert(string tableName, Dictionary<string, object> data)

// 데이터 업데이트
public async Task<List<Dictionary<string, object>>> Update(string tableName, Dictionary<string, object> data, string filter)

// 데이터 삭제
public async Task<List<Dictionary<string, object>>> Delete(string tableName, string filter)
```

### 사용 예제

```csharp
// 데이터베이스 서비스 초기화
var client = new SupabaseClient("https://your-project.supabase.co", "your-api-key");
var dbService = new SupabaseDatabaseService(client);

// 테이블 쿼리
var options = new QueryOptions
{
    Select = new List<string> { "id", "name", "created_at" },
    Limit = 10,
    OrderBy = "created_at",
    Ascending = false
};
var results = await dbService.QueryTable("users", options);

// 데이터 삽입
var newUser = new Dictionary<string, object>
{
    { "name", "John Doe" },
    { "email", "john@example.com" },
    { "age", 30 }
};
var insertedUser = await dbService.Insert("users", newUser);

// 데이터 업데이트
var updateData = new Dictionary<string, object>
{
    { "name", "Jane Doe" }
};
var updatedUsers = await dbService.Update("users", updateData, "id=eq.1");

// 데이터 삭제
var deletedUsers = await dbService.Delete("users", "id=eq.1");
```

## SupabaseStorageService

`SupabaseStorageService` 클래스는 파일 스토리지 작업을 위한 기능을 제공합니다.

### 생성자

```csharp
public SupabaseStorageService(SupabaseClient client)
```

### 속성

없음

### 메서드

```csharp
// 버킷 존재 여부 확인
public async Task<bool> BucketExists(string bucketName)

// 버킷 생성
public async Task<BucketInfo> CreateBucket(string bucketName, bool isPublic = false)

// 버킷 목록 가져오기
public async Task<List<BucketInfo>> ListBuckets()

// 파일 업로드
public async Task<FileObject> UploadFile(string bucketName, byte[] fileData, string fileName, string storageFilePath, string contentType = null)

// 파일 목록 가져오기
public async Task<List<FileObject>> ListFiles(string bucketName, string path = "", int? limit = null, int? offset = null)

// 파일 URL 가져오기
public async Task<string> GetFileUrl(string bucketName, string filePath, int? expiresIn = null)

// 파일 다운로드
public async Task<byte[]> DownloadFile(string bucketName, string filePath)

// 파일 삭제
public async Task DeleteFile(string bucketName, string filePath)
```

### 사용 예제

```csharp
// 스토리지 서비스 초기화
var client = new SupabaseClient("https://your-project.supabase.co", "your-api-key");
var storageService = new SupabaseStorageService(client);

// 버킷 생성
if (!await storageService.BucketExists("images"))
{
    await storageService.CreateBucket("images", true);
}

// 파일 업로드
byte[] imageData = File.ReadAllBytes("path/to/image.jpg");
var uploadedFile = await storageService.UploadFile("images", imageData, "image.jpg", "uploads/image.jpg", "image/jpeg");

// 파일 URL 가져오기
string imageUrl = await storageService.GetFileUrl("images", "uploads/image.jpg");

// 파일 다운로드
byte[] downloadedData = await storageService.DownloadFile("images", "uploads/image.jpg");

// 파일 삭제
await storageService.DeleteFile("images", "uploads/image.jpg");
```

## SupabaseLogger

`SupabaseLogger` 클래스는 로깅 기능을 제공합니다.

### 속성

```csharp
// 로그 레벨
public static LogLevel LogLevel { get; set; }

// 파일 로깅 활성화 여부
public static bool LogToFile { get; set; }

// 로그 파일 경로
public static string LogFilePath { get; set; }

// 로그 이력
public static IReadOnlyList<LogEntry> LogHistory { get; }
```

### 메서드

```csharp
// 로거 초기화
public static void Initialize(LogLevel logLevel = LogLevel.Info, bool logToFile = false, string logFilePath = "")

// 디버그 로그
public static void Debug(string message, string context = "Supabase")

// 정보 로그
public static void Info(string message, string context = "Supabase")

// 경고 로그
public static void Warning(string message, string context = "Supabase")

// 오류 로그
public static void Error(string message, string context = "Supabase")

// 예외 로그
public static void Exception(Exception exception, string context = "Supabase")

// 로그 이력 지우기
public static void ClearLogHistory()

// 로그 이력을 문자열로 가져오기
public static string GetLogHistoryAsString()
```

### 사용 예제

```csharp
// 로거 초기화
SupabaseLogger.Initialize(LogLevel.Debug, true, "Logs/supabase.log");

// 로그 작성
SupabaseLogger.LogDebug("디버그 메시지");
SupabaseLogger.Info("정보 메시지");
SupabaseLogger.Warning("경고 메시지");
SupabaseLogger.Error("오류 메시지");

try
{
    // 작업 수행
}
catch (Exception ex)
{
    SupabaseLogger.Exception(ex);
}

// 로그 이력 가져오기
string logHistory = SupabaseLogger.GetLogHistoryAsString();
```

## SupabaseErrorHandler

`SupabaseErrorHandler` 클래스는 오류 처리 기능을 제공합니다.

### 메서드

```csharp
// 오류 핸들러 초기화
public static void Initialize()

// 오류 콜백 설정
public static void SetErrorCallback(Action<string, ErrorCategory> callback)

// 예외 처리
public static string HandleException(Exception exception, string context = "Supabase")

// 오류 표시
public static void ShowError(string message, ErrorCategory category = ErrorCategory.Unknown)

// 경고 표시
public static void ShowWarning(string message, string context = "Supabase")
```

### 사용 예제

```csharp
// 오류 핸들러 초기화
SupabaseErrorHandler.Initialize();

// 오류 콜백 설정
SupabaseErrorHandler.SetErrorCallback((message, category) => {
    Debug.LogError($"[{category}] {message}");
    // UI에 오류 메시지 표시
});

try
{
    // 작업 수행
}
catch (Exception ex)
{
    string errorMessage = SupabaseErrorHandler.HandleException(ex);
    // errorMessage를 사용하여 사용자에게 오류 표시
}

// 직접 오류 표시
SupabaseErrorHandler.ShowError("설정을 로드할 수 없습니다.", ErrorCategory.Configuration);
```

## 예외 처리

`SupabaseException` 클래스는 Supabase 관련 예외를 나타냅니다.

### 속성

```csharp
// HTTP 상태 코드
public int StatusCode { get; }

// 오류 코드
public string ErrorCode { get; }

// 오류 카테고리
public ErrorCategory Category { get; }
```

### 메서드

```csharp
// 사용자 친화적인 오류 메시지 가져오기
public string GetUserFriendlyMessage()
```

### 오류 카테고리

```csharp
public enum ErrorCategory
{
    Authentication,  // 인증 관련 오류
    Database,        // 데이터베이스 관련 오류
    Storage,         // 스토리지 관련 오류
    Configuration,   // 설정 관련 오류
    Network,         // 네트워크 관련 오류
    Parsing,         // 파싱 관련 오류
    NotFound,        // 리소스를 찾을 수 없음
    ClientError,     // 클라이언트 오류
    ServerError,     // 서버 오류
    Unknown          // 알 수 없는 오류
}
```

### 사용 예제

```csharp
try
{
    // Supabase API 호출
}
catch (SupabaseException ex)
{
    switch (ex.Category)
    {
        case ErrorCategory.Authentication:
            // 인증 오류 처리
            break;
        case ErrorCategory.Database:
            // 데이터베이스 오류 처리
            break;
        case ErrorCategory.Storage:
            // 스토리지 오류 처리
            break;
        default:
            // 기타 오류 처리
            break;
    }
    
    // 사용자 친화적인 메시지 표시
    string message = ex.GetUserFriendlyMessage();
    Debug.LogError(message);
}
``` 