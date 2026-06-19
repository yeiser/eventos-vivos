terraform {
  required_version = ">= 1.5"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.6"
    }
  }
}

# La suscripción se toma de la variable ARM_SUBSCRIPTION_ID o de la sesión de `az login`.
provider "azurerm" {
  features {}
}
