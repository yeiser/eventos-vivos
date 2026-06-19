<#
  Genera datos de demostración realistas llamando a la API de EventosVivos.
  Crea eventos en los 3 venues con reservas en distintos estados (pendientes,
  confirmadas con código, canceladas), lo que también puebla el audit trail.

  Uso:  powershell -ExecutionPolicy Bypass -File scripts/seed-demo.ps1 [-ApiBase http://localhost:5080/api/v1]
#>
param(
  [string]$ApiBase = "http://localhost:5080/api/v1",
  [string]$Usuario = "admin",
  [string]$Password = "Admin123!"
)
$ErrorActionPreference = "Stop"

$login = Invoke-RestMethod "$ApiBase/auth/login" -Method Post -ContentType application/json `
  -Body (@{ nombreUsuario = $Usuario; password = $Password } | ConvertTo-Json)
$h = @{ Authorization = "Bearer $($login.token)" }

function NuevoEvento($t, $d, $v, $c, $p, $tipo, $ini, $fin) {
  $body = @{ titulo = $t; descripcion = $d; venueId = $v; capacidadMaxima = $c; precio = $p; tipo = $tipo; fechaInicio = $ini; fechaFin = $fin } | ConvertTo-Json
  Invoke-RestMethod "$ApiBase/eventos" -Method Post -ContentType application/json -Headers $h -Body $body
}
function NuevaReserva($id, $cant, $nom, $mail) {
  $body = @{ cantidad = $cant; nombreComprador = $nom; emailComprador = $mail } | ConvertTo-Json
  Invoke-RestMethod "$ApiBase/eventos/$id/reservas" -Method Post -ContentType application/json -Headers $h -Body $body
}
function AccionReserva($id, $accion) {
  Invoke-RestMethod "$ApiBase/reservas/$id/$accion" -Method Post -Headers $h | Out-Null
}

$eventos = @(
  @{ t = "Conferencia de IA Generativa"; d = "Tendencias y aplicaciones practicas de la IA generativa en la industria."; v = 1; c = 180; p = 90; tipo = "conferencia"; i = "2026-07-10T18:00:00-05:00"; f = "2026-07-10T21:00:00-05:00" },
  @{ t = "Concierto Sinfonico de Verano"; d = "La orquesta filarmonica interpreta clasicos en una noche inolvidable."; v = 1; c = 200; p = 150; tipo = "concierto"; i = "2026-07-18T19:00:00-05:00"; f = "2026-07-18T22:00:00-05:00" },
  @{ t = "Taller de Fotografia Urbana"; d = "Aprende composicion y luz natural recorriendo la ciudad."; v = 2; c = 30; p = 35; tipo = "taller"; i = "2026-07-12T09:00:00-05:00"; f = "2026-07-12T13:00:00-05:00" },
  @{ t = "Conferencia UX/UI 2026"; d = "Diseno de producto, investigacion de usuarios y sistemas de diseno."; v = 2; c = 50; p = 60; tipo = "conferencia"; i = "2026-07-25T17:00:00-05:00"; f = "2026-07-25T20:00:00-05:00" },
  @{ t = "Concierto de Rock Nacional"; d = "Las mejores bandas de rock del pais en un solo escenario."; v = 3; c = 500; p = 120; tipo = "concierto"; i = "2026-08-15T20:00:00-05:00"; f = "2026-08-15T23:00:00-05:00" },
  @{ t = "Festival de Jazz al Aire Libre"; d = "Una tarde de jazz con artistas locales e internacionales."; v = 3; c = 450; p = 110; tipo = "concierto"; i = "2026-09-05T18:00:00-05:00"; f = "2026-09-05T22:00:00-05:00" },
  @{ t = "Tech Summit Latinoamerica"; d = "Cloud, datos y arquitectura moderna con lideres de la industria."; v = 3; c = 400; p = 95; tipo = "conferencia"; i = "2026-09-20T09:00:00-05:00"; f = "2026-09-20T18:00:00-05:00" },
  @{ t = "Taller de Escritura Creativa"; d = "Tecnicas narrativas y desarrollo de personajes."; v = 2; c = 25; p = 25; tipo = "taller"; i = "2026-08-08T15:00:00-05:00"; f = "2026-08-08T18:00:00-05:00" }
)
$buyers = @(
  @{ n = "Ana Maria Gomez"; e = "ana.gomez@correo.com" }, @{ n = "Carlos Rodriguez"; e = "carlos.rodriguez@correo.com" },
  @{ n = "Valentina Torres"; e = "valentina.torres@correo.com" }, @{ n = "Andres Ramirez"; e = "andres.ramirez@correo.com" },
  @{ n = "Laura Martinez"; e = "laura.martinez@correo.com" }, @{ n = "Sebastian Lopez"; e = "sebastian.lopez@correo.com" },
  @{ n = "Daniela Castro"; e = "daniela.castro@correo.com" }, @{ n = "Juan David Perez"; e = "juan.perez@correo.com" },
  @{ n = "Mariana Ruiz"; e = "mariana.ruiz@correo.com" }, @{ n = "Felipe Moreno"; e = "felipe.moreno@correo.com" }
)

$nEv = 0; $nRes = 0; $nConf = 0; $nCanc = 0; $bi = 0
foreach ($e in $eventos) {
  $ev = NuevoEvento $e.t $e.d $e.v $e.c $e.p $e.tipo $e.i $e.f
  $nEv++
  $cuantas = 3 + ($nEv % 3)
  for ($k = 0; $k -lt $cuantas; $k++) {
    $b = $buyers[$bi % $buyers.Count]; $bi++
    $cant = 1 + (($bi * 2 + $k) % 6)
    if ($cant -gt $e.c) { $cant = 1 }
    $r = NuevaReserva $ev.id $cant $b.n $b.e
    $nRes++
    $a = ($bi + $k) % 5
    if ($a -le 2) { AccionReserva $r.id "confirmacion"; $nConf++ }
    elseif ($a -eq 3) { AccionReserva $r.id "cancelacion"; $nCanc++ }
  }
}

Write-Host ("OK -> Eventos: {0} | Reservas: {1} | Confirmadas: {2} | Canceladas: {3} | Pendientes: {4}" -f `
    $nEv, $nRes, $nConf, $nCanc, ($nRes - $nConf - $nCanc))
