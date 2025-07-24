using System;

namespace SupabaseBridge.Runtime
{
    /// <summary>
    /// Represents a Supabase-specific exception.
    /// </summary>
    public class SupabaseException : Exception
    {
        /// <summary>
        /// Gets the HTTP status code.
        /// </summary>
        public int StatusCode { get; }
        
        /// <summary>
        /// Gets the error code.
        /// </summary>
        public string ErrorCode { get; }
        
        /// <summary>
        /// Gets the error category.
        /// </summary>
        public ErrorCategory Category { get; }
        
        /// <summary>
        /// Initializes a new instance of the SupabaseException class.
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="statusCode">The HTTP status code</param>
        /// <param name="errorCode">The error code</param>
        public SupabaseException(string message, int statusCode, string errorCode)
            : base(message)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
            Category = DetermineCategory(statusCode, errorCode);
        }
        
        /// <summary>
        /// Initializes a new instance of the SupabaseException class.
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="statusCode">The HTTP status code</param>
        /// <param name="errorCode">The error code</param>
        /// <param name="innerException">The inner exception</param>
        public SupabaseException(string message, int statusCode, string errorCode, Exception innerException)
            : base(message, innerException)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
            Category = DetermineCategory(statusCode, errorCode);
        }
        
        /// <summary>
        /// Determines the error category based on the status code and error code.
        /// </summary>
        /// <param name="statusCode">The HTTP status code</param>
        /// <param name="errorCode">The error code</param>
        /// <returns>The error category</returns>
        private ErrorCategory DetermineCategory(int statusCode, string errorCode)
        {
            if (statusCode == 0 && errorCode != null)
            {
                // Client-side errors
                if (errorCode.StartsWith("AUTH_"))
                {
                    return ErrorCategory.Authentication;
                }
                else if (errorCode.StartsWith("DB_"))
                {
                    return ErrorCategory.Database;
                }
                else if (errorCode.StartsWith("STORAGE_"))
                {
                    return ErrorCategory.Storage;
                }
                else if (errorCode.StartsWith("CONFIG_"))
                {
                    return ErrorCategory.Configuration;
                }
                else if (errorCode.StartsWith("NETWORK_"))
                {
                    return ErrorCategory.Network;
                }
                else if (errorCode.StartsWith("PARSE_"))
                {
                    return ErrorCategory.Parsing;
                }
            }
            else
            {
                // Server-side errors
                if (statusCode >= 400 && statusCode < 500)
                {
                    if (statusCode == 401 || statusCode == 403)
                    {
                        return ErrorCategory.Authentication;
                    }
                    else if (statusCode == 404)
                    {
                        return ErrorCategory.NotFound;
                    }
                    else
                    {
                        return ErrorCategory.ClientError;
                    }
                }
                else if (statusCode >= 500)
                {
                    return ErrorCategory.ServerError;
                }
            }
            
            return ErrorCategory.Unknown;
        }
        
        /// <summary>
        /// Gets a user-friendly error message.
        /// </summary>
        /// <returns>A user-friendly error message</returns>
        public string GetUserFriendlyMessage()
        {
            switch (Category)
            {
                case ErrorCategory.Authentication:
                    return $"인증 오류: {Message}";
                case ErrorCategory.Database:
                    return $"데이터베이스 오류: {Message}";
                case ErrorCategory.Storage:
                    return $"스토리지 오류: {Message}";
                case ErrorCategory.Configuration:
                    return $"설정 오류: {Message}";
                case ErrorCategory.Network:
                    return $"네트워크 오류: {Message}";
                case ErrorCategory.Parsing:
                    return $"데이터 파싱 오류: {Message}";
                case ErrorCategory.NotFound:
                    return $"리소스를 찾을 수 없음: {Message}";
                case ErrorCategory.ClientError:
                    return $"클라이언트 오류: {Message}";
                case ErrorCategory.ServerError:
                    return $"서버 오류: {Message}";
                default:
                    return $"알 수 없는 오류: {Message}";
            }
        }
    }
    
    /// <summary>
    /// Represents the category of a Supabase error.
    /// </summary>
    public enum ErrorCategory
    {
        /// <summary>
        /// Authentication-related errors.
        /// </summary>
        Authentication,
        
        /// <summary>
        /// Database-related errors.
        /// </summary>
        Database,
        
        /// <summary>
        /// Storage-related errors.
        /// </summary>
        Storage,
        
        /// <summary>
        /// Configuration-related errors.
        /// </summary>
        Configuration,
        
        /// <summary>
        /// Network-related errors.
        /// </summary>
        Network,
        
        /// <summary>
        /// Data parsing errors.
        /// </summary>
        Parsing,
        
        /// <summary>
        /// Resource not found errors.
        /// </summary>
        NotFound,
        
        /// <summary>
        /// Client-side errors.
        /// </summary>
        ClientError,
        
        /// <summary>
        /// Server-side errors.
        /// </summary>
        ServerError,
        
        /// <summary>
        /// Unknown errors.
        /// </summary>
        Unknown
    }
} 