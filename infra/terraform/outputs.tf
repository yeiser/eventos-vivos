output "url" {
  description = "URL pública de la aplicación (frontend + API por el mismo origen)."
  value       = "http://${azurerm_container_group.app.fqdn}:8080"
}

output "fqdn" {
  value = azurerm_container_group.app.fqdn
}

output "acr_login_server" {
  description = "Servidor del ACR donde se publican las imágenes."
  value       = azurerm_container_registry.acr.login_server
}

output "acr_name" {
  value = azurerm_container_registry.acr.name
}

output "resource_group" {
  value = azurerm_resource_group.rg.name
}

output "jwt_secret" {
  description = "Secreto JWT generado (sensible)."
  value       = random_password.jwt.result
  sensitive   = true
}
