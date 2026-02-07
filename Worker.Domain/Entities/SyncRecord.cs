using Worker.Domain.Enums;

namespace Worker.Domain.Entities;

public sealed class SyncRecord : BaseEntity
{
    public string TableName { get; set; } = string.Empty;
    public string RecordId { get; set; } = string.Empty;
    public string RecordData { get; set; } = string.Empty; // JSON payload
    public string OperationType { get; set; } = string.Empty; // e.g., "Insert", "Update", "Delete"

    public ProcessingStatus Status { get; set; }
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
    public string? MainframeResponse { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime? NextAttemptAt { get; set; } // stop backoff

    //Core Rules
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(TableName)) return false;
        if (string.IsNullOrWhiteSpace(RecordId)) return false;
        return true;
    }
}




