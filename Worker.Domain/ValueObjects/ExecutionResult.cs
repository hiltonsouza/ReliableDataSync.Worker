namespace Worker.Domain.ValueObjects
{
    /// <summary>
    /// Result of attempting to apply a record to an external constrained system.
    /// Designed to be minimal and message-based.
    /// </summary>
    public sealed record ExecutionResult(
    bool Success,
    bool AlreadyExists,
    bool Retryable,
    string Message
)
    {
        public static ExecutionResult Ok(string message = "Processed successfully.")
            => new(true, false, false, message);

        public static ExecutionResult Exists(string message = "Record already exists.")
            => new(false, true, false, message);

        public static ExecutionResult RetryableError(string message)
            => new(false, false, true, message);

        public static ExecutionResult TerminalError(string message)
            => new(false, false, false, message);

        public ExecutionResult EnsureMessage()
            => string.IsNullOrWhiteSpace(Message) ? this with { Message = "No additional provided." } : this;
    }

}
