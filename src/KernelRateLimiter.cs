using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Soenneker.SemanticKernel.Pool.Abstract;

namespace Soenneker.SemanticKernel.Pool;

///<inheritdoc cref="IKernelRateLimiter"/>
public sealed class KernelRateLimiter : IKernelRateLimiter
{
    private readonly ConcurrentQueue<DateTimeOffset> _secondWindow = new();
    private readonly ConcurrentQueue<DateTimeOffset> _minuteWindow = new();
    private readonly ConcurrentQueue<DateTimeOffset> _dayWindow = new();

    private readonly int? _requestsPerSecond;
    private readonly int? _requestsPerMinute;
    private readonly int? _requestsPerDay;

    private readonly AsyncLock _lock = new();

    public KernelRateLimiter(int? requestsPerSecond = null, int? requestsPerMinute = null, int? requestsPerDay = null)
    {
        _requestsPerSecond = requestsPerSecond;
        _requestsPerMinute = requestsPerMinute;
        _requestsPerDay = requestsPerDay;
    }

    public async ValueTask<bool> TryConsume(CancellationToken cancellationToken = default)
    {
        if (_requestsPerSecond is null && _requestsPerMinute is null && _requestsPerDay is null)
            return true;

        using (await _lock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            CleanupWindow(_secondWindow, now.AddSeconds(-1));
            CleanupWindow(_minuteWindow, now.AddMinutes(-1));
            CleanupWindow(_dayWindow, now.AddDays(-1));

            if (_requestsPerSecond is int rps && _secondWindow.Count >= rps)
                return false;

            if (_requestsPerMinute is int rpm && _minuteWindow.Count >= rpm)
                return false;

            if (_requestsPerDay is int rpd && _dayWindow.Count >= rpd)
                return false;

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

            var secondRemaining = int.MaxValue;
            if (_requestsPerSecond is int rps)
            {
                CleanupWindow(_secondWindow, now.AddSeconds(-1));
                secondRemaining = Math.Max(0, rps - _secondWindow.Count);
            }

            var minuteRemaining = int.MaxValue;
            if (_requestsPerMinute is int rpm)
            {
                CleanupWindow(_minuteWindow, now.AddMinutes(-1));
                minuteRemaining = Math.Max(0, rpm - _minuteWindow.Count);
            }

            var dayRemaining = int.MaxValue;
            if (_requestsPerDay is int rpd)
            {
                CleanupWindow(_dayWindow, now.AddDays(-1));
                dayRemaining = Math.Max(0, rpd - _dayWindow.Count);
            }

            return (secondRemaining, minuteRemaining, dayRemaining);
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
