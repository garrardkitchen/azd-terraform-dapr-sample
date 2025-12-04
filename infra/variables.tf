variable "environment_name" {
  description = "Short name of the azd environment (e.g. dev, test, prod)."
  type        = string
}

variable "location" {
  description = "Azure location for all resources."
  type        = string
}

variable "resource_group_name_prefix" {
  description = "Prefix for the resource group name (project-level)."
  type        = string
  default     = "dapr-test"
}

variable "base_name" {
  description = "Base name used for shared resources such as the Service Bus namespace."
  type        = string
  default     = "daprtest"
}

variable "servicebus_topic_base_name" {
  description = "Base topic name; full topic name will be of the form '<base>-<environment_name>'."
  type        = string
  default     = "pubsub"
}

variable "tags" {
  description = "Common tags applied to all resources."
  type        = map(string)
  default     = {}
}
