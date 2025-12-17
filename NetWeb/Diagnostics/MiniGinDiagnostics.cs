using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace NetWeb.Diagnostics;

public static class NetWebDiagnostics
{
    public const string ActivitySourceName = "NetWeb";
    public const string MeterName = "NetWeb";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    public static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> HttpServerRequestCount = Meter.CreateCounter<long>(
        name: "http.server.request.count",
        unit: "{request}",
        description: "Number of HTTP requests processed by the server");

    public static readonly Histogram<double> HttpServerRequestDuration = Meter.CreateHistogram<double>(
        name: "http.server.request.duration",
        unit: "s",
        description: "Duration of HTTP requests processed by the server");

    public static readonly UpDownCounter<long> HttpServerActiveRequests = Meter.CreateUpDownCounter<long>(
        name: "http.server.active_requests",
        unit: "{request}",
        description: "Number of active HTTP requests being processed");

    public static readonly Counter<long> ExceptionsCount = Meter.CreateCounter<long>(
        name: "exceptions.count",
        unit: "{exception}",
        description: "Number of exceptions thrown while processing requests");
}
