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

| # | Issue                                                                                                                                                                              | Work around                                                                                              | Viable | Why                                                                                                                                           |
|---|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------|---|-----------------------------------------------------------------------------------------------------------------------------------------------|
| 1 | The dapr sidecar configuration for both the apiservice and frontend are correctly configured by Terraform. However, when an application is deployed, it removes this configuration | Do not use `azd publish`. Instead, build, push and update image using az cli instead                                                                                              | ❌ | temporary loss of functionality between applications. Will fail to publish and consume messages, state will not be persist and be accessible. |
| 2 | Setting the image name in Terraform before it has been built, will make the `terraform apply` fail.                                                                                | Use a standard public image, `lifecycle {ignore_changes}` for the image, then let the azd publish set it                                                                           | ✅ | -                                                                                                                                             |
