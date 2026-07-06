using System.ComponentModel.DataAnnotations;

namespace Notrelix.Infrastructure.Messaging.Options;

public sealed class RabbitMqOptions
{
    [Required]
    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 5672;

    public string VHost { get; set; } = "/";

    [Required]
    public string Username { get; set; } = "guest";

    [Required]
    public string Password { get; set; } = "guest";

    public bool UseSsl { get; set; }

    public int PrefetchCount { get; set; } = 16;

    public int ConcurrentMessageLimit { get; set; }

    public int RetryCount { get; set; } = 3;

    public int RetryIntervalMs { get; set; } = 200;

    public int CircuitBreakerTripThreshold { get; set; } = 15;

    public int CircuitBreakerActiveThreshold { get; set; } = 10;

    public int CircuitBreakerResetIntervalMinutes { get; set; } = 5;

    public int HealthCheckDegradedSeconds { get; set; } = 30;

    public int HealthCheckUnhealthySeconds { get; set; } = 60;
}
