using System;
using System.Collections.Generic;
using System.Text;
using Worker.Domain.Entities;
using Worker.Domain.Enums;
using Worker.Domain.Repositories;

namespace Worker.Application.Abstractions.Persistence
{
    public interface ISyncRecordRepository : IRepository<SyncRecord>
    {
        Task<SyncRecord?> GetNextPendingAsync();
        Task UpdateStatusAsync(Guid id, ProcessingStatus status, string? message = "", DateTime? nextAttempt = null);
    }
}
