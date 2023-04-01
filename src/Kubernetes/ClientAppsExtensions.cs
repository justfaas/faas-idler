using System.Text.Json;
using Json.Patch;
using k8s;
using k8s.Models;

internal static class KubernetesClientAppsExtensions
{
    /// <summary>
    /// Scales the number of replicas for a given deployment
    /// </summary>
    public static async Task ScaleAsync( this IAppsV1Operations appsV1
        , string ns
        , string name
        , int replicas )
    {
        var deployment = await appsV1.ReadNamespacedDeploymentAsync(
            namespaceParameter: ns,
            name: name
        );

        var patch = deployment.CreatePatch( x =>
        {
            x.Spec.Replicas = replicas;
        } );

        await appsV1.PatchNamespacedDeploymentAsync(
            namespaceParameter: ns,
            name: name,
            body: new V1Patch( patch, V1Patch.PatchType.JsonPatch )
        );
    }
}
