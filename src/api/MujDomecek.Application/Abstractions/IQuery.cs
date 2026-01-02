using MediatR;

namespace MujDomecek.Application.Abstractions;

public interface IQuery<out TResponse> : IRequest<TResponse> { }
