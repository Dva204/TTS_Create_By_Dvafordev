using System.Diagnostics;

namespace TTS_Create_By_DvaForDev.Services;

/// <summary>
/// A started uvicorn process is not necessarily an HTTP-ready backend.
/// Wait asynchronously for its health endpoint without freezing the UI,
/// killing an existing job or treating a brief connection refusal as failure.
/// </summary>
public static class BackendStartupWaiter
{
    public static async Task WaitUntilReadyAsync(
        Func<CancellationToken, Task<bool>> healthProbe,
        Func<bool> hasProcessExited,
        TimeSpan maximumWait,
        CancellationToken token,
        TimeSpan? pollInterval = null,
        Action<TimeSpan>? onProgress = null)
    {
        ArgumentNullException.ThrowIfNull(healthProbe);
        ArgumentNullException.ThrowIfNull(hasProcessExited);
        if (maximumWait <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(maximumWait));
        TimeSpan interval = pollInterval ?? TimeSpan.FromSeconds(1);
        if (interval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(pollInterval));
        var elapsed = Stopwatch.StartNew();
        int lastProgressBucket = -1;
        while (true)
        {
            token.ThrowIfCancellationRequested();
            if (await healthProbe(token)) return;
            if (hasProcessExited())
                throw new InvalidOperationException(
                    "Backend Python đã thoát trước khi API sẵn sàng. Xem log để biết lỗi import/model.");
            if (elapsed.Elapsed >= maximumWait)
                throw new TimeoutException(
                    $"Backend chưa sẵn sàng sau {maximumWait.TotalSeconds:F0} giây. " +
                    "Tiến trình vẫn được giữ nguyên; ứng dụng sẽ tiếp tục kiểm tra kết nối.");
            int bucket = (int)(elapsed.Elapsed.TotalSeconds / 15);
            if (bucket > lastProgressBucket && elapsed.Elapsed.TotalSeconds >= 15)
            {
                lastProgressBucket = bucket;
                onProgress?.Invoke(elapsed.Elapsed);
            }
            await Task.Delay(interval, token);
        }
    }
}
