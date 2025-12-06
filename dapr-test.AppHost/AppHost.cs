using CommunityToolkit.Aspire.Hosting.Dapr;

IResourceBuilder<IDaprComponentResource> pubsub;
string pubsubName = "pubsub-servicebus";
var builder = DistributedApplication.CreateBuilder(args);
builder.AddAzureContainerAppEnvironment("dev").WithDaprSidecar().WithDashboard();
var serviceBus = builder.AddAzureServiceBus("sbemulatorns");
var topic = serviceBus.AddServiceBusTopic("topic");

if (builder.ExecutionContext.IsRunMode) {
    
    serviceBus
        .RunAsEmulator(_ => { _.WithLifetime(ContainerLifetime.Persistent); });

    serviceBus.OnResourceReady(async (resource, evt, cancellationToken) =>
    {
        var conn = await resource.ConnectionStringExpression.GetValueAsync(cancellationToken);
        var mi = builder.Configuration["principalId"];
        var ns = $"{conn.Split(";")[0].Split("sb://")[1]}.servicebus.windows.net";
        var yaml = $"""
                  apiVersion: dapr.io/v1alpha1
                  kind: Component
                  metadata:
                    name: {pubsubName}
                  spec:
                    type: pubsub.azure.servicebus.topics
                    version: v1
                    metadata:
                      - name: connectionString
                        value: {conn}
                      - name: disableEntityManagement
                        value: "true"
                      - name: consumerID
                        value: "apiservice-subscription"    
                """;
        var filePath = Path.Combine(".", ".dapr", "components", "pubsub.yaml");
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await File.WriteAllTextAsync(filePath, yaml, cancellationToken);
    });

    topic.AddServiceBusSubscription("apiservice-subscription")
        .WithProperties(subscription =>
        {
            subscription.MaxDeliveryCount = 10;
        });
  
    pubsub = builder.AddDaprPubSub(pubsubName, new DaprComponentOptions
    {
      LocalPath = Path.Combine(".",".dapr","components","pubsub.yaml")
    });
}
else
{
    pubsub = builder.AddDaprPubSub(pubsubName);
}

var apiService = builder.AddProject<Projects.dapr_test_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WaitFor(serviceBus)
    .WithReference(serviceBus)
    .WithReference(topic);

var frontend = builder.AddProject<Projects.dapr_test_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WaitFor(apiService)
    .WaitFor(serviceBus)
    .WithReference(apiService)
    .WithReference(serviceBus)
    .WithReference(topic);

if (builder.ExecutionContext.IsRunMode)
{
    frontend.WithDaprSidecar(sidecar => sidecar.WithReference(pubsub));
    apiService.WithDaprSidecar(sidecar => sidecar.WithReference(pubsub));
}

builder.Build().Run();