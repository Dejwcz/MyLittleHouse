using MujDomecek.Application.Abstractions;

namespace MujDomecek.Infrastructure.Services;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
