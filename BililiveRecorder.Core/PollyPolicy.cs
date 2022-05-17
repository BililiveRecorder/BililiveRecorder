using System;
using System.Net.Http;
using BililiveRecorder.Core.Api;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Polly;
using Polly.Caching.Memory;
using Polly.CircuitBreaker;
using Polly.Registry;
using Serilog;

namespace BililiveRecorder.Core
{
    public class PollyPolicy : PolicyRegistry
    {
        private static readonly ILogger logger = Log.ForContext<PollyPolicy>();

        public PollyPolicy()
        {
            this.IpBlockedHttp412CircuitBreakerPolicy = Policy
                .Handle<Http412Exception>()
                .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 1,
                durationOfBreak: TimeSpan.FromMinutes(2),
                onBreak: (_, _) =>
                {
                    logger.Warning("检测到被屏蔽(HTTP 412)，暂停发送请求");
                },
                onReset: () =>
                {
                    logger.Information("屏蔽解除，恢复发送请求");
                },
                onHalfOpen: () =>
                {
                    logger.Debug(nameof(this.IpBlockedHttp412CircuitBreakerPolicy) + " onHalfOpen");
                });

            this.RequestFailedCircuitBreakerPolicy = Policy
                .Handle<HttpRequestException>()
                .Or<JsonException>()
                .Or<BilibiliApiResponseCodeNotZeroException>()
                .AdvancedCircuitBreakerAsync(
                failureThreshold: 0.8,
                samplingDuration: TimeSpan.FromSeconds(30),
                minimumThroughput: 6,
                durationOfBreak: TimeSpan.FromMinutes(1),
                onBreak: (_, _) =>
                {
                    logger.Warning("大部分请求出错，暂停发送请求");
                },
                onReset: () =>
                {
                    logger.Information("恢复正常发送请求");
                },
                onHalfOpen: () =>
                {
                    logger.Debug(nameof(this.RequestFailedCircuitBreakerPolicy) + " onHalfOpen");
                });

            var retry = Policy.Handle<Exception>().RetryAsync();

            var bulkhead = Policy.BulkheadAsync(maxParallelization: 5, maxQueuingActions: 200);

            this.memoryCache = new MemoryCache(new MemoryCacheOptions());
            var memoryCacheProvider = new MemoryCacheProvider(this.memoryCache);
            var cachePolicy = Policy.CacheAsync(memoryCacheProvider, TimeSpan.FromMinutes(2));

            this[PolicyNames.PolicyRoomInfoApiRequestAsync] = Policy.WrapAsync(bulkhead, retry, this.IpBlockedHttp412CircuitBreakerPolicy, this.RequestFailedCircuitBreakerPolicy);
            this[PolicyNames.PolicyDanmakuApiRequestAsync] = Policy.WrapAsync(cachePolicy, bulkhead, retry, this.IpBlockedHttp412CircuitBreakerPolicy, this.RequestFailedCircuitBreakerPolicy);
            this[PolicyNames.PolicyStreamApiRequestAsync] = Policy.WrapAsync(bulkhead, retry, this.IpBlockedHttp412CircuitBreakerPolicy, this.RequestFailedCircuitBreakerPolicy);
        }

        public readonly MemoryCache memoryCache;
        public readonly AsyncCircuitBreakerPolicy IpBlockedHttp412CircuitBreakerPolicy;
        public readonly AsyncCircuitBreakerPolicy RequestFailedCircuitBreakerPolicy;
    }
}
