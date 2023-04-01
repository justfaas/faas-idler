using System.Text.Json;
using Json.Patch;
using k8s;

public static class KubernetesObjectModelPatchExtensions
{
    public static JsonPatch? CreatePatch<T>( this T source, Action<T> modify ) where T : IKubernetesObject
    {
        var original = JsonSerializer.SerializeToDocument( source );

        modify.Invoke( source );

        var modified = JsonSerializer.SerializeToDocument( source );

        return original.CreatePatch( modified );
    }
}
