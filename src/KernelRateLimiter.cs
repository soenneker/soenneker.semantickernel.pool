using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Soenneker.SemanticKernel.Pool.Abstract;

namespace Soenneker.SemanticKernel.Pool;

/// <summary>
/// Implements a sliding window rate limiter with support for requests per second, per minute, and per day.
/// </summary>
public sealed class KernelRateLimiter : IKernelRateLimiter
{
    private readonly ConcurrentQueue<DateTimeOffset> _secondWindow;
    private readonly ConcurrentQueue<DateTimeOffset> _minuteWindow;
    private readonly ConcurrentQueue<DateTimeOffset> _dayWindow;
    private readonly int _requestsPerSecond;
    private readonly int _requestsPerMinute;
    private readonly int _requestsPerDay;
    private readonly AsyncLock _lock = new();

    public KernelRateLimiter(int requestsPerSecond, int requestsPerMinute, int requestsPerDay)
    {
        _requestsPerSecond = requestsPerSecond;
        _requestsPerMinute = requestsPerMinute;
        _requestsPerDay = requestsPerDay;
        _secondWindow = new ConcurrentQueue<DateTimeOffset>();
        _minuteWindow = new ConcurrentQueue<DateTimeOffset>();
        _dayWindow = new ConcurrentQueue<DateTimeOffset>();
    }

    public async ValueTask<bool> TryConsume(CancellationToken cancellationToken = default)
    {
        using (await _lock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            // Clean up expired entries
            CleanupWindow(_secondWindow, now.AddSeconds(-1));
            CleanupWindow(_minuteWindow, now.AddMinutes(-1));
            CleanupWindow(_dayWindow, now.AddDays(-1));

            // Check if we're under the limits
            if (_secondWindow.Count >= _requestsPerSecond || _minuteWindow.Count >= _requestsPerMinute || _dayWindow.Count >= _requestsPerDay)
            {
                return false;
            }

            // Add new request
            _secondWindow.Enqueue(now);
            _minuteWindow.Enqueue(now);
            _dayWindow.Enqueue(now);

            return true;
        }
    }

    public async ValueTask<(int Second, int Minute, int Day)> GetRemaining(CancellationToken cancellationToken = default)
    {
        using (await _lock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            // Clean up expired entries
            CleanupWindow(_secondWindow, now.AddSeconds(-1));
            CleanupWindow(_minuteWindow, now.AddMinutes(-1));
            CleanupWindow(_dayWindow, now.AddDays(-1));

            return (Second: Math.Max(0, _requestsPerSecond - _secondWindow.Count), Minute: Math.Max(0, _requestsPerMinute - _minuteWindow.Count),
                Day: Math.Max(0, _requestsPerDay - _dayWindow.Count));
        }
    }

    private static void CleanupWindow(ConcurrentQueue<DateTimeOffset> window, DateTimeOffset cutoff)
    {
        while (window.TryPeek(out DateTimeOffset oldest) && oldest < cutoff)
        {
            window.TryDequeue(out _);
        }
    }
}