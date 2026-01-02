namespace MujDomecek.Application.Abstractions;

public interface IClock
{
    DateTime UtcNow { get; }
}
