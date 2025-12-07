# dapr-test

Terraform Aspire (**w/ dapr**) sample, inner loop development and deploy using Azure Developer CLI (`azd`).

> [!IMPORTANT]
> For AZD, Terraform, Dapr, and Aspire to work together, remove `builder.AddAzureContainerAppEnvironment()` from your application code to let AZD manage the Container App infrastructure. Use the AzAPI provider in Terraform to enable the Aspire Dashboard.


## Infrastructure

Terraform files live under `infra/`.

## Key Resources

This infrastructure provisions the following Azure resources:

### Core Infrastructure
- **Resource Group**: `${base_name}-${environment_name}` - Contains all project resources
- **Log Analytics Workspace**: `${base_name}-logs-${environment_name}` - Centralized logging with 30-day retention
- **Container App Environment**: `${base_name}-env-${environment_name}` - Hosts all Container Apps with Dapr enabled
- **Container Registry**: `${base_name}${environment_name}acr` - Private registry for application container images

### Managed Identities
- **Apps Identity**: `${base_name}-${environment_name}-apps-mi` - Shared identity for Dapr components and Service Bus access
- **API Service Identity**: `${base_name}-${environment_name}-apiservice-mi` - Dedicated identity for API service with ACR pull permissions
- **Web Frontend Identity**: `${base_name}-${environment_name}-webfrontend-mi` - Dedicated identity for web frontend with ACR pull permissions

### Container Apps
- **apiservice**: API service with Dapr sidecar (app-id: `apiservice`, port: 8080)
  - External ingress enabled
  - Uses user-assigned identities for ACR access and Service Bus operations
  - Dapr-enabled for pub/sub and service invocation
  
- **webfrontend**: Web frontend with Dapr sidecar (app-id: `webfrontend`, port: 8080)
  - External ingress enabled
  - Uses user-assigned identities for ACR access and Service Bus operations
  - Dapr-enabled for pub/sub and service invocation

### Messaging Infrastructure
- **Service Bus Namespace**: `${base_name}-${environment_name}-sbns` (Standard SKU)
- **Service Bus Topic**: `${servicebus_topic_base_name}-${environment_name}` - Pub/sub messaging with partitioning enabled
- **Service Bus Subscription**: `apiservice-${environment_name}` - Subscription for API service (max 10 delivery attempts)

### Dapr Components
- **pubsub-servicebus**: Dapr pub/sub component using Azure Service Bus Topics
  - Configured with managed identity authentication
  - Scoped to both `apiservice` and `webfrontend`
  - Consumer ID: `apiservice-subscription`

### Aspire Dashboard
- **aspire-dashboard**: .NET Aspire Dashboard component for observability and diagnostics
  - Deployed using AzAPI provider (preview API version `2025-10-02-preview`)
  - Provides telemetry visualization and distributed tracing

### RBAC Assignments
- **AcrPull**: Granted to all managed identities for pulling container images
- **Azure Service Bus Data Owner**: Granted to apps identity for full Service Bus access
- **Azure Service Bus Data Sender**: Granted to apps identity for publishing messages
- **Azure Service Bus Data Receiver**: Granted to apps identity for consuming messages

### Important Configuration Notes
- Container images use `lifecycle.ignore_changes` to prevent Terraform from overwriting images deployed by Azure Developer CLI
- Initial placeholder image: `mcr.microsoft.com/mcr/hello-world:latest`
- Both Container Apps listen on port 8080 with HTTP protocol
- Service Bus namespace naming avoids reserved suffixes (`-sb`, `-mgmt`)

---


To deploy:

**Step 1**:

```bash
azd up -e dev
```

**Step 2**:

- _If, you are managing infra via AppHost (see **Issue 1**), you've now have to_:
  - Enable dapr for both apps. Use the app name for the dapr id and port 8080 for both.

- _If, you are managing infra via AppHost (see **Issue 2**), you're done!_:

---

## Issues

| # | Issue                                                                                                                                                                           | Workaround                                                                                                                                                             | Viable | Why                                                                                                                                                                                    |
|---|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 1 | Dapr sidecar configuration for both the API service and frontend is correctly provisioned by Terraform, but gets removed when the application is deployed through `azd publish` | Avoid using `azd publish`. Instead, manually build, push, and update container images using the Azure CLI                                                              | ❌      | Creates temporary loss of Dapr functionality between deployments. Applications will fail to publish/consume messages, and state will not persist or be accessible during this window. |
| 2 | Dapr sidecar configuration for both the API service and frontend is correctly provisioned by Terraform, but gets removed when the application is deployed through `azd publish` | Remove the Container App Environment from code management by deleting the `builder.AddAzureContainerAppEnvironment("dev")` call. Delegate all infrastructure to `azd`  | ✅      | -                                                                                                                                                                                      |
| 3 | Referencing a container image name in Terraform before the image has been built causes `terraform apply` to fail                                                                | Use a placeholder public image initially, add `lifecycle {ignore_changes = [template[0].container[0].image]}` to the Terraform resource, then deploy via `azd publish` | ✅      | -                                                                                                                                                                                      |
| 4 | Aspire Dashboard is not an option if in azurerm_container_app_environment so the Aspire dashboard will not be enabled                                                           | Use the azapi profider and the azapi_resource resource and set the property `{ componentType = "AspireDashboard"}`                                                     | ✅      | -                                                                                                                                                                                      |