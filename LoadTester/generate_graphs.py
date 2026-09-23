import pandas as pd
import matplotlib.pyplot as plt
import os
#usa los resultados para hacer las graficas 
CSV_FILE = "results_summary.csv"
OUT_DIR = "graficas"

os.makedirs(OUT_DIR, exist_ok=True)

df = pd.read_csv(CSV_FILE)
df["success_rate"] = (df["success"] / df["total"]) * 100

df["order"] = range(len(df))
labels = df["label"].unique()

def plot_by_label(x_col, y_col, xlabel, ylabel, title, filename, kind="line"):
    plt.figure(figsize=(8, 5))
    styles = [
        {"linestyle": "-", "marker": "o", "linewidth": 2.5},
        {"linestyle": "--", "marker": "s", "linewidth": 1.5},
    ]
    for i, label in enumerate(labels):
        sub = df[df["label"] == label].sort_values(x_col)
        style = styles[i % len(styles)]
        plt.plot(sub[x_col], sub[y_col], label=label, markersize=9, **style)
    plt.xlabel(xlabel)
    plt.ylabel(ylabel)
    plt.title(title)
    plt.legend(title="Endpoint")
    plt.grid(True, linestyle="--", alpha=0.5)
    plt.tight_layout()
    path = os.path.join(OUT_DIR, filename)
    plt.savefig(path, dpi=150)
    plt.close()
    print(f"Guardado: {path}")


plot_by_label(
    "concurrency", "avg_latency_ms",
    "Concurrencia (requests simultaneos)", "Latencia promedio (ms)",
    "Carga vs Tiempo de respuesta", "1_carga_vs_latencia.png"
)


plot_by_label(
    "concurrency", "success_rate",
    "Concurrencia (requests simultaneos)", "Tasa de exito (%)",
    "Carga vs Tasa de exito", "2_carga_vs_exito.png"
)


plot_by_label(
    "concurrency", "failed",
    "Concurrencia (requests simultaneos)", "Requests fallidos",
    "Carga vs Fallos", "3_carga_vs_fallos.png"
)

plt.figure(figsize=(9, 5))
pivot_cpu = df.pivot_table(index="round", columns="label", values="avg_cpu_percent", sort=False)
pivot_cpu.plot(kind="bar", ax=plt.gca())
plt.xlabel("Ronda")
plt.ylabel("CPU promedio (%)")
plt.title("Ronda vs Uso de CPU")
plt.legend(title="Endpoint")
plt.tight_layout()
plt.savefig(os.path.join(OUT_DIR, "4_ronda_vs_cpu.png"), dpi=150)
plt.close()
print(f"Guardado: {os.path.join(OUT_DIR, '4_ronda_vs_cpu.png')}")


plt.figure(figsize=(9, 5))
pivot_mem = df.pivot_table(index="round", columns="label", values="avg_mem_mb", sort=False)
pivot_mem.plot(kind="bar", ax=plt.gca())
plt.xlabel("Ronda")
plt.ylabel("Memoria promedio (MB)")
plt.title("Ronda vs Uso de memoria")
plt.legend(title="Endpoint")
plt.tight_layout()
plt.savefig(os.path.join(OUT_DIR, "5_ronda_vs_memoria.png"), dpi=150)
plt.close()
print(f"Guardado: {os.path.join(OUT_DIR, '5_ronda_vs_memoria.png')}")

print("\nListo. Todas las graficas quedaron en la carpeta 'graficas/'.")