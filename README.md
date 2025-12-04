# dapr-test

Infrastructure and Aspire app sample using Azure Developer CLI (`azd`).

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

You must now enable dapr for both apps.  Use the app name for the dapr id and port 8080 for both.
