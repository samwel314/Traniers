using ERP.Domain.Common.Results;

namespace ERP.Domain.Common.Exceptions;

/// <summary>
/// Thrown only when an invariant is broken in a way the caller could not have
/// anticipated - i.e. a bug. Expected failures return Result instead.
/// </summary>
public class DomainException : Exception
{
    public DomainException(Error error) : base(error.Description) => Error = error;

    public DomainException(string message) : base(message)
        => Error = Error.Failure("Domain.InvariantViolated", message);

    public Error Error { get; }
}
