using System.Diagnostics.Metrics;
using Notrelix.Infrastructure.Observability.Metrics;

namespace Notrelix.Infrastructure.Tests.Observability;

public class MetricsServiceTests
{
    [Fact]
    public void Outbox_gauges_expose_spec_instrument_names()
    {
        using var service = new MetricsService();
        using var listener = new MeterListener();
        var observed = new Dictionary<string, double>();

        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == MetricsService.MeterName &&
                instrument.Name is "outbox_pending_count" or "outbox_failed_count"
                    or "outbox_dead_letter_count" or "outbox_oldest_age_ms")
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<int>((instrument, measurement, _, _) =>
            observed[instrument.Name] = measurement);
        listener.SetMeasurementEventCallback<double>((instrument, measurement, _, _) =>
            observed[instrument.Name] = measurement);
        listener.Start();

        service.UpdateOutboxCounts(pending: 3, failed: 2, deadLetter: 1, oldestAgeMs: 1250.5);
        listener.RecordObservableInstruments();

        observed.Should().ContainKeys(
            "outbox_pending_count", "outbox_failed_count",
            "outbox_dead_letter_count", "outbox_oldest_age_ms");
        observed["outbox_pending_count"].Should().Be(3);
        observed["outbox_failed_count"].Should().Be(2);
        observed["outbox_dead_letter_count"].Should().Be(1);
        observed["outbox_oldest_age_ms"].Should().Be(1250.5);

        service.UpdateOutboxCounts(pending: 0, failed: 0, deadLetter: 0, oldestAgeMs: null);
        listener.RecordObservableInstruments();
        observed["outbox_pending_count"].Should().Be(0);
        observed["outbox_oldest_age_ms"].Should().Be(0);
    }

    [Fact]
    public void Histogram_and_counter_instruments_use_spec_names()
    {
        using var service = new MetricsService();
        var instrumentNames = new List<string>();
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == MetricsService.MeterName)
                {
                    instrumentNames.Add(instrument.Name);
                    meterListener.EnableMeasurementEvents(instrument);
                }
            },
        };
        listener.Start();

        instrumentNames.Should().Contain(new[]
        {
            "outbox_dispatched_count",
            "publish_failures",
            "inbox_duplicates",
            "commit_to_claim_ms",
            "commit_to_publish_ms",
            "outbox_publish_duration_ms",
            "realtime_publish_ms",
        });
    }
}
