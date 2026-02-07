using System;
using System.Collections.Generic;
using System.Text;
using Worker.Domain.Entities;
using Worker.Domain.Enums;

namespace Worker.Domain.Repositories
{
    // Inherits everything from the generic method + specific queue methods.
    public interface ISyncRecordRepository: IRepository<SyncRecord>
    {
        //the atomic queue method ( replaces the old ClaimNextEligibleAsync)
        Task<SyncRecord?> GetNextPendingAsync();
        Task UpdateStatusAsync(Guid id, ProcessingStatus status, string message, DateTime? nextAttempt = null);
    }

}
