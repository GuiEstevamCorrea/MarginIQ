namespace Application.Services;

/// <summary>
/// Cache interface for AI responses to improve performance.
/// Reduces AI calls and improves response times.
/// </summary>
public interface IAIResponseCache
{
    /// <summary>
    /// Tries to get a cached value
    /// </summary>
    /// <typeparam name="T">Type of cached value</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="value">Retrieved value if found</param>
    /// <returns>True if value was found in cache</returns>
    bool TryGet<T>(string key, out T? value) where T : class;

    /// <summary>
    /// Sets a value in cache with expiration
    /// </summary>
    /// <typeparam name="T">Type of value to cache</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="value">Value to cache</param>
    /// <param name="expiration">How long to keep in cache</param>
    void Set<T>(string key, T value, TimeSpan expiration) where T : class;

    /// <summary>
    /// Removes a value from cache
    /// </summary>
    /// <param name="key">Cache key</param>
    void Remove(string key);

    /// <summary>
    /// Clears all cached values
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets cache statistics
    /// </summary>
    /// <returns>Cache statistics</returns>
    CacheStatistics GetStatistics();
}

/// <summary>
/// Cache statistics for monitoring
/// </summary>
public class CacheStatistics
{
    public int TotalEntries { get; set; }
    public long TotalHits { get; set; }
    public long TotalMisses { get; set; }
    public double HitRate => TotalHits + TotalMisses > 0
        ? (double)TotalHits / (TotalHits + TotalMisses) * 100
        : 0;
    public long MemoryUsageBytes { get; set; }
    public DateTime LastClearedAt { get; set; }
}

/// <summary>
/// Performance metrics interface for tracking AI service performance
/// </summary>
public interface IPerformanceMetrics
{
    /// <summary>
    /// Records a successful operation
    /// </summary>
    void RecordSuccess(string operation);

    /// <summary>
    /// Records an error
    /// </summary>
    void RecordError(string operation, string errorType);

    /// <summary>
    /// Records a timeout
    /// </summary>
    void RecordTimeout(string operation);

    /// <summary>
    /// Records response time
    /// </summary>
    void RecordResponseTime(string operation, TimeSpan duration, bool fromCache);

    /// <summary>
    /// Records cache hit
    /// </summary>
    void RecordCacheHit(string operation);

    /// <summary>
    /// Records cache miss
    /// </summary>
    void RecordCacheMiss(string operation);

    /// <summary>
    /// Records circuit breaker open event
    /// </summary>
    void RecordCircuitBreakerOpen(string operation);

    /// <summary>
    /// Records fallback usage
    /// </summary>
    void RecordFallbackUsed(string operation, string reason);

    /// <summary>
    /// Gets performance statistics
    /// </summary>
    /// <param name="operation">Operation name (optional, null for all)</param>
    /// <param name="since">Start date (optional)</param>
    /// <returns>Performance statistics</returns>
    PerformanceStatistics GetStatistics(string? operation = null, DateTime? since = null);
}

/// <summary>
/// Performance statistics
/// </summary>
public class PerformanceStatistics
{
    public string? Operation { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public long TotalRequests { get; set; }
    public long SuccessfulRequests { get; set; }
    public long FailedRequests { get; set; }
    public long TimeoutRequests { get; set; }
    public long CacheHits { get; set; }
    public long CacheMisses { get; set; }
    public long FallbackUsages { get; set; }
    public long CircuitBreakerTrips { get; set; }
    
    public double SuccessRate => TotalRequests > 0
        ? (double)SuccessfulRequests / TotalRequests * 100
        : 0;
    
    public double CacheHitRate => (CacheHits + CacheMisses) > 0
        ? (double)CacheHits / (CacheHits + CacheMisses) * 100
        : 0;
    
    public double TimeoutRate => TotalRequests > 0
        ? (double)TimeoutRequests / TotalRequests * 100
        : 0;

    public TimeSpan? AverageResponseTime { get; set; }
    public TimeSpan? P50ResponseTime { get; set; }
    public TimeSpan? P95ResponseTime { get; set; }
    public TimeSpan? P99ResponseTime { get; set; }
    
    public Dictionary<string, long> ErrorsByType { get; set; } = new();
    public Dictionary<string, long> FallbackReasons { get; set; } = new();
}

/// <summary>
/// In-memory implementation of AI response cache (for development/testing)
/// In production, use Redis or similar distributed cache
/// </summary>
public class InMemoryAIResponseCache : IAIResponseCache
{
    private readonly Dictionary<string, CacheEntry> _cache = new();
    private readonly object _lock = new object();
    private long _totalHits = 0;
    private long _totalMisses = 0;
    private DateTime _lastClearedAt = DateTime.UtcNow;

    public bool TryGet<T>(string key, out T? value) where T : class
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                if (entry.ExpiresAt > DateTime.UtcNow)
                {
                    _totalHits++;
                    value = entry.Value as T;
                    return value != null;
                }
                else
                {
                    // Expired - remove it
                    _cache.Remove(key);
                }
            }

            _totalMisses++;
            value = null;
            return false;
        }
    }

    public void Set<T>(string key, T value, TimeSpan expiration) where T : class
    {
        lock (_lock)
        {
            _cache[key] = new CacheEntry
            {
                Value = value,
                ExpiresAt = DateTime.UtcNow.Add(expiration),
                CreatedAt = DateTime.UtcNow
            };

            // Cleanup expired entries periodically
            if (_cache.Count > 1000)
            {
                CleanupExpiredEntries();
            }
        }
    }

    public void Remove(string key)
    {
        lock (_lock)
        {
            _cache.Remove(key);
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _cache.Clear();
            _lastClearedAt = DateTime.UtcNow;
        }
    }

    public CacheStatistics GetStatistics()
    {
        lock (_lock)
        {
            // Estimate memory usage (rough approximation)
            long memoryUsage = _cache.Count * 1024; // Assume ~1KB per entry

            return new CacheStatistics
            {
                TotalEntries = _cache.Count,
                TotalHits = _totalHits,
                TotalMisses = _totalMisses,
                MemoryUsageBytes = memoryUsage,
                LastClearedAt = _lastClearedAt
            };
        }
    }

    private void CleanupExpiredEntries()
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _cache
            .Where(kvp => kvp.Value.ExpiresAt <= now)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _cache.Remove(key);
        }
    }

    private class CacheEntry
    {
        public object Value { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

/// <summary>
/// In-memory implementation of performance metrics (for development/testing)
/// In production, use Prometheus, Application Insights, or similar
/// </summary>
public class InMemoryPerformanceMetrics : IPerformanceMetrics
{
    private readonly List<MetricEntry> _entries = new();
    private readonly object _lock = new object();

    public void RecordSuccess(string operation)
    {
        lock (_lock)
        {
            _entries.Add(new MetricEntry
            {
                Operation = operation,
                Type = MetricType.Success,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    public void RecordError(string operation, string errorType)
    {
        lock (_lock)
        {
            _entries.Add(new MetricEntry
            {
                Operation = operation,
                Type = MetricType.Error,
                ErrorType = errorType,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    public void RecordTimeout(string operation)
    {
        lock (_lock)
        {
            _entries.Add(new MetricEntry
            {
                Operation = operation,
                Type = MetricType.Timeout,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    public void RecordResponseTime(string operation, TimeSpan duration, bool fromCache)
    {
        lock (_lock)
        {
            _entries.Add(new MetricEntry
            {
                Operation = operation,
                Type = fromCache ? MetricType.CacheHit : MetricType.ResponseTime,
                ResponseTime = duration,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    public void RecordCacheHit(string operation)
    {
        lock (_lock)
        {
            _entries.Add(new MetricEntry
            {
                Operation = operation,
                Type = MetricType.CacheHit,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    public void RecordCacheMiss(string operation)
    {
        lock (_lock)
        {
            _entries.Add(new MetricEntry
            {
                Operation = operation,
                Type = MetricType.CacheMiss,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    public void RecordCircuitBreakerOpen(string operation)
    {
        lock (_lock)
        {
            _entries.Add(new MetricEntry
            {
                Operation = operation,
                Type = MetricType.CircuitBreakerOpen,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    public void RecordFallbackUsed(string operation, string reason)
    {
        lock (_lock)
        {
            _entries.Add(new MetricEntry
            {
                Operation = operation,
                Type = MetricType.FallbackUsed,
                FallbackReason = reason,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    public PerformanceStatistics GetStatistics(string? operation = null, DateTime? since = null)
    {
        lock (_lock)
        {
            var sinceDate = since ?? DateTime.UtcNow.AddHours(-1);
            var entries = _entries
                .Where(e => e.Timestamp >= sinceDate)
                .Where(e => operation == null || e.Operation == operation)
                .ToList();

            var responseTimes = entries
                .Where(e => e.ResponseTime.HasValue)
                .Select(e => e.ResponseTime!.Value)
                .OrderBy(t => t)
                .ToList();

            var stats = new PerformanceStatistics
            {
                Operation = operation,
                PeriodStart = sinceDate,
                PeriodEnd = DateTime.UtcNow,
                TotalRequests = entries.Count(e => e.Type == MetricType.Success || e.Type == MetricType.Error || e.Type == MetricType.Timeout),
                SuccessfulRequests = entries.Count(e => e.Type == MetricType.Success),
                FailedRequests = entries.Count(e => e.Type == MetricType.Error),
                TimeoutRequests = entries.Count(e => e.Type == MetricType.Timeout),
                CacheHits = entries.Count(e => e.Type == MetricType.CacheHit),
                CacheMisses = entries.Count(e => e.Type == MetricType.CacheMiss),
                FallbackUsages = entries.Count(e => e.Type == MetricType.FallbackUsed),
                CircuitBreakerTrips = entries.Count(e => e.Type == MetricType.CircuitBreakerOpen)
            };

            if (responseTimes.Any())
            {
                stats.AverageResponseTime = TimeSpan.FromMilliseconds(responseTimes.Average(t => t.TotalMilliseconds));
                stats.P50ResponseTime = responseTimes[responseTimes.Count / 2];
                stats.P95ResponseTime = responseTimes[(int)(responseTimes.Count * 0.95)];
                stats.P99ResponseTime = responseTimes[(int)(responseTimes.Count * 0.99)];
            }

            stats.ErrorsByType = entries
                .Where(e => e.Type == MetricType.Error && e.ErrorType != null)
                .GroupBy(e => e.ErrorType!)
                .ToDictionary(g => g.Key, g => (long)g.Count());

            stats.FallbackReasons = entries
                .Where(e => e.Type == MetricType.FallbackUsed && e.FallbackReason != null)
                .GroupBy(e => e.FallbackReason!)
                .ToDictionary(g => g.Key, g => (long)g.Count());

            return stats;
        }
    }

    private enum MetricType
    {
        Success,
        Error,
        Timeout,
        ResponseTime,
        CacheHit,
        CacheMiss,
        CircuitBreakerOpen,
        FallbackUsed
    }

    private class MetricEntry
    {
        public string Operation { get; set; } = string.Empty;
        public MetricType Type { get; set; }
        public DateTime Timestamp { get; set; }
        public TimeSpan? ResponseTime { get; set; }
        public string? ErrorType { get; set; }
        public string? FallbackReason { get; set; }
    }
}
