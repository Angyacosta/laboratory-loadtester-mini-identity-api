# LoadTester — Pruebas de carga controlada para mini-identity-api-dotnet

Consola de línea de comandos en .NET 8 desarrollada como parte del laboratorio
**"Controlled Load Testing and Resilience Analysis of a .NET Web API"** del
programa de Ingeniería de Sistemas de la Universidad de los Llanos (2026-I).

La aplicación envía peticiones HTTP repetidas y concurrentes contra un
endpoint configurable de la API [mini-identity-api-dotnet](https://github.com/wolfcor10/mini-identity-api-dotnet)
corriendo en **entorno local**, y mide:

- Tiempo de respuesta (latencia promedio, mínima y máxima)
- Cantidad de requests exitosos y fallidos
- Uso de CPU y memoria del proceso durante la prueba
- Duración real de cada ronda

Los resultados se guardan automáticamente en archivos `.csv` para su
posterior análisis y graficación.


## Requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download) o superior
- [Python 3](https://www.python.org/downloads/) con `pandas` y `matplotlib`
  (solo para generar las gráficas)
- La API [mini-identity-api-dotnet](https://github.com/wolfcor10/mini-identity-api-dotnet)
  corriendo localmente

## Cómo correr una ronda de prueba

```bash
dotnet build

dotnet run -- --url http://localhost:5132/api/auth/login --method POST \
  --label login --round c5 --concurrency 5 --duration 10
```

### Parámetros disponibles

| Parámetro | Descripción | Valor por defecto |
|---|---|---|
| `--url` | Endpoint objetivo | `https://localhost:5001/api/auth/login` |
| `--method` | Método HTTP (`GET`, `POST`, etc.) | `POST` |
| `--concurrency` | Número de workers concurrentes | `5` |
| `--duration` | Duración de la ronda en segundos | `10` |
| `--round` | Nombre identificador de la ronda | `round1` |
| `--label` | Etiqueta del endpoint bajo prueba | `login` |
| `--body` | Cuerpo JSON de la petición | credenciales de admin de ejemplo |
| `--no-body` | Omite el cuerpo (útil para `GET`) | — |
| `--token` | Token Bearer para endpoints autenticados | — |

## Salida generada

- `results_summary.csv` — una fila resumen por cada ronda ejecutada (se
  agrega, no se sobrescribe)
- `results_raw_<label>_<round>.csv` — detalle de cada request individual de
  esa ronda

## Generar las gráficas

```bash
pip install pandas matplotlib
python generate_graphs.py
```

Genera en la carpeta `graficas/`:

1. Carga vs Tiempo de respuesta
2. Carga vs Tasa de éxito
3. Carga vs Fallos
4. Ronda vs Uso de CPU
5. Ronda vs Uso de memoria


Universidad de los Llanos — Facultad de Ciencias Básicas e Ingenierías —
Ingeniería de Sistemas 2026-I
