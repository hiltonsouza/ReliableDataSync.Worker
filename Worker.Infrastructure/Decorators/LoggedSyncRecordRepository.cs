using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Worker.Domain.Entities;
using Worker.Domain.Enums;
using Worker.Domain.Repositories;

namespace Worker.Infrastructure.Decorators;

public class LoggedSyncRecordRepository : ISyncRecordRepository
{
    readonly ISyncRecordRepository _inner;
    readonly ILogger<LoggedSyncRecordRepository> _logger;

    public LoggedSyncRecordRepository(ISyncRecordRepository inner, ILogger<LoggedSyncRecordRepository> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    // Specific methods for queue management
    public async Task<SyncRecord?> GetNextPendingAsync()
    {
        _logger.LogInformation("Fetching next pending record from queue...");
        var stopwatch = Stopwatch.StartNew();
        var record = await _inner.GetNextPendingAsync();

        stopwatch.Stop();

        if (record != null)
        {
            _logger.LogInformation("Record {RecordId} fetched successfully in {Elapsed}ms", record.Id, stopwatch.ElapsedMilliseconds);
        }
        else
        {
            _logger.LogDebug("No pending records found. Time elapsed: {Elapsed}ms", stopwatch.ElapsedMilliseconds);
        }

        return record;
    }

    public async Task UpdateStatusAsync(Guid id, ProcessingStatus status, string? message, DateTime? nextAttempt = null)
    {
        _logger.LogInformation("Updating record {Id} status to '{Status}'. Message: {Message}", id, status, message ?? "N/A");

        try
        {
            await _inner.UpdateStatusAsync(id, status, message, nextAttempt);
        }catch(Exception ex)
        {
            _logger.LogError(ex, "Failed to update status for Record {Id}.", id);
            throw;
        }            
    }


    // -- Base Repository Methods (Delegation with optional logging) --
    public Task<SyncRecord?> GetByIdAsync(Guid id)
    {
        return _inner.GetByIdAsync(id);
    }

    public Task<IEnumerable<SyncRecord>> GetAllAsync()
    {
        return _inner.GetAllAsync();
    }

    public async Task<bool> AddAsync(SyncRecord entity)
    {
        _logger.LogInformation("Adding new SyncRecord: {RecordId} ({OperationType})", entity.RecordId, entity.OperationType);

        return await _inner.AddAsync(entity);
    }

    public async Task<bool> UpdateAsync(SyncRecord entity)
    {
        // Detailed logging for full updates is often too noisy, keeping it debug
        _logger.LogDebug("Updating SyncRecord {Id} full entity.", entity.Id);
        return await _inner.UpdateAsync(entity);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        _logger.LogInformation("Soft-deleting SyncRecord {Id}.", id);
        return await _inner.DeleteAsync(id);
    }
}
