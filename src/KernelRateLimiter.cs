using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Asyncs.Locks;
using Soenneker.Extensions.ValueTask;
using Soenneker.SemanticKernel.Dtos.Options;
using Soenneker.SemanticKernel.Pool.Abstract;

namespace Soenneker.SemanticKernel.Pool;

///<inheritdoc cref="IKernelRateLimiter"/>
public sealed class KernelRateLimiter : IKernelRateLimiter
{
    private readonly ConcurrentQueue<DateTimeOffset> _secondWindow = new();
    private readonly ConcurrentQueue<DateTimeOffset> _minuteWindow = new();
    private readonly ConcurrentQueue<DateTimeOffset> _dayWindow = new();
    private readonly ConcurrentQueue<(DateTimeOffset Timestamp, int Tokens)> _tokenDayWindow = new();

    private readonly int? _requestsPerSecond;
    private readonly int? _requestsPerMinute;
    private readonly int? _requestsPerDay;
    private readonly int? _tokensPerDay;

    private readonly AsyncLock _lock = new();

    public KernelRateLimiter(SemanticKernelOptions options)
    {
        _requestsPerSecond = options.RequestsPerSecond;
        _requestsPerMinute = options.RequestsPerMinute;
        _requestsPerDay = options.RequestsPerDay;
        _tokensPerDay = options.TokensPerDay;
    }

    public ValueTask<bool> TryConsume(CancellationToken cancellationToken = default) => TryConsume(1, cancellationToken);

    public async ValueTask<bool> TryConsume(int tokens, CancellationToken cancellationToken = default)
    {
        if (_requestsPerSecond is null && _requestsPerMinute is null && _requestsPerDay is null && _tokensPerDay is null)
        {
            return true;
        }

        using (await _lock.Lock(cancellationToken).NoSync())
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            CleanupWindow(_secondWindow, now.Ticks - TimeSpan.TicksPerSecond);
            CleanupWindow(_minuteWindow, now.Ticks - TimeSpan.TicksPerMinute);
            CleanupWindow(_dayWindow, now.Ticks - TimeSpan.TicksPerDay);
            CleanupTokenWindow(_tokenDayWindow, now.Ticks - TimeSpan.TicksPerDay);

            if (_requestsPerSecond is int rps && _secondWindow.Count >= rps)
                return false;

            if (_requestsPerMinute is int rpm && _minuteWindow.Count >= rpm)
                return false;

            if (_requestsPerDay is int rpd && _dayWindow.Count >= rpd)
                return false;

            if (_tokensPerDay is int tpd && GetTokenSum() + tokens > tpd)
                return false;

            var ts = new DateTimeOffset(now.Ticks, TimeSpan.Zero); // reuse trimmed

            _secondWindow.Enqueue(ts);
            _minuteWindow.Enqueue(ts);
            _dayWindow.Enqueue(ts);

            if (_tokensPerDay is not null)
                _tokenDayWindow.Enqueue((ts, tokens));

            return true;
        }
    }

    public async ValueTask<(int Second, int Minute, int Day)> GetRemaining(CancellationToken cancellationToken = default)
    {
        using (await _lock.Lock(cancellationToken).NoSync())
        {
            long nowTicks = DateTimeOffset.UtcNow.Ticks;

            CleanupWindow(_secondWindow, nowTicks - TimeSpan.TicksPerSecond);
            CleanupWindow(_minuteWindow, nowTicks - TimeSpan.TicksPerMinute);
            CleanupWindow(_dayWindow, nowTicks - TimeSpan.TicksPerDay);
            CleanupTokenWindow(_tokenDayWindow, nowTicks - TimeSpan.TicksPerDay);

            int secondRemaining = _requestsPerSecond.HasValue ? Math.Max(0, _requestsPerSecond.Value - _secondWindow.Count) : int.MaxValue;
            int minuteRemaining = _requestsPerMinute.HasValue ? Math.Max(0, _requestsPerMinute.Value - _minuteWindow.Count) : int.MaxValue;
            int dayRemaining = _requestsPerDay.HasValue ? Math.Max(0, _requestsPerDay.Value - _dayWindow.Count) : int.MaxValue;

            return (secondRemaining, minuteRemaining, dayRemaining);
        }
    }

    public async ValueTask<int> GetRemainingTokens(CancellationToken cancellationToken = default)
    {
        if (_tokensPerDay is null)
            return int.MaxValue;

        using (await _lock.Lock(cancellationToken).NoSync())
        {
            CleanupTokenWindow(_tokenDayWindow, DateTimeOffset.UtcNow.Ticks - TimeSpan.TicksPerDay);
            return Math.Max(0, _tokensPerDay.Value - GetTokenSum());
        }
    }

    private static void CleanupWindow(ConcurrentQueue<DateTimeOffset> window, long cutoffTicks)
    {
        while (window.TryPeek(out DateTimeOffset ts) && ts.Ticks < cutoffTicks)
            window.TryDequeue(out _);
    }

    private static void CleanupTokenWindow(ConcurrentQueue<(DateTimeOffset Timestamp, int Tokens)> window, long cutoffTicks)
    {
        while (window.TryPeek(out (DateTimeOffset Timestamp, int Tokens) item) && item.Timestamp.Ticks < cutoffTicks)
            window.TryDequeue(out _);
    }

    private int GetTokenSum()
    {
        var total = 0;

        foreach ((_, int tokens) in _tokenDayWindow)
        {
            total += tokens;
        }

        return total;
    }
}