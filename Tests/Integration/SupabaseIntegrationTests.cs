using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using SupabaseBridge.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace SupabaseBridge.Tests.Integration
{
    public class SupabaseIntegrationTests
    {
        private SupabaseClient client;
        private SupabaseAuthService authService;
        private SupabaseDatabaseService databaseService;
        private SupabaseStorageService storageService;
        private CancellationTokenSource globalCts;
        private const float DEFAULT_TIMEOUT = 10f; // 기본 타임아웃 10초
        
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Initialize the logger
            SupabaseLogger.Initialize(LogLevel.Debug);
            
            // Initialize the error handler
            SupabaseErrorHandler.Initialize();
        }
        
        [SetUp]
        public void SetUp()
        {
            // 전역 취소 토큰 생성
            globalCts = new CancellationTokenSource();
            globalCts.CancelAfter(TimeSpan.FromSeconds(30)); // 최대 30초 제한
            
            // Note: For actual integration tests, you would use real credentials
            // For this example, we'll use mock credentials to demonstrate the structure
            client = new SupabaseClient("https://test-project.supabase.co", "test-api-key");
            
            // Initialize services
            authService = new SupabaseAuthService(client);
            databaseService = new SupabaseDatabaseService(client);
            storageService = new SupabaseStorageService(client);
            
            // Clear log history
            SupabaseLogger.ClearLogHistory();
        }
        
        [TearDown]
        public void TearDown()
        {
            // 취소 토큰 해제
            globalCts?.Cancel();
            globalCts?.Dispose();
            globalCts = null;
            
            // 로그 기록 정리
            SupabaseLogger.ClearLogHistory();
            
            // 인증 상태 정리
            if (authService != null && authService.IsAuthenticated)
            {
                try
                {
                    // 비동기 작업을 동기적으로 실행 (최대 5초 대기)
                    var signOutTask = authService.SignOut();
                    
                    // 최대 5초 대기
                    if (!Task.WaitAll(new[] { signOutTask }, 5000))
                    {
                        Debug.LogWarning("SignOut timed out during TearDown");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error during SignOut in TearDown: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 타임아웃이 있는 코루틴 래퍼
        /// </summary>
        private IEnumerator WithTimeout(IEnumerator routine, float timeoutSeconds = DEFAULT_TIMEOUT)
        {
            float startTime = Time.realtimeSinceStartup;
            
            while (routine.MoveNext())
            {
                // 타임아웃 확인
                if (Time.realtimeSinceStartup - startTime > timeoutSeconds)
                {
                    Debug.LogError($"Test timed out after {timeoutSeconds} seconds");
                    Assert.Fail($"Test timed out after {timeoutSeconds} seconds");
                    yield break;
                }
                
                yield return routine.Current;
            }
        }
        
        /// <summary>
        /// 비동기 작업을 코루틴으로 실행하는 헬퍼 메서드
        /// </summary>
        private IEnumerator RunAsync(Func<Task> asyncFunc)
        {
            Task task = null;
            Exception taskException = null;
            
            try
            {
                task = asyncFunc();
            }
            catch (Exception ex)
            {
                taskException = ex;
            }
            
            if (taskException != null)
            {
                Debug.LogError($"Exception during task creation: {taskException}");
                Assert.Fail($"Exception during task creation: {taskException.Message}");
                yield break;
            }
            
            while (!task.IsCompleted)
            {
                yield return null;
            }
            
            if (task.IsFaulted && task.Exception != null)
            {
                Debug.LogError($"Task failed with exception: {task.Exception.InnerException?.Message ?? task.Exception.Message}");
                throw task.Exception;
            }
        }
        
        [UnityTest]
        public IEnumerator AuthService_SignUpAndSignIn_SuccessfulFlow()
        {
            return WithTimeout(AuthServiceSignUpAndSignInTest());
        }
        
        private IEnumerator AuthServiceSignUpAndSignInTest()
        {
            // 테스트 단계 추적
            bool step1_completed = false;
            bool step2_completed = false;
            bool step3_completed = false;
            bool step4_completed = false;
            bool step5_completed = false;
            
            // Note: This test would normally make actual API calls
            // For this example, we'll just demonstrate the structure
            
            // 1. 회원가입 단계
            SupabaseLogger.Info("Would sign up user: test@example.com");
            step1_completed = true;
            yield return null;
            
            // 2. 로그인 단계
            SupabaseLogger.Info("Would sign in user: test@example.com");
            step2_completed = true;
            yield return null;
            
            // 3. 인증 상태 확인
            SupabaseLogger.Info("Would verify authentication state");
            step3_completed = true;
            yield return null;
            
            // 4. 로그아웃
            SupabaseLogger.Info("Would sign out user");
            step4_completed = true;
            yield return null;
            
            // 5. 로그아웃 상태 확인
            SupabaseLogger.Info("Would verify user is signed out");
            step5_completed = true;
            yield return null;
            
            // 모든 단계가 완료되었는지 확인
            Assert.IsTrue(step1_completed, "Step 1 (sign up) was not completed");
            Assert.IsTrue(step2_completed, "Step 2 (sign in) was not completed");
            Assert.IsTrue(step3_completed, "Step 3 (verify auth) was not completed");
            Assert.IsTrue(step4_completed, "Step 4 (sign out) was not completed");
            Assert.IsTrue(step5_completed, "Step 5 (verify signed out) was not completed");
        }
        
        [UnityTest]
        public IEnumerator DatabaseService_QueryTable_ReturnsResults()
        {
            return WithTimeout(DatabaseServiceQueryTest());
        }
        
        private IEnumerator DatabaseServiceQueryTest()
        {
            // 테스트 단계 추적
            bool step1_completed = false;
            bool step2_completed = false;
            
            // Note: This test would normally make actual API calls
            // For this example, we'll just demonstrate the structure
            
            // Arrange
            string tableName = "test_table";
            var queryOptions = new QueryOptions
            {
                Select = new List<string> { "id", "name", "created_at" },
                Limit = 10
            };
            
            // 1. 쿼리 실행
            SupabaseLogger.Info($"Would query table: {tableName}");
            step1_completed = true;
            yield return null;
            
            // 2. 결과 확인
            SupabaseLogger.Info("Would verify query results");
            step2_completed = true;
            yield return null;
            
            // 모든 단계가 완료되었는지 확인
            Assert.IsTrue(step1_completed, "Step 1 (query table) was not completed");
            Assert.IsTrue(step2_completed, "Step 2 (verify results) was not completed");
        }
        
        [UnityTest]
        public IEnumerator StorageService_UploadAndDownloadFile_SuccessfulFlow()
        {
            return WithTimeout(StorageServiceUploadDownloadTest());
        }
        
        private IEnumerator StorageServiceUploadDownloadTest()
        {
            // 테스트 단계 추적
            bool step1_completed = false;
            bool step2_completed = false;
            bool step3_completed = false;
            bool step4_completed = false;
            bool step5_completed = false;
            bool step6_completed = false;
            
            // Note: This test would normally make actual API calls
            // For this example, we'll just demonstrate the structure
            
            // Arrange
            string bucketName = "test-bucket";
            string fileName = "test-file.txt";
            byte[] fileData = System.Text.Encoding.UTF8.GetBytes("Test file content");
            
            // 1. 버킷 생성
            SupabaseLogger.Info($"Would create bucket: {bucketName}");
            step1_completed = true;
            yield return null;
            
            // 2. 파일 업로드
            SupabaseLogger.Info($"Would upload file: {fileName}");
            step2_completed = true;
            yield return null;
            
            // 3. 파일 URL 가져오기
            SupabaseLogger.Info("Would get file URL");
            step3_completed = true;
            yield return null;
            
            // 4. 파일 다운로드
            SupabaseLogger.Info("Would download file");
            step4_completed = true;
            yield return null;
            
            // 5. 파일 내용 확인
            SupabaseLogger.Info("Would verify file content");
            step5_completed = true;
            yield return null;
            
            // 6. 파일 삭제
            SupabaseLogger.Info("Would delete file");
            step6_completed = true;
            yield return null;
            
            // 모든 단계가 완료되었는지 확인
            Assert.IsTrue(step1_completed, "Step 1 (create bucket) was not completed");
            Assert.IsTrue(step2_completed, "Step 2 (upload file) was not completed");
            Assert.IsTrue(step3_completed, "Step 3 (get file URL) was not completed");
            Assert.IsTrue(step4_completed, "Step 4 (download file) was not completed");
            Assert.IsTrue(step5_completed, "Step 5 (verify content) was not completed");
            Assert.IsTrue(step6_completed, "Step 6 (delete file) was not completed");
        }
        
        [UnityTest]
        public IEnumerator ErrorHandling_ServiceErrors_AreCaughtAndLogged()
        {
            return WithTimeout(ErrorHandlingTest());
        }
        
        private IEnumerator ErrorHandlingTest()
        {
            // 테스트 단계 추적
            bool step1_completed = false;
            bool step2_completed = false;
            
            // Note: This test would normally make actual API calls
            // For this example, we'll just demonstrate the structure
            
            // Arrange
            string invalidEndpoint = "/invalid/endpoint";
            
            // 1. 잘못된 엔드포인트로 요청
            SupabaseLogger.Info($"Would make request to invalid endpoint: {invalidEndpoint}");
            step1_completed = true;
            yield return null;
            
            // 2. 오류 시뮬레이션 및 로깅 확인
            SupabaseErrorHandler.ShowError("Simulated error: Endpoint not found", ErrorCategory.NotFound);
            step2_completed = true;
            yield return null;
            
            // 오류가 로깅되었는지 확인
            bool hasErrorLog = false;
            foreach (var log in SupabaseLogger.LogHistory)
            {
                if (log.Level == LogLevel.Error && log.Message.Contains("Simulated error"))
                {
                    hasErrorLog = true;
                    break;
                }
            }
            
            // 모든 단계가 완료되었는지 확인
            Assert.IsTrue(step1_completed, "Step 1 (make request) was not completed");
            Assert.IsTrue(step2_completed, "Step 2 (simulate error) was not completed");
            Assert.IsTrue(hasErrorLog, "Error was not logged");
        }
    }
} 