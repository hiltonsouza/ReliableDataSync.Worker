using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Worker.Application.Abstractions.External;
using Worker.Application.Abstractions.Persistence;
using Worker.Application.Abstractions.Time;
using Worker.Application.Services;
using Worker.Domain.Enums;

namespace Worker.Application.UseCases
{
    public sealed class ProcessNextRecordUseCase
    {
        readonly IRecordQueueRepository _queueRepository;
        readonly IExternalSystemClient _externalSystem;
        readonly ExecutionClassifier _classifier;
        readonly IClock _clock;

        public ProcessNextRecordUseCase(
            IRecordQueueRepository queueRepository,
            IExternalSystemClient externalSystem,
            ExecutionClassifier classifier,
            IClock clock)
        {
            _queueRepository = queueRepository;
            _externalSystem = externalSystem;
            _classifier = classifier;
            _clock = clock;
        }

        public async Task<bool> ExecuteAsync(CancellationToken cancellationToken)
        {
            var nowUtc = _clock.UtcNow;

            var record = await _queueRepository.ClaimNextEligibleAsync(nowUtc, cancellationToken);

            if (record is null) return false;

            try
            {
                // External call (can be mainframe, terminal, API, etc.)
                var result = await _externalSystem.UpsertAsync(record, cancellationToken);
                var status = _classifier.Classify(result);

                await _queueRepository.SaveOutcomeAsync(record.Id, status, result.EnsureMessage().Message, nowUtc, cancellationToken);

                return true;
            }
            catch (OperationCanceledException)
            {
                // propagate cancellations
                throw;
            }
            catch (Exception ex)
            {
                var message = $"Unexpected error: {ex.GetType().Name} - {ex.Message}";
                await _queueRepository.SaveOutcomeAsync(record.Id, ProcessingStatus.RetryableError, message, nowUtc, cancellationToken);
                return true;
            }

        }
    }
}
