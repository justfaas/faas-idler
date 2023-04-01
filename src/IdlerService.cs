using System.Text.Json;
using Json.Patch;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Caching.Memory;

/*
Use a KubeController instead...

~~~
When a new Function is deployed, we start monitoring its metrics.
If a function is deleted, we stop monitoring it.

This is better than constantly scanning for functions
~~~

In reality, we can't watch Functions, we need to watch HPAs instead...
Functions only exists if the CRD is installed, which is not required
if you are using the CLI.

The approach should be listing and watching HPAs that contain the
function label. It doesn't matter who manages it.
*/

internal sealed class IdlerService : BackgroundService
{
    private readonly ILogger logger;
    private readonly Kubernetes client;
    private readonly FunctionStateStore store;

    public IdlerService( ILoggerFactory loggerFactory, Kubernetes kubernetesClient, FunctionStateStore stateStore )
    {
        logger = loggerFactory.CreateLogger<IdlerService>();
        client = kubernetesClient;
        store = stateStore;
    }

    public override Task StartAsync( CancellationToken cancellationToken = default( CancellationToken ) )
    {
        logger.LogInformation( "Started." );

        return base.StartAsync( cancellationToken );
    }

    public override Task StopAsync( CancellationToken cancellationToken = default( CancellationToken ) )
    {
        logger.LogInformation( "Stopped." );

        return base.StopAsync( cancellationToken );
    }

    protected override async Task ExecuteAsync( CancellationToken stoppingToken )
    {
        while ( !stoppingToken.IsCancellationRequested )
        {
            await PrivateExecuteAsync( stoppingToken );

            await Task.Delay( 3000, stoppingToken );
        }
    }

    private async Task PrivateExecuteAsync( CancellationToken cancellationToken )
    {
        /*
        retrieve function scalers with label justfaas.com/scale-to-zero: true
        */

        var hpas = await client.GetHorizontalPodAutoscalersAsync(
            ex => logger.LogError( $"Failed to retrieve scaling information: {ex.Message}" )
        );

        if ( !hpas.Any() )
        {
            // no scalers found;
            return;
        }

        var tasks = hpas.Select( hpa => ExecuteAsync( hpa ) )
            .ToArray();

        await Task.WhenAll( tasks );
    }

    private async Task ExecuteAsync( V2HorizontalPodAutoscaler hpa )
    {
        var ns = hpa.Namespace();
        var name = hpa.Name();
        var metricName = hpa.Spec.Metrics.First().ObjectProperty.Metric.Name;

        var replicas = hpa.Status.DesiredReplicas;

        if ( string.IsNullOrEmpty( metricName ) )
        {
            // unable to find metric name on hpa
            return;
        }

        var metric = await client.GetFunctionMetricsAsync( 
            ns,
            name,
            metricName,
            ex => logger.LogError( $"Failed to retrieve metrics: {ex.Message}" )
        );

        if ( metric == null )
        {
            // metrics not found!
            logger.LogWarning( $"Unable to find '{metricName}' metric value for {ns}.{name}." );
            return;
        }

        if ( metric.Value?.Equals( "0" ) == true )
        {
            await WhenIdleAsync( hpa );
        }
        else
        {
            await WhenActiveAsync( hpa );
        }
    }

    private async Task WhenIdleAsync( V2HorizontalPodAutoscaler hpa )
    {
        var ns = hpa.Namespace();
        var key = hpa.Key();

        var cooldownPeriod = TimeSpanHelper.ParseDuration( 
            hpa.GetAnnotation( "justfaas.com/scale-to-zero-cooldown" )
        ) ?? TimeSpan.FromMinutes( 30 ); // default is 30m

        var state = store.Get( key );

        if ( !state.IdleStarted.HasValue )
        {
            state.IdleStarted = DateTime.UtcNow;
            state.IsScalingUp = false;
        }

        if ( !state.IsIdling( cooldownPeriod ) )
        {
            // cooldown period hasn't yet been reached
            store.Set( key, state );

            return;
        }

        if ( !state.IsScalingDown && ( hpa.Status.DesiredReplicas > 0 ) )
        {
            state.IsScalingDown = true;

            try
            {
                await client.AppsV1.ScaleAsync( ns, hpa.Spec.ScaleTargetRef.Name, 0 );

                logger.LogInformation( $"deployment.apps/{hpa.Spec.ScaleTargetRef.Name} scaled to zero." );
            }
            catch ( Exception ex )
            {
                logger.LogError( $"deployment.apps/{hpa.Spec.ScaleTargetRef.Name} scaling failed. {ex.Message}" );
            }
        }

        if ( state.IsScalingDown && ( hpa.Status.DesiredReplicas == 0 ) )
        {
            state.IsScalingDown = false;
        }

        store.Set( key, state );
    }

    private async Task WhenActiveAsync( V2HorizontalPodAutoscaler hpa )
    {
        var ns = hpa.Namespace();
        var key = hpa.Key();

        var state = store.Get( key );

        if ( state.IdleStarted.HasValue )
        {
            state.IdleStarted = null;
            state.IsScalingDown = false;
        }

        if ( !state.IsScalingUp && ( hpa.Status.DesiredReplicas == 0 ) )
        {
            state.IsScalingUp = true;

            try
            {
                await client.AppsV1.ScaleAsync( ns, hpa.Spec.ScaleTargetRef.Name, hpa.Spec.MinReplicas ?? 1 );

                logger.LogInformation( $"deployment.apps/{hpa.Spec.ScaleTargetRef.Name} restored." );
            }
            catch ( Exception ex )
            {
                logger.LogError( $"deployment.apps/{hpa.Spec.ScaleTargetRef.Name} scaling failed. {ex.Message}" );
            }
        }

        if ( state.IsScalingUp && ( hpa.Status.DesiredReplicas > 0 ) )
        {
            state.IsScalingUp = false;
        }

        store.Set( key, state );
    }
}
