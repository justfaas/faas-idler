using k8s;

// builder
var builder = Host.CreateApplicationBuilder();

// logging
builder.Logging.ClearProviders()
    .AddSimpleConsole( options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
    } );

// services
builder.Services.AddSingleton<Kubernetes>( provider =>
{
    var config = KubernetesClientConfiguration.IsInCluster()
        ? KubernetesClientConfiguration.InClusterConfig()
        : KubernetesClientConfiguration.BuildConfigFromConfigFile();

    return new Kubernetes( config );
} );

builder.Services.AddHostedService<IdlerService>()
    .AddMemoryCache()
    .AddTransient<FunctionStateStore>();

// app
var app = builder.Build();

app.Run();
