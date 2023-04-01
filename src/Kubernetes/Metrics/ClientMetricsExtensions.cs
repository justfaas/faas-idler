using k8s;
using k8s.Models;

internal static class KubernetesClientMetricsExtensions
{
    public static async Task<V1Beta1MetricValue?> GetFunctionMetricsAsync( this Kubernetes client
        , string ns
        , string name
        , string metricName
        , Action<Exception>? onError = null )
    {
        var url = string.Concat( client.BaseUri
            , $"apis/custom.metrics.k8s.io/v1beta1/namespaces/{ns}/functions.justfaas.com/{name}/{metricName}" );

        try
        {
            var httpRequest = new HttpRequestMessage( HttpMethod.Get, url );

            if ( client.Credentials != null )
            {
                await client.Credentials.ProcessHttpRequestAsync( httpRequest, CancellationToken.None );
            }

            var response = await client.HttpClient.SendAsync( httpRequest, HttpCompletionOption.ResponseHeadersRead );

            if ( !response.IsSuccessStatusCode )
            {
                var content = await response.Content.ReadAsStringAsync();

                onError?.Invoke( new Exception( $"Received a {(int)response.StatusCode}." ) );

                return ( null );
            }

            var metrics = await response.Content.ReadFromJsonAsync<V1Beta1MetricValueList>();

            return metrics?.Items?.FirstOrDefault();
        }
        catch ( Exception ex )
        {
            onError?.Invoke( ex );

            return ( null );
        }
    }
}
