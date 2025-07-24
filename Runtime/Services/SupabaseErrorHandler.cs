using System;
using System.Collections.Generic;
using UnityEngine;

namespace SupabaseBridge.Runtime
{
    /// <summary>
    /// Handles errors and provides error messages for Supabase operations.
    /// </summary>
    public static class SupabaseErrorHandler
    {
        private static readonly Dictionary<string, string> _errorMessages = new Dictionary<string, string>();
        private static Action<string, ErrorCategory> _onErrorCallback;
        
        /// <summary>
        /// Initializes the error handler.
        /// </summary>
        public static void Initialize()
        {
            // Initialize error messages
            InitializeErrorMessages();
        }
        
        /// <summary>
        /// Sets the error callback.
        /// </summary>
        /// <param name="callback">The callback to invoke when an error occurs</param>
        public static void SetErrorCallback(Action<string, ErrorCategory> callback)
        {
            _onErrorCallback = callback;
        }
        
        /// <summary>
        /// Handles an exception.
        /// </summary>
        /// <param name="exception">The exception to handle</param>
        /// <param name="context">The context in which the exception occurred</param>
        /// <returns>A user-friendly error message</returns>
        public static string HandleException(Exception exception, string context = "Supabase")
        {
            string message;
            ErrorCategory category = ErrorCategory.Unknown;
            
            if (exception is SupabaseException supabaseEx)
            {
                message = GetErrorMessage(supabaseEx);
                category = supabaseEx.Category;
            }
            else
            {
                message = $"오류가 발생했습니다: {exception.Message}";
            }
            
            // Log the exception
            SupabaseLogger.Exception(exception, context);
            
            // Invoke the error callback if set
            _onErrorCallback?.Invoke(message, category);
            
            return message;
        }
        
        /// <summary>
        /// Gets an error message for a Supabase exception.
        /// </summary>
        /// <param name="exception">The Supabase exception</param>
        /// <returns>A user-friendly error message</returns>
        private static string GetErrorMessage(SupabaseException exception)
        {
            // Check if we have a specific message for this error code
            if (!string.IsNullOrEmpty(exception.ErrorCode) && _errorMessages.TryGetValue(exception.ErrorCode, out string specificMessage))
            {
                return specificMessage;
            }
            
            // Return the user-friendly message from the exception
            return exception.GetUserFriendlyMessage();
        }
        
        /// <summary>
        /// Initializes the error messages dictionary.
        /// </summary>
        private static void InitializeErrorMessages()
        {
            // Authentication errors
            _errorMessages["AUTH_INVALID_CREDENTIALS"] = "잘못된 이메일 또는 비밀번호입니다.";
            _errorMessages["AUTH_EMAIL_TAKEN"] = "이미 사용 중인 이메일 주소입니다.";
            _errorMessages["AUTH_WEAK_PASSWORD"] = "비밀번호가 너무 약합니다. 더 강력한 비밀번호를 사용하세요.";
            _errorMessages["AUTH_USER_NOT_FOUND"] = "사용자를 찾을 수 없습니다.";
            _errorMessages["AUTH_TOKEN_EXPIRED"] = "인증 토큰이 만료되었습니다. 다시 로그인하세요.";
            _errorMessages["AUTH_NETWORK_ERROR"] = "네트워크 오류로 인해 인증할 수 없습니다.";
            
            // Database errors
            _errorMessages["DB_QUERY_ERROR"] = "데이터베이스 쿼리 실행 중 오류가 발생했습니다.";
            _errorMessages["DB_CONNECTION_ERROR"] = "데이터베이스 연결 오류가 발생했습니다.";
            _errorMessages["DB_CONSTRAINT_VIOLATION"] = "데이터베이스 제약 조건 위반이 발생했습니다.";
            _errorMessages["DB_PERMISSION_ERROR"] = "데이터베이스 작업에 대한 권한이 없습니다.";
            
            // Storage errors
            _errorMessages["STORAGE_BUCKET_NOT_FOUND"] = "스토리지 버킷을 찾을 수 없습니다.";
            _errorMessages["STORAGE_FILE_NOT_FOUND"] = "파일을 찾을 수 없습니다.";
            _errorMessages["STORAGE_PERMISSION_ERROR"] = "스토리지 작업에 대한 권한이 없습니다.";
            _errorMessages["STORAGE_UPLOAD_ERROR"] = "파일 업로드 중 오류가 발생했습니다.";
            _errorMessages["STORAGE_DOWNLOAD_ERROR"] = "파일 다운로드 중 오류가 발생했습니다.";
            
            // Configuration errors
            _errorMessages["CONFIG_INVALID_URL"] = "잘못된 Supabase URL입니다.";
            _errorMessages["CONFIG_INVALID_KEY"] = "잘못된 Supabase API 키입니다.";
            _errorMessages["CONFIG_NOT_FOUND"] = "Supabase 설정을 찾을 수 없습니다.";
            
            // Network errors
            _errorMessages["NETWORK_ERROR"] = "네트워크 오류가 발생했습니다. 인터넷 연결을 확인하세요.";
            _errorMessages["NETWORK_TIMEOUT"] = "네트워크 요청 시간이 초과되었습니다.";
            
            // Parsing errors
            _errorMessages["PARSE_ERROR"] = "데이터 파싱 중 오류가 발생했습니다.";
            _errorMessages["PARSE_JSON_ERROR"] = "JSON 데이터 파싱 중 오류가 발생했습니다.";
        }
        
        /// <summary>
        /// Shows an error message in the Unity console.
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="category">The error category</param>
        public static void ShowError(string message, ErrorCategory category = ErrorCategory.Unknown)
        {
            // Log the error
            string contextString = category.ToString();
            SupabaseLogger.Error(message, contextString);
            
            // Invoke the error callback if set
            _onErrorCallback?.Invoke(message, category);
        }
        
        /// <summary>
        /// Shows a warning message in the Unity console.
        /// </summary>
        /// <param name="message">The warning message</param>
        /// <param name="context">The context of the warning</param>
        public static void ShowWarning(string message, string context = "Supabase")
        {
            // Log the warning
            SupabaseLogger.Warning(message, context);
        }
    }
} 