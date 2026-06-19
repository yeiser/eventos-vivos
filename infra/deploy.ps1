<#
  Despliegue de EventosVivos en Azure (ambiente de demostración, ACI).

  Orquesta el bootstrap del registry, la publicación de imágenes y el apply del
  grupo de contenedores, resolviendo el orden (el ACI necesita las imágenes ya
  publicadas en el ACR).

  Requisitos: az CLI (con `az login`), Docker y Terraform.

  Uso:
    pwsh ./infra/deploy.ps1                 # despliega
    pwsh ./infra/deploy.ps1 -Destroy        # libera toda la infraestructura

  La autenticación de Terraform usa la sesión de `az` (variables ARM_*). Si tu
  suscripción está en estado "Warned", crea un Service Principal y exporta
  ARM_CLIENT_ID / ARM_CLIENT_SECRET / ARM_TENANT_ID / ARM_SUBSCRIPTION_ID.
#>
param(
  [string]$Tag = "1.0.0",
  [switch]$Destroy
)
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$tf   = Join-Path $PSScriptRoot "terraform"

if (-not $env:ARM_SUBSCRIPTION_ID) { $env:ARM_SUBSCRIPTION_ID = (az account show --query id -o tsv) }

terraform -chdir="$tf" init

if ($Destroy) {
  terraform -chdir="$tf" destroy -auto-approve -var="image_tag=$Tag"
  return
}

# 1) Bootstrap: Resource Group + Container Registry (necesarios para publicar imágenes).
terraform -chdir="$tf" apply -auto-approve `
  -target=azurerm_resource_group.rg -target=azurerm_container_registry.acr -var="image_tag=$Tag"

$acr = terraform -chdir="$tf" output -raw acr_name

# 2) Construir y publicar las imágenes en el ACR (incluida postgres, para no depender de Docker Hub).
az acr login -n $acr
docker build -t "$acr.azurecr.io/eventosvivos-api:$Tag" "$root/backend"
docker build -t "$acr.azurecr.io/eventosvivos-web:$Tag" "$root/frontend/eventos-vivos-web"
docker pull postgres:16-alpine
docker tag  postgres:16-alpine "$acr.azurecr.io/postgres:16-alpine"
docker push "$acr.azurecr.io/eventosvivos-api:$Tag"
docker push "$acr.azurecr.io/eventosvivos-web:$Tag"
docker push "$acr.azurecr.io/postgres:16-alpine"

# 3) Crear el grupo de contenedores (ACI) que consume las imágenes publicadas.
terraform -chdir="$tf" apply -auto-approve -var="image_tag=$Tag"

Write-Host "`nURL pública:" (terraform -chdir="$tf" output -raw url) -ForegroundColor Green
Write-Host "Datos de demo (opcional):"
Write-Host "  pwsh ./scripts/seed-demo.ps1 -ApiBase `"$(terraform -chdir="$tf" output -raw url)/api/v1`""
