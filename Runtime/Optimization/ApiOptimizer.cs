using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace SupabaseBridge.Runtime.Optimization
{
    /// <summary>
    /// API 통신을 최적화하기 위한 유틸리티 클래스
    /// </summary>
    public static class ApiOptimizer
    {
        // 캐시 설정
        private const int DEFAULT_CACHE_TIMEOUT = 60; // 기본 캐시 유효 시간(초)
        private const int MAX_CACHE_ITEMS = 100; // 최대 캐시 항목 수
        
        // 캐시 저장소
        private static readonly Dictionary<string, CacheItem> responseCache = new Dictionary<string, CacheItem>();
        
        // 배치 작업 큐
        private static readonly Dictionary<string, List<BatchItem>> batchQueues = new Dictionary<string, List<BatchItem>>();
        private static readonly Dictionary<string, float> batchTimers = new Dictionary<string, float>();
        
        // 요청 제한 설정
        private static readonly Dictionary<string, RateLimitInfo> rateLimits = new Dictionary<string, RateLimitInfo>();
        
        /// <summary>
        /// 응답 캐싱을 사용하여 API 호출을 최적화합니다.
        /// </summary>
        /// <typeparam name="T">응답 데이터 타입</typeparam>
        /// <param name="cacheKey">캐시 키</param>
        /// <param name="apiCall">API 호출 함수</param>
        /// <param name="cacheTimeout">캐시 유효 시간(초)</param>
        /// <returns>캐시된 응답 또는 새로 호출된 응답</returns>
        public static async Task<T> CachedApiCall<T>(string cacheKey, Func<Task<T>> apiCall, int cacheTimeout = DEFAULT_CACHE_TIMEOUT)
        {
            // 캐시 키 정규화
            cacheKey = NormalizeCacheKey(cacheKey);
            
            // 캐시에서 응답 확인
            if (responseCache.TryGetValue(cacheKey, out CacheItem cacheItem))
            {
                // 캐시가 유효한지 확인
                if ((DateTime.UtcNow - cacheItem.Timestamp).TotalSeconds < cacheTimeout)
                {
                    SupabaseLogger.LogDebug($"Cache hit: {cacheKey}", "ApiOptimizer");
                    return (T)cacheItem.Data;
                }
                else
                {
                    // 캐시 만료
                    SupabaseLogger.LogDebug($"Cache expired: {cacheKey}", "ApiOptimizer");
                    responseCache.Remove(cacheKey);
                }
            }
            
            // API 호출
            SupabaseLogger.LogDebug($"Cache miss: {cacheKey}", "ApiOptimizer");
            T response = await apiCall();
            
            // 캐시 저장
            StoreInCache(cacheKey, response);
            
            return response;
        }
        
        /// <summary>
        /// 캐시를 무효화합니다.
        /// </summary>
        /// <param name="cacheKey">캐시 키 (null이면 모든 캐시 무효화)</param>
        public static void InvalidateCache(string cacheKey = null)
        {
            if (string.IsNullOrEmpty(cacheKey))
            {
                // 모든 캐시 무효화
                SupabaseLogger.LogDebug("Invalidating all cache", "ApiOptimizer");
                responseCache.Clear();
            }
            else
            {
                // 특정 캐시 무효화
                cacheKey = NormalizeCacheKey(cacheKey);
                if (responseCache.ContainsKey(cacheKey))
                {
                    SupabaseLogger.LogDebug($"Invalidating cache: {cacheKey}", "ApiOptimizer");
                    responseCache.Remove(cacheKey);
                }
            }
        }
        
        /// <summary>
        /// 캐시 키를 정규화합니다.
        /// </summary>
        /// <param name="key">원본 키</param>
        /// <returns>정규화된 키</returns>
        private static string NormalizeCacheKey(string key)
        {
            return key.ToLowerInvariant().Trim();
        }
        
        /// <summary>
        /// 응답을 캐시에 저장합니다.
        /// </summary>
        /// <param name="key">캐시 키</param>
        /// <param name="data">응답 데이터</param>
        private static void StoreInCache(string key, object data)
        {
            // 캐시 크기 제한 확인
            if (responseCache.Count >= MAX_CACHE_ITEMS)
            {
                // 가장 오래된 항목 제거
                string oldestKey = null;
                DateTime oldestTime = DateTime.MaxValue;
                
                foreach (var entry in responseCache)
                {
                    if (entry.Value.Timestamp < oldestTime)
                    {
                        oldestTime = entry.Value.Timestamp;
                        oldestKey = entry.Key;
                    }
                }
                
                if (oldestKey != null)
                {
                    responseCache.Remove(oldestKey);
                }
            }
            
            // 캐시에 저장
            responseCache[key] = new CacheItem
            {
                Data = data,
                Timestamp = DateTime.UtcNow
            };
        }
        
        /// <summary>
        /// 배치 처리를 사용하여 API 호출을 최적화합니다.
        /// </summary>
        /// <typeparam name="TRequest">요청 데이터 타입</typeparam>
        /// <typeparam name="TResponse">응답 데이터 타입</typeparam>
        /// <param name="batchKey">배치 작업 키</param>
        /// <param name="request">요청 데이터</param>
        /// <param name="batchApiCall">배치 API 호출 함수</param>
        /// <param name="batchInterval">배치 간격(초)</param>
        /// <param name="maxBatchSize">최대 배치 크기</param>
        /// <returns>API 호출 결과</returns>
        public static Task<TResponse> BatchApiCall<TRequest, TResponse>(
            string batchKey,
            TRequest request,
            Func<List<TRequest>, Task<List<TResponse>>> batchApiCall,
            float batchInterval = 0.5f,
            int maxBatchSize = 10)
        {
            // 작업 완료 소스
            var tcs = new TaskCompletionSource<TResponse>();
            
            // 배치 큐 초기화
            if (!batchQueues.ContainsKey(batchKey))
            {
                batchQueues[batchKey] = new List<BatchItem>();
                batchTimers[batchKey] = Time.realtimeSinceStartup;
            }
            
            // 배치 항목 생성
            var batchItem = new BatchItem
            {
                Request = request,
                CompletionSource = tcs
            };
            
            // 배치 큐에 추가
            batchQueues[batchKey].Add(batchItem);
            
            // 배치 실행 조건 확인
            bool executeBatch = false;
            
            // 배치 크기 확인
            if (batchQueues[batchKey].Count >= maxBatchSize)
            {
                SupabaseLogger.LogDebug($"Batch size limit reached for {batchKey}", "ApiOptimizer");
                executeBatch = true;
            }
            
            // 배치 간격 확인
            if (Time.realtimeSinceStartup - batchTimers[batchKey] >= batchInterval)
            {
                SupabaseLogger.LogDebug($"Batch interval elapsed for {batchKey}", "ApiOptimizer");
                executeBatch = true;
            }
            
            // 배치 실행
            if (executeBatch)
            {
                ExecuteBatch(batchKey, batchApiCall);
            }
            else
            {
                // 배치 간격 후 실행 예약
                float remainingTime = batchInterval - (Time.realtimeSinceStartup - batchTimers[batchKey]);
                
                // 코루틴 대신 Timer 사용
                var timer = new System.Threading.Timer(_ =>
                {
                    // 메인 스레드에서 실행
                    UnityMainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        if (batchQueues.ContainsKey(batchKey) && batchQueues[batchKey].Count > 0)
                        {
                            ExecuteBatch(batchKey, batchApiCall);
                        }
                    });
                }, null, (int)(remainingTime * 1000), System.Threading.Timeout.Infinite);
            }
            
            return tcs.Task;
        }
        
        /// <summary>
        /// 배치 작업을 실행합니다.
        /// </summary>
        /// <typeparam name="TRequest">요청 데이터 타입</typeparam>
        /// <typeparam name="TResponse">응답 데이터 타입</typeparam>
        /// <param name="batchKey">배치 작업 키</param>
        /// <param name="batchApiCall">배치 API 호출 함수</param>
        private static async void ExecuteBatch<TRequest, TResponse>(
            string batchKey,
            Func<List<TRequest>, Task<List<TResponse>>> batchApiCall)
        {
            if (!batchQueues.ContainsKey(batchKey) || batchQueues[batchKey].Count == 0)
            {
                return;
            }
            
            // 현재 배치 항목 가져오기
            var batchItems = batchQueues[batchKey];
            
            // 배치 큐 초기화
            batchQueues[batchKey] = new List<BatchItem>();
            batchTimers[batchKey] = Time.realtimeSinceStartup;
            
            // 요청 데이터 목록 생성
            var requests = new List<TRequest>();
            foreach (var item in batchItems)
            {
                requests.Add((TRequest)item.Request);
            }
            
            try
            {
                // 배치 API 호출
                SupabaseLogger.LogDebug($"Executing batch of {requests.Count} requests for {batchKey}", "ApiOptimizer");
                var responses = await batchApiCall(requests);
                
                // 응답 처리
                if (responses.Count == batchItems.Count)
                {
                    // 각 요청에 대한 응답 할당
                    for (int i = 0; i < batchItems.Count; i++)
                    {
                        var tcs = (TaskCompletionSource<TResponse>)batchItems[i].CompletionSource;
                        tcs.SetResult(responses[i]);
                    }
                }
                else
                {
                    // 응답 수가 요청 수와 일치하지 않는 경우 오류
                    throw new SupabaseException(
                        $"Batch response count ({responses.Count}) does not match request count ({batchItems.Count})",
                        0,
                        "BATCH_MISMATCH");
                }
            }
            catch (Exception ex)
            {
                // 오류 처리
                SupabaseLogger.Error($"Batch execution failed: {ex.Message}", "ApiOptimizer");
                
                // 모든 작업에 오류 전파
                foreach (var item in batchItems)
                {
                    var tcs = item.CompletionSource as TaskCompletionSource<TResponse>;
                    tcs?.SetException(ex);
                }
            }
        }
        
        /// <summary>
        /// 요청 제한을 적용하여 API 호출을 최적화합니다.
        /// </summary>
        /// <typeparam name="T">응답 데이터 타입</typeparam>
        /// <param name="rateLimitKey">요청 제한 키</param>
        /// <param name="apiCall">API 호출 함수</param>
        /// <param name="maxRequestsPerMinute">분당 최대 요청 수</param>
        /// <returns>API 호출 결과</returns>
        public static async Task<T> RateLimitedApiCall<T>(
            string rateLimitKey,
            Func<Task<T>> apiCall,
            int maxRequestsPerMinute = 60)
        {
            // 요청 제한 정보 초기화
            if (!rateLimits.ContainsKey(rateLimitKey))
            {
                rateLimits[rateLimitKey] = new RateLimitInfo
                {
                    RequestCount = 0,
                    ResetTime = DateTime.UtcNow.AddMinutes(1)
                };
            }
            
            // 요청 제한 정보 가져오기
            var rateLimit = rateLimits[rateLimitKey];
            
            // 제한 시간 초기화 확인
            if (DateTime.UtcNow >= rateLimit.ResetTime)
            {
                rateLimit.RequestCount = 0;
                rateLimit.ResetTime = DateTime.UtcNow.AddMinutes(1);
            }
            
            // 요청 제한 확인
            if (rateLimit.RequestCount >= maxRequestsPerMinute)
            {
                // 제한 초과 시 대기
                double waitTime = (rateLimit.ResetTime - DateTime.UtcNow).TotalMilliseconds;
                SupabaseLogger.Warning($"Rate limit reached for {rateLimitKey}. Waiting for {waitTime}ms", "ApiOptimizer");
                
                await Task.Delay((int)waitTime + 100); // 100ms 추가 대기
                
                // 제한 초기화
                rateLimit.RequestCount = 0;
                rateLimit.ResetTime = DateTime.UtcNow.AddMinutes(1);
            }
            
            // 요청 카운트 증가
            rateLimit.RequestCount++;
            
            // API 호출
            return await apiCall();
        }
        
        /// <summary>
        /// 재시도 로직을 적용하여 API 호출을 최적화합니다.
        /// </summary>
        /// <typeparam name="T">응답 데이터 타입</typeparam>
        /// <param name="apiCall">API 호출 함수</param>
        /// <param name="maxRetries">최대 재시도 횟수</param>
        /// <param name="initialDelayMs">초기 지연 시간(밀리초)</param>
        /// <param name="shouldRetry">재시도 조건 함수</param>
        /// <returns>API 호출 결과</returns>
        public static async Task<T> RetryApiCall<T>(
            Func<Task<T>> apiCall,
            int maxRetries = 3,
            int initialDelayMs = 500,
            Func<Exception, bool> shouldRetry = null)
        {
            int retryCount = 0;
            int delay = initialDelayMs;
            
            while (true)
            {
                try
                {
                    // API 호출
                    return await apiCall();
                }
                catch (Exception ex)
                {
                    // 재시도 횟수 초과 확인
                    if (retryCount >= maxRetries)
                    {
                        SupabaseLogger.Error($"Max retries reached ({maxRetries}). Giving up.", "ApiOptimizer");
                        throw;
                    }
                    
                    // 재시도 조건 확인
                    if (shouldRetry != null && !shouldRetry(ex))
                    {
                        SupabaseLogger.Warning($"Not retrying due to exception: {ex.Message}", "ApiOptimizer");
                        throw;
                    }
                    
                    // 네트워크 오류인 경우에만 재시도
                    if (ex is SupabaseException supabaseEx && supabaseEx.Category != ErrorCategory.Network)
                    {
                        SupabaseLogger.Warning($"Not retrying non-network error: {ex.Message}", "ApiOptimizer");
                        throw;
                    }
                    
                    // 재시도 대기
                    retryCount++;
                    SupabaseLogger.Warning($"Retry {retryCount}/{maxRetries} after {delay}ms due to: {ex.Message}", "ApiOptimizer");
                    
                    await Task.Delay(delay);
                    
                    // 지수 백오프
                    delay *= 2;
                }
            }
        }
        
        /// <summary>
        /// 캐시 항목 클래스
        /// </summary>
        private class CacheItem
        {
            public object Data { get; set; }
            public DateTime Timestamp { get; set; }
        }
        
        /// <summary>
        /// 배치 항목 클래스
        /// </summary>
        private class BatchItem
        {
            public object Request { get; set; }
            public object CompletionSource { get; set; }
        }
        
        /// <summary>
        /// 요청 제한 정보 클래스
        /// </summary>
        private class RateLimitInfo
        {
            public int RequestCount { get; set; }
            public DateTime ResetTime { get; set; }
        }
    }
    
    /// <summary>
    /// 메인 스레드 디스패처 클래스
    /// </summary>
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        private static UnityMainThreadDispatcher _instance;
        private readonly Queue<Action> _executionQueue = new Queue<Action>();
        
        public static UnityMainThreadDispatcher Instance
        {
            get
            {
                if (_instance == null)
                {
                    // 새 게임 오브젝트 생성
                    var go = new GameObject("UnityMainThreadDispatcher");
                    _instance = go.AddComponent<UnityMainThreadDispatcher>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        private void Update()
        {
            lock (_executionQueue)
            {
                while (_executionQueue.Count > 0)
                {
                    _executionQueue.Dequeue().Invoke();
                }
            }
        }
        
        /// <summary>
        /// 메인 스레드에서 실행할 작업을 큐에 추가합니다.
        /// </summary>
        /// <param name="action">실행할 작업</param>
        public void Enqueue(Action action)
        {
            lock (_executionQueue)
            {
                _executionQueue.Enqueue(action);
            }
        }
    }
} 