using Microsoft.Extensions.Logging;
using Worker.Application.Abstractions.External;
using Worker.Application.Abstractions.Persistence;
using Worker.Application.Abstractions.Time;
using Worker.Application.Services;
using Worker.Domain.Enums;

namespace Worker.Application.UseCases;

public sealed class ProcessNextRecordUseCase
{
    private readonly IRecordQueueRepository _queueRepository;
    private readonly IExternalSystemClient _externalSystem;
    private readonly ExecutionClassifier _classifier;
    private readonly IClock _clock;
    private readonly ILogger<ProcessNextRecordUseCase> _logger;

    public ProcessNextRecordUseCase(
        IRecordQueueRepository queueRepository,
        IExternalSystemClient externalSystem,
        ExecutionClassifier classifier,
        IClock clock,
        ILogger<ProcessNextRecordUseCase> logger)
    {
        _queueRepository = queueRepository;
        _externalSystem = externalSystem;
        _classifier = classifier;
        _clock = clock;
        _logger = logger;
    }

    public async Task<bool> ExecuteAsync(CancellationToken cancellationToken)
    {
        var startedUtc = _clock.UtcNow;

        _logger.LogDebug("Looking for eligible record at {Now:o}", startedUtc);

        var record = await _queueRepository.ClaimNextEligibleAsync(startedUtc, cancellationToken);

        if (record is null)
        {
            _logger.LogDebug("No eligible record found.");
            return false;
        }

        // record.Attempts aqui já está incrementado (por causa do claim), então é o attempt atual
        _logger.LogInformation(
            "Processing {ExternalKey} Attempt={Attempt} Status={Status} Id={Id}",
            record.ExternalKey, record.Attempts, record.Status, record.Id);

        try
        {
            _logger.LogInformation("Calling external system for {ExternalKey} Id={Id}...", record.ExternalKey, record.Id);

            var result = await _externalSystem.UpsertAsync(record, cancellationToken);
            var status = _classifier.Classify(result);

            var msg = result.EnsureMessage().Message ?? "";

            _logger.LogInformation(
                "External result {ExternalKey} => {Status}. Message={Message}",
                record.ExternalKey, status, msg);
            
            var finishedUtc = _clock.UtcNow;

            await _queueRepository.SaveOutcomeAsync(record.Id, status, msg, finishedUtc, cancellationToken);

            _logger.LogInformation("Saved outcome {ExternalKey} => {Status}", record.ExternalKey, status);

            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var message = $"Unexpected error: {ex.GetType().Name} - {ex.Message}";
            _logger.LogError(ex, "Unhandled error processing {ExternalKey} Id={Id}", record.ExternalKey, record.Id);

            await _queueRepository.SaveOutcomeAsync(record.Id, ProcessingStatus.RetryableError, message, startedUtc, cancellationToken);
            _logger.LogWarning("Saved outcome {ExternalKey} => RetryableError", record.ExternalKey);

            return true;
        }
    }

}
