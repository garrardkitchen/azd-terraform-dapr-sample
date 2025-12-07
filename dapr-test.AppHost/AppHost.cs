using CommunityToolkit.Aspire.Hosting.Dapr;

IResourceBuilder<IDaprComponentResource> pubsub;
string pubsubName = "pubsub-servicebus";
var builder = DistributedApplication.CreateBuilder(args);

// todo: If I comment this line out, it will defer the services' host infrastructure to azd
// builder.AddAzureContainerAppEnvironment("dev").WithDaprSidecar().WithDashboard();

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
    .WithDaprSidecar(sidecar => sidecar.WithReference(pubsub))
    .WithReference(serviceBus)
    .WithReference(topic)
    .WaitFor(serviceBus);

var frontend = builder.AddProject<Projects.dapr_test_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithDaprSidecar(sidecar => sidecar.WithReference(pubsub))
    .WithReference(apiService)
    .WithReference(serviceBus)
    .WithReference(topic)
    .WaitFor(apiService)
    .WaitFor(serviceBus);

builder.Build().Run();