namespace Domain.Exceptions;

/// <summary>
/// Base exception for domain-level business rule violations.
/// Contains an error code used in the standard error response.
/// </summary>
public class TicketDomainException : Exception
{
    public string Code { get; }
    public int StatusCode { get; }

    public TicketDomainException(string code, string message, int statusCode = 400)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }
}

public class InvalidTransitionException : TicketDomainException
{
    public InvalidTransitionException(string message)
        : base("INVALID_TICKET_TRANSITION", message, 409) { }
}

public class TicketAlreadyClaimedException : TicketDomainException
{
    public TicketAlreadyClaimedException(string ticketNumber)
        : base("TICKET_ALREADY_CLAIMED", $"{ticketNumber} is already assigned to another agent.", 409) { }
}

public class ReopenWindowExpiredException : TicketDomainException
{
    public ReopenWindowExpiredException()
        : base("REOPEN_WINDOW_EXPIRED", "The 48-hour reopen window has expired.", 409) { }
}

public class TicketClosedException : TicketDomainException
{
    public TicketClosedException()
        : base("TICKET_CLOSED", "A closed ticket cannot receive new messages or change state.", 409) { }
}

public class TicketNotFoundException : TicketDomainException
{
    public TicketNotFoundException()
        : base("TICKET_NOT_FOUND", "The requested ticket was not found.", 404) { }
}

public class ForbiddenException : TicketDomainException
{
    public ForbiddenException(string message = "You do not have permission to perform this action.")
        : base("FORBIDDEN", message, 403) { }
}

public class ConcurrencyConflictException : TicketDomainException
{
    public ConcurrencyConflictException()
        : base("CONCURRENCY_CONFLICT", "The ticket was modified by another user. Please retry.", 409) { }
}
