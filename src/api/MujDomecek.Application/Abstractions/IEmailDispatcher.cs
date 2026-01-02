using MujDomecek.Application.DTOs;

namespace MujDomecek.Application.Abstractions;

public interface IEmailDispatcher
{
    Task SendAsync(EmailJobRequest request, CancellationToken ct = default);
}
