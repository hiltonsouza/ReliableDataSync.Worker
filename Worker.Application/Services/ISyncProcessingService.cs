using Microsoft.Extensions.Logging;
using Worker.Domain.Entities;
using Worker.Domain.Enums;
using Worker.Domain.Repositories;
using Worker.Domain.Shared;

namespace Worker.Application.Services;

public interface ISyncProcessingService
{
    Task ProcessPendingRecordsAsync(int batchSize, CancellationToken ct);
}
public class SyncProcessingService : ISyncProcessingService
{
    readonly ISyncRecordRepository _repository;
    readonly ILogger<SyncProcessingService> _logger;

    // delegate definition
    private delegate Task<Result> OperationStrategy(SyncRecord record, CancellationToken ct);

    // strategy dictionary
    private readonly Dictionary<string, OperationStrategy> _strategies;

    public SyncProcessingService(ISyncRecordRepository repository, ILogger<SyncProcessingService> logger)
    {
        _repository = repository;
        _logger = logger;

        //Map string -> function
        _strategies = new Dictionary<string, OperationStrategy>(StringComparer.OrdinalIgnoreCase)
        {
            { "INSERT", HandleInsertAsync },
            { "UPDATE", HandleInsertAsync },
            { "DELETE", HandleInsertAsync },
        };
    }
    public async Task ProcessPendingRecordsAsync(int batchSize, CancellationToken ct)
    {
        for (int i = 0; i < batchSize; i++)
        {
            if (ct.IsCancellationRequested) break;

            var record = await _repository.GetNextPendingAsync();

            if (record == null)
                break;

            try
            {
                if (!_strategies.TryGetValue(record.OperationType, out var strategy))
                {
                    await _repository.UpdateStatusAsync(record.Id, ProcessingStatus.Failed, $"Unsupported operation type: {record.OperationType}");
                    continue;
                }

                _logger.LogInformation("Running strategy '{Op}' to the register {Id}", record.OperationType, record.Id);

                var result = await strategy(record, ct);

                if (result.IsSuccess)
                {
                    await _repository.UpdateStatusAsync(record.Id, ProcessingStatus.Completed, "Success");
                }
                else
                {
                    //simple backoff of 2 minutes
                    var nextAttempt = DateTime.UtcNow.AddMinutes(2);
                    await _repository.UpdateStatusAsync(record.Id, ProcessingStatus.Retry, result.Error, nextAttempt);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing record {Id}", record.Id);
                await _repository.UpdateStatusAsync(record.Id, ProcessingStatus.Failed, ex.Message);
            }
        }
    }

    // consolidated strategy handler for all operations, can be easily extended to handle specific logic per operation type
    private async Task<Result> HandleInsertAsync(SyncRecord record, CancellationToken ct)
    {
        // Simulate external calling
        await Task.Delay(50, ct);
        return Result.Success();
    }

    private async Task<Result> HandleUpdateAsync(SyncRecord record, CancellationToken ct)
    {
        await Task.Delay(50, ct);
        return Result.Success();
    }

    private async Task<Result> HandleDeleteAsync(SyncRecord record, CancellationToken ct)
    {
        await Task.Delay(50, ct);
        return Result.Success();
    }
}