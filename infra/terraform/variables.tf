variable "project" {
  description = "Prefijo para nombrar los recursos."
  type        = string
  default     = "eventosvivos"
}

variable "location" {
  description = "Región de Azure (debe soportar Azure Container Instances)."
  type        = string
  default     = "eastus"
}

variable "image_tag" {
  description = "Etiqueta de las imágenes de la API y el frontend en el ACR."
  type        = string
  default     = "1.0.0"
}

variable "postgres_db" {
  description = "Nombre de la base de datos."
  type        = string
  default     = "eventosvivos"
}

variable "postgres_user" {
  description = "Usuario de la base de datos."
  type        = string
  default     = "eventosvivos"
}

# --- Tamaños de los contenedores (mínimos para un ambiente de demostración) ---
variable "api_cpu" {
  type    = number
  default = 0.5
}
variable "api_memory" {
  type    = number
  default = 1.0
}
variable "db_cpu" {
  type    = number
  default = 0.5
}
variable "db_memory" {
  type    = number
  default = 0.5
}
variable "web_cpu" {
  type    = number
  default = 0.5
}
variable "web_memory" {
  type    = number
  default = 0.5
}
