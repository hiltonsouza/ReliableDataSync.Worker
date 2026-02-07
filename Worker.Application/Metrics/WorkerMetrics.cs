using System.Diagnostics;
using System.Diagnostics.Metrics;
using Worker.Application.Abstractions.Metrics;

namespace Worker.Application.Metrics;

public sealed class WorkerMetrics : IWorkerMetrics
{
    private readonly Counter<long> _processedCounter;
    private readonly Counter<long> _failedCounter;
    private readonly Counter<long> _retryableCounter;
    private readonly Counter<long> _emptyPollCounter;
    private readonly Histogram<double> _durationHistogram;

    public WorkerMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("Worker.Sync");

        _processedCounter = meter.CreateCounter<long>("worker.records.processed");
        _failedCounter = meter.CreateCounter<long>("worker.records.failed");
        _retryableCounter = meter.CreateCounter<long>("worker.records.retryable");
        _emptyPollCounter = meter.CreateCounter<long>("worker.poll.empty");
        _durationHistogram = meter.CreateHistogram<double>("worker.record.duration.ms", unit: "ms");
    }

    public IDisposable TrackProcessing(string externalKey)
        => new DurationScope(_durationHistogram, externalKey);

    public void RecordSuccess() => _processedCounter.Add(1);

    public void RecordFailure() => _failedCounter.Add(1);

    public void RecordRetryable() => _retryableCounter.Add(1);

    public void RecordEmptyPoll() => _emptyPollCounter.Add(1);

    private sealed class DurationScope : IDisposable
    {
        private readonly Histogram<double> _histogram;
        private readonly string _externalKey;
        private readonly Stopwatch _stopwatch;

        public DurationScope(Histogram<double> histogram, string externalKey)
        {
            _histogram = histogram;
            _externalKey = externalKey;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _histogram.Record(_stopwatch.Elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("external_key", _externalKey));
        }
    }
}
