using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SupabaseBridge.Editor.Optimization
{
    /// <summary>
    /// 에디터 UI의 성능을 최적화하기 위한 유틸리티 클래스
    /// </summary>
    public static class EditorUIOptimizer
    {
        // 가상 스크롤링을 위한 상태 저장
        private static Dictionary<string, Vector2> scrollPositions = new Dictionary<string, Vector2>();
        private static Dictionary<string, int> visibleItemCounts = new Dictionary<string, int>();
        private static Dictionary<string, float> itemHeights = new Dictionary<string, float>();
        
        /// <summary>
        /// 가상 스크롤 뷰를 구현하여 대량의 항목을 효율적으로 표시합니다.
        /// </summary>
        /// <param name="id">스크롤 뷰의 고유 ID</param>
        /// <param name="totalItems">전체 항목 수</param>
        /// <param name="drawItem">항목을 그리는 콜백 함수 (인덱스를 매개변수로 받음)</param>
        /// <param name="itemHeight">항목의 높이 (픽셀)</param>
        /// <param name="visibleHeight">스크롤 뷰의 가시 영역 높이 (픽셀)</param>
        public static void VirtualScrollView(string id, int totalItems, Action<int> drawItem, float itemHeight = 20f, float visibleHeight = 300f)
        {
            // 스크롤 위치 초기화
            if (!scrollPositions.ContainsKey(id))
            {
                scrollPositions[id] = Vector2.zero;
            }
            
            // 항목 높이 저장
            itemHeights[id] = itemHeight;
            
            // 가시 항목 수 계산
            int visibleCount = Mathf.CeilToInt(visibleHeight / itemHeight) + 1; // 경계에 걸친 항목을 위해 +1
            visibleItemCounts[id] = visibleCount;
            
            // 컨텐츠 전체 높이 계산
            float contentHeight = totalItems * itemHeight;
            
            // 스크롤 뷰 시작
            scrollPositions[id] = EditorGUILayout.BeginScrollView(scrollPositions[id], GUILayout.Height(visibleHeight));
            
            // 스크롤 위치에 따라 시작 인덱스 계산
            int startIndex = Mathf.FloorToInt(scrollPositions[id].y / itemHeight);
            startIndex = Mathf.Max(0, startIndex);
            
            // 끝 인덱스 계산
            int endIndex = Mathf.Min(startIndex + visibleCount, totalItems);
            
            // 스크롤 위치에 따라 상단 공간 추가
            if (startIndex > 0)
            {
                GUILayout.Space(startIndex * itemHeight);
            }
            
            // 가시 항목만 그리기
            for (int i = startIndex; i < endIndex; i++)
            {
                drawItem(i);
            }
            
            // 하단 공간 추가
            if (endIndex < totalItems)
            {
                GUILayout.Space((totalItems - endIndex) * itemHeight);
            }
            
            // 스크롤 뷰 종료
            EditorGUILayout.EndScrollView();
        }
        
        /// <summary>
        /// 데이터 로드를 지연시켜 UI 응답성을 향상시킵니다.
        /// </summary>
        /// <param name="id">로드 작업의 고유 ID</param>
        /// <param name="loadData">데이터를 로드하는 비동기 함수</param>
        /// <param name="onDataLoaded">데이터 로드 완료 시 호출되는 콜백</param>
        /// <param name="loadingMessage">로딩 중 표시할 메시지</param>
        public static void LazyLoad<T>(string id, Func<T> loadData, Action<T> onDataLoaded, string loadingMessage = "Loading...")
        {
            // 이미 진행 중인 작업이 있는지 확인
            if (EditorApplication.update.GetInvocationList().Length > 0)
            {
                EditorGUILayout.HelpBox(loadingMessage, MessageType.Info);
                return;
            }
            
            // 비동기 작업 등록
            EditorApplication.update += () =>
            {
                try
                {
                    // 데이터 로드
                    T data = loadData();
                    
                    // UI 스레드에서 콜백 실행
                    EditorApplication.delayCall += () =>
                    {
                        onDataLoaded(data);
                        EditorWindow.GetWindow<SupabaseEditorWindow>().Repaint();
                    };
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error during lazy loading: {ex.Message}");
                }
                
                // 작업 완료 후 이벤트 제거
                EditorApplication.update -= EditorApplication.update;
            };
        }
        
        /// <summary>
        /// 테이블 데이터를 효율적으로 표시합니다.
        /// </summary>
        /// <param name="id">테이블의 고유 ID</param>
        /// <param name="headers">테이블 헤더</param>
        /// <param name="rows">테이블 행 데이터</param>
        /// <param name="columnWidths">각 열의 너비 (비율)</param>
        /// <param name="visibleHeight">테이블의 가시 영역 높이 (픽셀)</param>
        public static void OptimizedTable(string id, string[] headers, List<string[]> rows, float[] columnWidths = null, float visibleHeight = 300f)
        {
            if (rows == null || rows.Count == 0)
            {
                EditorGUILayout.HelpBox("No data available.", MessageType.Info);
                return;
            }
            
            // 열 너비 기본값 설정
            if (columnWidths == null || columnWidths.Length != headers.Length)
            {
                columnWidths = new float[headers.Length];
                for (int i = 0; i < headers.Length; i++)
                {
                    columnWidths[i] = 1.0f / headers.Length;
                }
            }
            
            // 헤더 그리기
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            for (int i = 0; i < headers.Length; i++)
            {
                GUILayout.Label(headers[i], EditorStyles.toolbarButton, GUILayout.Width(EditorGUIUtility.currentViewWidth * columnWidths[i]));
            }
            EditorGUILayout.EndHorizontal();
            
            // 가상 스크롤 뷰로 행 그리기
            VirtualScrollView(id, rows.Count, (index) =>
            {
                EditorGUILayout.BeginHorizontal();
                for (int i = 0; i < headers.Length; i++)
                {
                    string cellValue = index < rows.Count && i < rows[index].Length ? rows[index][i] : "";
                    
                    // 셀 값이 너무 길면 잘라내기
                    if (cellValue.Length > 100)
                    {
                        cellValue = cellValue.Substring(0, 97) + "...";
                    }
                    
                    GUILayout.Label(cellValue, EditorStyles.label, GUILayout.Width(EditorGUIUtility.currentViewWidth * columnWidths[i]));
                }
                EditorGUILayout.EndHorizontal();
            }, 20f, visibleHeight);
        }
        
        /// <summary>
        /// 대량의 데이터를 페이징 처리하여 표시합니다.
        /// </summary>
        /// <param name="id">페이징의 고유 ID</param>
        /// <param name="data">전체 데이터</param>
        /// <param name="pageSize">페이지당 항목 수</param>
        /// <param name="drawItems">항목을 그리는 콜백 함수 (페이지 데이터를 매개변수로 받음)</param>
        public static void PaginatedView<T>(string id, List<T> data, int pageSize, Action<List<T>> drawItems)
        {
            if (data == null || data.Count == 0)
            {
                EditorGUILayout.HelpBox("No data available.", MessageType.Info);
                return;
            }
            
            // 현재 페이지 인덱스 초기화
            if (!SessionState.GetBool($"{id}_initialized", false))
            {
                SessionState.SetInt($"{id}_page", 0);
                SessionState.SetBool($"{id}_initialized", true);
            }
            
            int currentPage = SessionState.GetInt($"{id}_page", 0);
            int totalPages = Mathf.CeilToInt((float)data.Count / pageSize);
            
            // 페이지 범위 확인
            currentPage = Mathf.Clamp(currentPage, 0, totalPages - 1);
            
            // 현재 페이지 데이터 계산
            int startIndex = currentPage * pageSize;
            int count = Mathf.Min(pageSize, data.Count - startIndex);
            List<T> pageData = data.GetRange(startIndex, count);
            
            // 페이지 데이터 그리기
            drawItems(pageData);
            
            // 페이징 컨트롤 그리기
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            // 이전 페이지 버튼
            GUI.enabled = currentPage > 0;
            if (GUILayout.Button("◀", GUILayout.Width(30)))
            {
                SessionState.SetInt($"{id}_page", currentPage - 1);
            }
            
            // 페이지 표시
            GUILayout.Label($"Page {currentPage + 1} of {totalPages}", EditorStyles.miniLabel);
            
            // 다음 페이지 버튼
            GUI.enabled = currentPage < totalPages - 1;
            if (GUILayout.Button("▶", GUILayout.Width(30)))
            {
                SessionState.SetInt($"{id}_page", currentPage + 1);
            }
            
            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 검색 및 필터링 기능을 제공합니다.
        /// </summary>
        /// <param name="id">검색의 고유 ID</param>
        /// <param name="data">전체 데이터</param>
        /// <param name="searchPredicate">검색 조건 함수</param>
        /// <returns>필터링된 데이터</returns>
        public static List<T> SearchAndFilter<T>(string id, List<T> data, Func<T, string, bool> searchPredicate)
        {
            if (data == null || data.Count == 0)
            {
                return new List<T>();
            }
            
            // 검색어 초기화
            if (!EditorPrefs.HasKey($"{id}_search"))
            {
                EditorPrefs.SetString($"{id}_search", "");
            }
            
            // 검색 UI
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            string searchTerm = EditorGUILayout.TextField(EditorPrefs.GetString($"{id}_search", ""));
            EditorPrefs.SetString($"{id}_search", searchTerm);
            
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                EditorPrefs.SetString($"{id}_search", "");
                searchTerm = "";
            }
            EditorGUILayout.EndHorizontal();
            
            // 검색어가 비어있으면 모든 데이터 반환
            if (string.IsNullOrEmpty(searchTerm))
            {
                return data;
            }
            
            // 검색 조건에 맞는 데이터 필터링
            List<T> filteredData = new List<T>();
            foreach (var item in data)
            {
                if (searchPredicate(item, searchTerm))
                {
                    filteredData.Add(item);
                }
            }
            
            return filteredData;
        }
        
        /// <summary>
        /// 데이터 캐싱을 관리합니다.
        /// </summary>
        /// <param name="id">캐시의 고유 ID</param>
        /// <param name="loadData">데이터를 로드하는 함수</param>
        /// <param name="cacheTimeoutSeconds">캐시 유효 시간(초)</param>
        /// <returns>캐시된 데이터 또는 새로 로드된 데이터</returns>
        public static T CachedData<T>(string id, Func<T> loadData, int cacheTimeoutSeconds = 300) where T : class
        {
            // 캐시 키
            string cacheKey = $"SupabaseBridge_{id}_cache";
            string timestampKey = $"SupabaseBridge_{id}_timestamp";
            
            // 캐시된 데이터가 있고 유효 시간이 지나지 않았으면 캐시 사용
            if (SessionState.GetString(cacheKey, "") != "" && 
                (EditorApplication.timeSinceStartup - SessionState.GetFloat(timestampKey, 0)) < cacheTimeoutSeconds)
            {
                try
                {
                    string json = SessionState.GetString(cacheKey, "");
                    return JsonUtility.FromJson<T>(json);
                }
                catch (Exception)
                {
                    // 캐시 데이터가 손상된 경우 새로 로드
                }
            }
            
            // 데이터 로드
            T data = loadData();
            
            // 캐시 업데이트
            if (data != null)
            {
                try
                {
                    string json = JsonUtility.ToJson(data);
                    SessionState.SetString(cacheKey, json);
                    SessionState.SetFloat(timestampKey, (float)EditorApplication.timeSinceStartup);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to cache data: {ex.Message}");
                }
            }
            
            return data;
        }
        
        /// <summary>
        /// 캐시를 무효화합니다.
        /// </summary>
        /// <param name="id">캐시의 고유 ID</param>
        public static void InvalidateCache(string id)
        {
            string cacheKey = $"SupabaseBridge_{id}_cache";
            string timestampKey = $"SupabaseBridge_{id}_timestamp";
            
            SessionState.EraseString(cacheKey);
            SessionState.EraseFloat(timestampKey);
        }
    }
} 