namespace Worker.Application.Abstractions.Metrics;

public interface IWorkerMetrics
{
    IDisposable TrackProcessing(string externalKey);
    void RecordSuccess();
    void RecordFailure();
    void RecordRetryable();
    void RecordEmptyPoll();
}

