output "resource_group_name" {
  description = "Name of the resource group created for this environment."
  value       = azurerm_resource_group.rg.name
}

output "container_app_environment_name" {
  description = "Name of the Azure Container Apps environment."
  value       = azurerm_container_app_environment.aca_env.name
}

output "DEV_AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN" {
  description = "Default domain for the Azure Container Apps environment."
  value       = azurerm_container_app_environment.aca_env.default_domain
}

output "DEV_AZURE_CONTAINER_APPS_ENVIRONMENT_ID" {
  description = "Resource ID of the Azure Container Apps environment."
  value       = azurerm_container_app_environment.aca_env.id
}

output "servicebus_namespace_name" {
  description = "Name of the Azure Service Bus namespace used for Dapr pub/sub."
  value       = azurerm_servicebus_namespace.sb.name
}

output "servicebus_topic_name" {
  description = "Name of the Azure Service Bus topic used for Dapr pub/sub in this environment."
  value       = azurerm_servicebus_topic.pubsub.name
}

output "SBEMULATORNS_SERVICEBUSENDPOINT" {
  description = "Service Bus endpoint URL for the namespace."
  value       = azurerm_servicebus_namespace.sb.endpoint
}

output "container_registry_login_server" {
  description = "Login server of the Azure Container Registry used for app images."
  value       = azurerm_container_registry.acr.login_server
}

output "DEV_AZURE_CONTAINER_REGISTRY_ENDPOINT" {
  description = "Endpoint (login server URL) of the Azure Container Registry."
  value       = azurerm_container_registry.acr.login_server
}

output "DEV_AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID" {
  description = "Resource ID of the managed identity used to access the Azure Container Registry."
  value       = azurerm_user_assigned_identity.apps_mi.id
}

output "apps_managed_identity_client_id" {
  description = "Client ID of the user-assigned managed identity used by application services."
  value       = azurerm_user_assigned_identity.apps_mi.client_id
}

output "apps_managed_identity_resource_id" {
  description = "Resource ID of the user-assigned managed identity used by application services."
  value       = azurerm_user_assigned_identity.apps_mi.id
}

# User-assigned identities for Container Apps (apiservice and web)
output "APISERVICE_IDENTITY_CLIENTID" {
  description = "Client ID of the user-assigned managed identity for the apiservice Container App."
  value       = azurerm_user_assigned_identity.apiservice_mi.client_id
}

output "APISERVICE_IDENTITY_ID" {
  description = "Resource ID of the user-assigned managed identity used by the apiservice Container App."
  value       = azurerm_user_assigned_identity.apiservice_mi.id
}

output "WEBFRONTEND_IDENTITY_CLIENTID" {
  description = "Client ID of the user-assigned managed identity for the web Container App."
  value       = azurerm_user_assigned_identity.webfrontend_mi.client_id
}

output "WEBFRONTEND_IDENTITY_ID" {
  description = "Resource ID of the user-assigned managed identity used by the web Container App."
  value       = azurerm_user_assigned_identity.webfrontend_mi.id
}

output "AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID" {
  description = "Resource ID of the managed identity used to access the Azure Container Registry."
  value       = azurerm_user_assigned_identity.apps_mi.id
}

output "AZURE_CONTAINER_APPS_ENVIRONMENT_ID" {
  description = "Resource ID of the Azure Container Apps environment."
  value       = azurerm_container_app_environment.aca_env.id
}

output "AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN" {
  description = "Default domain for the Azure Container Apps environment."
  value       = azurerm_container_app_environment.aca_env.default_domain
}

output "MANAGED_IDENTITY_CLIENT_ID" {
  description = "Client ID of the user-assigned managed identity used by application services."
  value       = azurerm_user_assigned_identity.apps_mi.client_id
}