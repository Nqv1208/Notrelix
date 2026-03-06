using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using TodoApp.Application.Common.Interfaces;

namespace TodoApp.Infrastructure.Caching
{
    public class RedisCacheService : IRedisCacheService
    {
        private IDistributedCache _cache;

        public RedisCacheService(IDistributedCache cache)
        {
            _cache = cache;
        }
        public async Task<T?> GetAsync<T>(string key)
        {
            var data = await _cache.GetStringAsync(key);

            if(data is null) return default(T);

            return JsonSerializer.Deserialize<T>(data);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration)
        {
            var options = new DistributedCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = expiration
            };

            await _cache.SetStringAsync(key, JsonSerializer.Serialize(value), options);
        }

        public async Task Refresh<T>(string key)
        {
            throw new NotImplementedException();
        }

        public async Task Remove<T>(string key)
        {
            throw new NotImplementedException();
        }
    }
}