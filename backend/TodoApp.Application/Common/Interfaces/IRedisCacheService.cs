

namespace TodoApp.Application.Common.Interfaces
{
    public interface IRedisCacheService
    {
        Task SetAsync<T>(string key, T value, TimeSpan? expiration);
        Task<T?> GetAsync<T>(string key);
        Task Remove<T>(string key);
        Task Refresh<T>(string key);
    }
}