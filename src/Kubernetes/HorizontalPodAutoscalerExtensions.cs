using k8s.Models;

internal static class V2HorizontalPodAutoscalerStoreExtensions
{
    public static string Key( this V2HorizontalPodAutoscaler hpa )
        => string.Concat( hpa.Namespace(), "/", hpa.Name() );
}
