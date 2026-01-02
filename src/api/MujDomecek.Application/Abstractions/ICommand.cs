using MediatR;

namespace MujDomecek.Application.Abstractions;

public interface ICommand<out TResponse> : IRequest<TResponse> { }
