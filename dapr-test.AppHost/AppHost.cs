using Aspire.Hosting.Azure;
using CommunityToolkit.Aspire.Hosting.Dapr;
using Azure.Provisioning;

var builder = DistributedApplication.CreateBuilder(args);

var acaEnv = builder
    .AddAzureContainerAppEnvironment("dev")
    .WithDaprSidecar()
    .WithDashboard();

string pubsubName = "pubsub-servicebus";

var serviceBus = builder.AddAzureServiceBus("sbemulatorns")
    .OnResourceReady(async (resource, evt, cancellationToken) =>
{
    var conn = await resource.ConnectionStringExpression.GetValueAsync(cancellationToken);
    var mi = builder.Configuration["principalId"];
    var ns = $"{conn.Split(";")[0].Split("sb://")[1]}.servicebus.windows.net";
    var yaml = string.Empty;

    if (builder.ExecutionContext.IsRunMode)  {
        yaml = $"""
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
    } 
    else
    {
        yaml = $"""
                apiVersion: dapr.io/v1alpha1
                kind: Component
                metadata:
                  name: {pubsubName}
                spec:
                  type: pubsub.azure.servicebus.topics
                  version: v1
                  metadata:
                    - name: namespaceName
                      value: {ns}
                    - name: azureClientId
                      value: {mi}
                    - name: consumerID
                      value: "apiservice-subscription"    
              """;
    }
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

if (builder.ExecutionContext.IsRunMode) {

  topic.AddServiceBusSubscription("apiservice-subscription")
    .WithProperties(subscription =>
    {
        subscription.MaxDeliveryCount = 10;
    });
}

var pubsub2 = builder.AddDaprPubSub(pubsubName, new DaprComponentOptions
{
    LocalPath = Path.Combine(".",".dapr","components","pubsub.yaml")
});

var apiService = builder.AddProject<Projects.dapr_test_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WaitFor(serviceBus)
    .WithReference(serviceBus)
    .WithReference(topic);

if (builder.ExecutionContext.IsRunMode)
{
    apiService.WithDaprSidecar(sidecar => sidecar.WithReference(pubsub2));
}

var frontend = builder.AddProject<Projects.dapr_test_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService)
    // .WaitFor(pubsub)
    .WaitFor(serviceBus)
    .WithReference(serviceBus)
    .WithReference(topic);

if (builder.ExecutionContext.IsRunMode)
{
    frontend.WithDaprSidecar(sidecar => sidecar.WithReference(pubsub2));
}

builder.Build().Run();