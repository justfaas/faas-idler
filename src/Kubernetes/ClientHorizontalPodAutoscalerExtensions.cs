using k8s;
using k8s.Models;

internal static class KubernetesClientHorizontalPodAutoscalerExtensions
{
    public static async Task<IEnumerable<V2HorizontalPodAutoscaler>> GetHorizontalPodAutoscalersAsync( this IKubernetes client
        , Action<Exception>? onError = null )
    {
        try
        {
            var list = await client.AutoscalingV2.ListHorizontalPodAutoscalerForAllNamespacesAsync(
                labelSelector: "justfaas.com/name,justfaas.com/scale-to-zero=true"
            );

            return list.Items;
        }
        catch ( Exception ex )
        {
            onError?.Invoke( ex );

            return Enumerable.Empty<V2HorizontalPodAutoscaler>();
        }
    }
}
