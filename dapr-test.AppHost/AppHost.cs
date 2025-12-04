using Aspire.Hosting.Azure;
using CommunityToolkit.Aspire.Hosting.Dapr;
using Azure.Provisioning;

var builder = DistributedApplication.CreateBuilder(args);

//
var acaEnv = builder
    .AddAzureContainerAppEnvironment("dev")
    .WithDaprSidecar();


// builder.AddDapr();

// var pubsub = builder.AddDaprPubSub("pubsub");


var serviceBus = builder.AddAzureServiceBus("sbemulatorns")
    .OnResourceReady(async (resource, evt, cancellationToken) =>
{
    var conn = await resource.ConnectionStringExpression.GetValueAsync(cancellationToken);
    // 
    var yaml = $"""
                apiVersion: dapr.io/v1alpha1
                kind: Component
                metadata:
                  name: pubsub2
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
    var filePath = Path.Combine(".",".dapr","components","pubsub.yaml");
    Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
    await File.WriteAllTextAsync(filePath, yaml, cancellationToken);
});

if (builder.ExecutionContext.IsRunMode)
{
    serviceBus
        .RunAsEmulator(_ => { _.WithLifetime(ContainerLifetime.Persistent); });
}

var topic = serviceBus.AddServiceBusTopic("topic");
topic.AddServiceBusSubscription("apiservice-subscription")
    .WithProperties(subscription =>
    {
        subscription.MaxDeliveryCount = 10;
    });

var pubsub2 = builder.AddDaprPubSub("pubsub2", new DaprComponentOptions
{
    LocalPath = Path.Combine(".",".dapr","components","pubsub2.yaml")
});

var apiService = builder.AddProject<Projects.dapr_test_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WaitFor(serviceBus)
    .WithReference(serviceBus)
    .WithReference(topic)
    // .WithDaprSidecar( sidecar => sidecar.WithReference(pubsub).WithReference(pubsub2));
    .WithDaprSidecar( sidecar => sidecar.WithReference(pubsub2));

builder.AddProject<Projects.dapr_test_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService)
    // .WaitFor(pubsub)
    .WaitFor(serviceBus)
    .WithReference(serviceBus)
    .WithReference(topic)
    // .WithDaprSidecar( sidecar => sidecar.WithReference(pubsub).WithReference(pubsub2));
    .WithDaprSidecar( sidecar => sidecar.WithReference(pubsub2));

builder.Build().Run();