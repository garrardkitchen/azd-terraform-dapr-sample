# dapr-test

Terraform Aspire sample, inner loop development and deploy using Azure Developer CLI (`azd`).

## Infrastructure

Terraform files live under `infra/`.

Key resources:
- Resource group named `dapr-test-<environment_name>`
- Log Analytics workspace `daprtest-logs-<environment_name>`
- Container Apps environment `daprtest-env-<environment_name>`
- Service Bus namespace named `daprtest-<environment_name>-sbns` to comply with Azure naming rules (names must not end with `-sb` or `-mgmt`).

This file will evolve as the project grows.

> [!IMPORTANT]
> AZD environment must be named `dev` for this to work

To deploy:

**Step 1**:

```bash
azd up -e dev
```

**Step 2**:

You must now enable dapr for both apps (see **Issue 1** before).  Use the app name for the dapr id and port 8080 for both.

---

## Issues

| # | Issue                                                                                                                                                                                   | Workaround                                                                                                                                                      | Viable | Why                                                                                                                                                                                    |
|---|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 1 | Dapr sidecar configuration for both the API service and frontend is correctly provisioned by Terraform, but gets removed when the application is deployed through `azd publish`        | Avoid using `azd publish`. Instead, manually build, push, and update container images using the Azure CLI                                                       | ❌      | Creates temporary loss of Dapr functionality between deployments. Applications will fail to publish/consume messages, and state will not persist or be accessible during this window. |
| 2 | Dapr sidecar configuration for both the API service and frontend is correctly provisioned by Terraform, but gets removed when the application is deployed through `azd publish`        | Remove the Container App Environment from code management by deleting the `builder.AddAzureContainerAppEnvironment("dev")` call. Delegate all infrastructure to `azd` | ✅      | -                                                                                                                                                                                      |
| 3 | Referencing a container image name in Terraform before the image has been built causes `terraform apply` to fail                                                                       | Use a placeholder public image initially, add `lifecycle {ignore_changes = [template[0].container[0].image]}` to the Terraform resource, then deploy via `azd publish` | ✅      | -                                                                                                                                                                                      |