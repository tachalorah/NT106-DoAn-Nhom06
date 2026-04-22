using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using SecureChat.Client.Constants;
using SecureChat.Client.Helpers;
using SecureChat.Client.Models;

namespace SecureChat.Client.Services.Api
{
    public interface IAuthService
    {
        Task<ServiceResult<RequestOtpResponseDto>> RequestPasswordOtpAsync(string email, CancellationToken cancellationToken = default);
        Task<ServiceResult<VerifyOtpResponseDto>> VerifyPasswordOtpAsync(string email, string otp, CancellationToken cancellationToken = default);
        Task<ServiceResult<ResetPasswordResponseDto>> ResetPasswordAsync(string resetToken, string newPassword, CancellationToken cancellationToken = default);
    }

    public sealed class AuthService(HttpClient httpClient, Action<string>? logger = null) : IAuthService
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly Action<string> _logger = logger ?? (message => Debug.WriteLine(message));

        public async Task<ServiceResult<RequestOtpResponseDto>> RequestPasswordOtpAsync(string email, CancellationToken cancellationToken = default)
        {
            if (!ValidationHelper.IsValidEmail(email))
            {
                return ServiceResult<RequestOtpResponseDto>.Fail("Invalid email format.", "INVALID_EMAIL");
            }

            var request = new RequestOtpDto { Email = email.Trim() };

            try
            {
                Log("Requesting forgot-password OTP.");

                var response = await _httpClient.PostAsJsonAsync(ApiEndpoints.Auth.RequestPasswordOtp, request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await ReadErrorAsync(response, cancellationToken);
                    Log($"OTP request returned status {(int)response.StatusCode}. ErrorCode={error.ErrorCode}");
                }

                var genericMessage = "If the email is registered, an OTP has been sent.";
                return ServiceResult<RequestOtpResponseDto>.Ok(
                    new RequestOtpResponseDto
                    {
                        Success = true,
                        Message = genericMessage
                    },
                    genericMessage);
            }
            catch (HttpRequestException ex)
            {
                Log($"Network error while requesting OTP: {ex.Message}");
                return ServiceResult<RequestOtpResponseDto>.Fail("Unable to reach the server. Please try again.", "NETWORK_ERROR");
            }
            catch (TaskCanceledException ex)
            {
                Log($"Request OTP timed out/canceled: {ex.Message}");
                return ServiceResult<RequestOtpResponseDto>.Fail("The request timed out. Please try again.", "REQUEST_TIMEOUT");
            }
            catch (Exception ex)
            {
                Log($"Unexpected error while requesting OTP: {ex.Message}");
                return ServiceResult<RequestOtpResponseDto>.Fail("An unexpected error occurred.", "UNKNOWN_ERROR");
            }
        }

        public async Task<ServiceResult<VerifyOtpResponseDto>> VerifyPasswordOtpAsync(string email, string otp, CancellationToken cancellationToken = default)
        {
            if (!ValidationHelper.IsValidEmail(email))
            {
                return ServiceResult<VerifyOtpResponseDto>.Fail("Invalid email format.", "INVALID_EMAIL");
            }

            if (string.IsNullOrWhiteSpace(otp) || otp.Length != 6)
            {
                return ServiceResult<VerifyOtpResponseDto>.Fail("OTP must be 6 digits.", "INVALID_OTP");
            }

            var request = new VerifyOtpDto
            {
                Email = email.Trim(),
                Otp = otp.Trim()
            };

            try
            {
                Log("Verifying forgot-password OTP.");

                var response = await _httpClient.PostAsJsonAsync(ApiEndpoints.Auth.VerifyPasswordOtp, request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await ReadErrorAsync(response, cancellationToken);
                    var code = NormalizeErrorCode(error.ErrorCode, error.Message, error.Error);
                    var message = MapOtpError(code);
                    Log($"OTP verification failed. Status={(int)response.StatusCode} Code={code}");
                    return ServiceResult<VerifyOtpResponseDto>.Fail(message, code);
                }

                var data = await response.Content.ReadFromJsonAsync<VerifyOtpResponseDto>(cancellationToken: cancellationToken)
                           ?? new VerifyOtpResponseDto { Success = false, Message = "Invalid response from server." };

                if (string.IsNullOrWhiteSpace(data.ResetToken))
                {
                    return ServiceResult<VerifyOtpResponseDto>.Fail("Verification succeeded but no reset token was returned.", "MISSING_RESET_TOKEN");
                }

                data.Success = true;
                return ServiceResult<VerifyOtpResponseDto>.Ok(data, data.Message);
            }
            catch (HttpRequestException ex)
            {
                Log($"Network error while verifying OTP: {ex.Message}");
                return ServiceResult<VerifyOtpResponseDto>.Fail("Unable to reach the server. Please try again.", "NETWORK_ERROR");
            }
            catch (TaskCanceledException ex)
            {
                Log($"Verify OTP timed out/canceled: {ex.Message}");
                return ServiceResult<VerifyOtpResponseDto>.Fail("The request timed out. Please try again.", "REQUEST_TIMEOUT");
            }
            catch (Exception ex)
            {
                Log($"Unexpected error while verifying OTP: {ex.Message}");
                return ServiceResult<VerifyOtpResponseDto>.Fail("An unexpected error occurred.", "UNKNOWN_ERROR");
            }
        }

        public async Task<ServiceResult<ResetPasswordResponseDto>> ResetPasswordAsync(string resetToken, string newPassword, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(resetToken))
            {
                return ServiceResult<ResetPasswordResponseDto>.Fail("Reset token is missing.", "MISSING_RESET_TOKEN");
            }

            if (!ValidationHelper.IsStrongPassword(newPassword, out var passwordError))
            {
                return ServiceResult<ResetPasswordResponseDto>.Fail(passwordError, "WEAK_PASSWORD");
            }

            var request = new ResetPasswordDto
            {
                ResetToken = resetToken,
                NewPassword = newPassword
            };

            try
            {
                Log("Submitting new password.");

                var response = await _httpClient.PostAsJsonAsync(ApiEndpoints.Auth.ResetPassword, request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await ReadErrorAsync(response, cancellationToken);
                    var code = NormalizeErrorCode(error.ErrorCode, error.Message, error.Error);
                    var message = MapResetError(code);
                    Log($"Password reset failed. Status={(int)response.StatusCode} Code={code}");
                    return ServiceResult<ResetPasswordResponseDto>.Fail(message, code);
                }

                var data = await response.Content.ReadFromJsonAsync<ResetPasswordResponseDto>(cancellationToken: cancellationToken)
                           ?? new ResetPasswordResponseDto { Success = true, Message = "Password reset successfully." };

                data.Success = true;
                return ServiceResult<ResetPasswordResponseDto>.Ok(data, data.Message);
            }
            catch (HttpRequestException ex)
            {
                Log($"Network error while resetting password: {ex.Message}");
                return ServiceResult<ResetPasswordResponseDto>.Fail("Unable to reach the server. Please try again.", "NETWORK_ERROR");
            }
            catch (TaskCanceledException ex)
            {
                Log($"Reset password timed out/canceled: {ex.Message}");
                return ServiceResult<ResetPasswordResponseDto>.Fail("The request timed out. Please try again.", "REQUEST_TIMEOUT");
            }
            catch (Exception ex)
            {
                Log($"Unexpected error while resetting password: {ex.Message}");
                return ServiceResult<ResetPasswordResponseDto>.Fail("An unexpected error occurred.", "UNKNOWN_ERROR");
            }
        }

        private static async Task<ApiErrorResponseDto> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            try
            {
                return await response.Content.ReadFromJsonAsync<ApiErrorResponseDto>(cancellationToken: cancellationToken)
                       ?? new ApiErrorResponseDto();
            }
            catch
            {
                return new ApiErrorResponseDto();
            }
        }

        private static string NormalizeErrorCode(string? errorCode, string? message, string? error)
        {
            var source = string.IsNullOrWhiteSpace(errorCode)
                ? (message ?? error ?? string.Empty)
                : errorCode;

            return source.Trim().ToUpperInvariant().Replace(" ", "_");
        }

        private static string MapOtpError(string code)
        {
            if (code.Contains("EXPIRED"))
            {
                return "OTP expired. Please request a new code.";
            }

            if (code.Contains("INVALID") || code.Contains("OTP"))
            {
                return "Invalid OTP. Please check and try again.";
            }

            return "Unable to verify OTP. Please try again.";
        }

        private static string MapResetError(string code)
        {
            if (code.Contains("EXPIRED") || code.Contains("TOKEN"))
            {
                return "Reset token expired or invalid. Please restart the forgot-password flow.";
            }

            return "Unable to reset password. Please try again.";
        }

        private void Log(string message)
        {
            _logger($"[{DateTime.UtcNow:O}] [AuthService] {message}");
        }
    }
}
