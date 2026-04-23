using System;

namespace SecureChat.Client.Models
{
    public sealed class RequestOtpDto
    {
        public string Email { get; set; } = string.Empty;
    }

    public sealed class VerifyOtpDto
    {
        public string Email { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
    }

    public sealed class ResetPasswordDto
    {
        public string ResetToken { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public sealed class RequestOtpResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public sealed class VerifyOtpResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ResetToken { get; set; }
        public string? ErrorCode { get; set; }
    }

    public sealed class ResetPasswordResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ErrorCode { get; set; }
    }

    public sealed class ApiErrorResponseDto
    {
        public string? Message { get; set; }
        public string? Error { get; set; }
        public string? ErrorCode { get; set; }
    }

    public sealed class ServiceResult<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ErrorCode { get; set; }

        public static ServiceResult<T> Ok(T data, string message) => new()
        {
            Success = true,
            Data = data,
            Message = message
        };

        public static ServiceResult<T> Fail(string message, string? errorCode = null) => new()
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode
        };
    }
}
