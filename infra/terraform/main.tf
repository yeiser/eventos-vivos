# EventosVivos — infraestructura MÍNIMA para un ambiente de demostración.
#
# Un único grupo de contenedores de Azure Container Instances (ACI) con 3 contenedores
# (PostgreSQL + API + frontend) que comparten red (se comunican por localhost). El frontend
# (Nginx) es el único expuesto públicamente y hace de proxy de la API → sin CORS, sin VM,
# sin base de datos gestionada, sin Key Vault. Los secretos se generan aleatoriamente.
#
# Recursos: Resource Group + Container Registry (Basic) + Container Group.

resource "random_string" "suffix" {
  length  = 6
  special = false
  upper   = false
}

resource "random_password" "pg" {
  length  = 24
  special = false # alfanumérico: seguro dentro de la cadena de conexión
}

resource "random_password" "jwt" {
  length  = 48
  special = false
}

locals {
  acr_name  = "${var.project}acr${random_string.suffix.result}" # único global, minúsculas
  dns_label = "${var.project}-${random_string.suffix.result}"
  # FQDN determinista de ACI: <label>.<region>.azurecontainer.io
  fqdn = "${local.dns_label}.${var.location}.azurecontainer.io"
}

resource "azurerm_resource_group" "rg" {
  name     = "${var.project}-rg"
  location = var.location
}

resource "azurerm_container_registry" "acr" {
  name                = local.acr_name
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  sku                 = "Basic"
  admin_enabled       = true # credenciales de admin para que ACI haga pull (mínimo)
}

resource "azurerm_container_group" "app" {
  name                = "${var.project}-aci"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  os_type             = "Linux"
  ip_address_type     = "Public"
  dns_name_label      = local.dns_label
  restart_policy      = "OnFailure" # la API reintenta hasta que PostgreSQL esté listo

  image_registry_credential {
    server   = azurerm_container_registry.acr.login_server
    username = azurerm_container_registry.acr.admin_username
    password = azurerm_container_registry.acr.admin_password
  }

  # Solo el frontend (puerto 8080, Nginx no-root) queda expuesto a Internet.
  exposed_port {
    port     = 8080
    protocol = "TCP"
  }

  # --- PostgreSQL (efímero: los datos se regeneran con el seed al arrancar) ---
  # Se sirve desde el ACR (no desde Docker Hub) para evitar su límite de pulls anónimos.
  # Sin bloque `ports`: es interno, los demás contenedores lo alcanzan por localhost:5432.
  container {
    name   = "db"
    image  = "${azurerm_container_registry.acr.login_server}/postgres:16-alpine"
    cpu    = var.db_cpu
    memory = var.db_memory

    environment_variables = {
      POSTGRES_DB   = var.postgres_db
      POSTGRES_USER = var.postgres_user
    }
    secure_environment_variables = {
      POSTGRES_PASSWORD = random_password.pg.result
    }
  }

  # --- API .NET (migra + siembra al arrancar) ---
  # Sin bloque `ports`: es interno, el frontend la alcanza por localhost:8080.
  container {
    name   = "api"
    image  = "${azurerm_container_registry.acr.login_server}/eventosvivos-api:${var.image_tag}"
    cpu    = var.api_cpu
    memory = var.api_memory

    # En el grupo ACI los contenedores comparten red: la API escucha en 5000 para no
    # colisionar con Nginx (8080). El frontend la alcanza por localhost:5000.
    environment_variables = {
      ASPNETCORE_ENVIRONMENT = "Production"
      ASPNETCORE_URLS        = "http://+:5000"
      Database__AutoMigrate  = "true"
      Cors__Origins__0       = "http://${local.fqdn}:8080"
    }
    secure_environment_variables = {
      ConnectionStrings__Postgres = "Host=localhost;Port=5432;Database=${var.postgres_db};Username=${var.postgres_user};Password=${random_password.pg.result}"
      Jwt__SecretKey              = random_password.jwt.result
    }
  }

  # --- Frontend (Nginx: estáticos + proxy /api → localhost:8080) ---
  container {
    name   = "web"
    image  = "${azurerm_container_registry.acr.login_server}/eventosvivos-web:${var.image_tag}"
    cpu    = var.web_cpu
    memory = var.web_memory

    ports {
      port     = 8080
      protocol = "TCP"
    }

    environment_variables = {
      API_UPSTREAM = "localhost:5000"
    }
  }
}
