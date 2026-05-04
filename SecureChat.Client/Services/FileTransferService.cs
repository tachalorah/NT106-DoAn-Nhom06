using System;
using System.Threading;
using System.Threading.Tasks;

namespace SecureChat.Client.Services
{
    /// <summary>
    /// Prepared for moving file-transfer logic out of UI code.
    /// Provides download and verification helpers.
    /// </summary>
    public sealed class FileTransferService
    {
        /// <summary>
        /// Download a file from the given URL to destinationPath, reporting progress and honoring cancellation.
        /// This implementation delegates to FileService.DownloadAsync to reuse streaming logic.
        /// </summary>
        public async Task DownloadAsync(string url, string destinationPath, IProgress<int>? progress, CancellationToken token)
        {
            await FileService.DownloadAsync(url, destinationPath, progress, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Verify the file at filePath against expected SHA-256 hex string (case-insensitive).
        /// Returns true when matched.
        /// </summary>
        public async Task<bool> VerifyAsync(string filePath, string expectedSha256)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("filePath is required", nameof(filePath));
            if (string.IsNullOrWhiteSpace(expectedSha256)) return false;

            var localHash = await FileService.ComputeSha256Async(filePath).ConfigureAwait(false);
            return string.Equals(localHash, expectedSha256, StringComparison.OrdinalIgnoreCase);
        }
    }
}
