locals {
  rg_name              = "${var.resource_group_name_prefix}-${var.environment_name}"
  # Service Bus namespace names cannot end with "-sb" or "-mgmt".
  # Use an environment-scoped name with an allowed suffix.
  servicebus_namespace = "${var.base_name}-${var.environment_name}-sbns"
  servicebus_topic     = "${var.servicebus_topic_base_name}-${var.environment_name}"
}

resource "azurerm_resource_group" "rg" {
  name     = local.rg_name
  location = var.location

  tags = var.tags
}

resource "azurerm_log_analytics_workspace" "law" {
  name                = "${var.base_name}-logs-${var.environment_name}"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  sku                 = "PerGB2018"
  retention_in_days   = 30

  tags = var.tags
}

# User-assigned managed identity used by application services
resource "azurerm_user_assigned_identity" "apps_mi" {
  name                = "${var.base_name}-${var.environment_name}-apps-mi"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name

  tags = var.tags
}

# User-assigned managed identity used by the apiservice Container App
resource "azurerm_user_assigned_identity" "apiservice_mi" {
  name                = "${var.base_name}-${var.environment_name}-apiservice-mi"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name

  tags = var.tags
}

# User-assigned managed identity used by the webfrontend Container App
resource "azurerm_user_assigned_identity" "webfrontend_mi" {
  name                = "${var.base_name}-${var.environment_name}-webfrontend-mi"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name

  tags = var.tags
}

resource "azurerm_container_app_environment" "aca_env" {
  name                = "${var.base_name}-env-${var.environment_name}"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name

  log_analytics_workspace_id = azurerm_log_analytics_workspace.law.id

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.apps_mi.id]
  }

  tags = var.tags
}

# Azure Container Registry used for application images
resource "azurerm_container_registry" "acr" {
  name                = "${var.base_name}${var.environment_name}acr"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  sku                 = "Basic"
  admin_enabled       = false

  tags = var.tags
}

# Allow the apps managed identity to pull images from ACR
resource "azurerm_role_assignment" "apps_mi_acr_pull" {
  scope                = azurerm_container_registry.acr.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_user_assigned_identity.apps_mi.principal_id
}

# Allow the apiservice managed identity to pull images from ACR
resource "azurerm_role_assignment" "apiservice_mi_acr_pull" {
  scope                = azurerm_container_registry.acr.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_user_assigned_identity.apiservice_mi.principal_id
}

# Allow the webfrontend managed identity to pull images from ACR
resource "azurerm_role_assignment" "webfrontend_mi_acr_pull" {
  scope                = azurerm_container_registry.acr.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_user_assigned_identity.webfrontend_mi.principal_id
}

resource "azurerm_servicebus_namespace" "sb" {
  name                = local.servicebus_namespace
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  sku                 = "Standard"

  tags = var.tags
}

resource "azurerm_servicebus_topic" "pubsub" {
  name         = local.servicebus_topic
  namespace_id = azurerm_servicebus_namespace.sb.id

  partitioning_enabled = true
}

resource "azurerm_servicebus_subscription" "apiservice" {
  name               = "apiservice-${var.environment_name}"
  topic_id           = azurerm_servicebus_topic.pubsub.id
  max_delivery_count = 10
}

# Allow the apps managed identity to send/receive on the Service Bus namespace
resource "azurerm_role_assignment" "apps_mi_sb_sender" {
  scope                = azurerm_servicebus_namespace.sb.id
  role_definition_name = "Azure Service Bus Data Sender"
  principal_id         = azurerm_user_assigned_identity.apps_mi.principal_id
}

resource "azurerm_role_assignment" "apps_mi_sb_receiver" {
  scope                = azurerm_servicebus_namespace.sb.id
  role_definition_name = "Azure Service Bus Data Receiver"
  principal_id         = azurerm_user_assigned_identity.apps_mi.principal_id
}

# Dapr pubsub component for Azure Container Apps using Service Bus topics
# Implemented via azapi against Microsoft.App/managedEnvironments/daprComponents
resource "azapi_resource" "dapr_pubsub_servicebus" {
  name      = "pubsub-servicebus"
  type      = "Microsoft.App/managedEnvironments/daprComponents@2023-05-01"
  parent_id = azurerm_container_app_environment.aca_env.id

  # azapi 2.x expects an HCL object for `body`, not a JSON-encoded string.
  body = {
    properties = {
      componentType = "pubsub.azure.servicebus.topics"
      version       = "v1"
      metadata = [
        {
          name  = "namespaceName"
          value = azurerm_servicebus_namespace.sb.name
        },
        {
          name  = "consumerID"
          value = "apiservice-subscription"
        },
        {
          name  = "disableEntityManagement"
          value = "true"
        }
      ]
      scopes = [
        "apiservice",
        "webfrontend"
      ]
    }
  }
}

# Container App for the API service
resource "azurerm_container_app" "apiservice" {
  name                         = "apiservice"
  resource_group_name          = azurerm_resource_group.rg.name
  container_app_environment_id = azurerm_container_app_environment.aca_env.id

  revision_mode = "Single"

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.apiservice_mi.id, azurerm_user_assigned_identity.apps_mi.id]
  }

  registry {
    server   = azurerm_container_registry.acr.login_server
    identity = azurerm_user_assigned_identity.apiservice_mi.id
  }

  template {
    container {
      name   = "apiservice"
      image  = "mcr.microsoft.com/mcr/hello-world:latest"
      cpu    = 0.5
      memory = "1Gi"

      env {
        name  = "ASPNETCORE_URLS"
        value = "http://0.0.0.0:8080"
      }
    }

  }

  lifecycle {
    ignore_changes = [ 
      template[0].container[0].image
     ]
  }

  ingress {
    external_enabled = true
    target_port      = 8080
    transport        = "auto"

    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }
}

# Container App for the webfrontend frontend
resource "azurerm_container_app" "webfrontend" {
  name                         = "webfrontend"
  resource_group_name          = azurerm_resource_group.rg.name
  container_app_environment_id = azurerm_container_app_environment.aca_env.id

  revision_mode = "Single"

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.webfrontend_mi.id, azurerm_user_assigned_identity.apps_mi.id]
  }

  registry {
    server   = azurerm_container_registry.acr.login_server
    identity = azurerm_user_assigned_identity.webfrontend_mi.id
  }

  template {
    container {
      name   = "webfrontend"
      image  = "mcr.microsoft.com/mcr/hello-world:latest"
      cpu    = 0.5
      memory = "1Gi"

      env {
        name  = "ASPNETCORE_URLS"
        value = "http://0.0.0.0:8080"
      }
    }

  }

  lifecycle {
    ignore_changes = [ 
      template[0].container[0].image
     ]
  }

  ingress {
    external_enabled = true
    target_port      = 8080
    transport        = "auto"

    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }
}
