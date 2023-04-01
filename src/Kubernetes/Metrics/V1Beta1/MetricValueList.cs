public sealed class V1Beta1MetricValueList
{
    public string? Kind { get; set; }
    public string? ApiVersion { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
    public IEnumerable<V1Beta1MetricValue>? Items { get; set; }
}
