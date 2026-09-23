using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;


CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;


var config = ParseArgs(args);

Console.WriteLine("=== LoadTester ===");
Console.WriteLine($"URL objetivo     : {config.Url}");
Console.WriteLine($"Método HTTP      : {config.Method}");
Console.WriteLine($"Concurrencia     : {config.Concurrency}");
Console.WriteLine($"Duración (seg)   : {config.DurationSeconds}");
Console.WriteLine($"Nombre de ronda  : {config.RoundName}");
Console.WriteLine($"Endpoint (label) : {config.Label}");
Console.WriteLine();

if (!config.Url.Contains("localhost") && !config.Url.Contains("127.0.0.1"))
{
    Console.WriteLine("ADVERTENCIA: la guía exige que las pruebas se hagan solo contra localhost.");
    Console.WriteLine("Verifica que la URL apunte a tu entorno local antes de continuar.");
    Console.Write("¿Continuar de todas formas? (s/n): ");
    var resp = Console.ReadLine();
    if (!string.Equals(resp, "s", StringComparison.OrdinalIgnoreCase))
    {
        return;
    }
}

var results = new System.Collections.Concurrent.ConcurrentBag<RequestResult>();
using var httpClient = new HttpClient
{
    Timeout = TimeSpan.FromSeconds(10)
};

var process = Process.GetCurrentProcess();
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(config.DurationSeconds));
var stopwatch = Stopwatch.StartNew();

var cpuSamples = new List<double>();
var memSamples = new List<long>();
var monitorTask = Task.Run(async () =>
{
    var lastCpu = process.TotalProcessorTime;
    var lastTime = DateTime.UtcNow;
    while (!cts.IsCancellationRequested)
    {
        await Task.Delay(500);
        process.Refresh();
        var nowCpu = process.TotalProcessorTime;
        var nowTime = DateTime.UtcNow;
        var cpuUsedMs = (nowCpu - lastCpu).TotalMilliseconds;
        var elapsedMs = (nowTime - lastTime).TotalMilliseconds;
        var cpuPercent = elapsedMs > 0
            ? (cpuUsedMs / (Environment.ProcessorCount * elapsedMs)) * 100.0
            : 0;

        cpuSamples.Add(cpuPercent);
        memSamples.Add(process.WorkingSet64 / (1024 * 1024)); // MB

        lastCpu = nowCpu;
        lastTime = nowTime;
    }
});


var workers = new List<Task>();
for (int i = 0; i < config.Concurrency; i++)
{
    workers.Add(Task.Run(async () =>
    {
        while (!cts.IsCancellationRequested)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                using var request = new HttpRequestMessage(new HttpMethod(config.Method), config.Url);
                if (!string.IsNullOrEmpty(config.Body))
                {
                    request.Content = new StringContent(config.Body, Encoding.UTF8, "application/json");
                }
                if (!string.IsNullOrEmpty(config.Token))
                {
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.Token);
                }

                using var response = await httpClient.SendAsync(request, cts.Token);
                sw.Stop();

                results.Add(new RequestResult(
                    Success: response.IsSuccessStatusCode,
                    StatusCode: (int)response.StatusCode,
                    ElapsedMs: sw.Elapsed.TotalMilliseconds,
                    Error: null));
            }
            catch (OperationCanceledException)
            {
                // fin de la ronda
            }
            catch (Exception ex)
            {
                sw.Stop();
                results.Add(new RequestResult(
                    Success: false,
                    StatusCode: 0,
                    ElapsedMs: sw.Elapsed.TotalMilliseconds,
                    Error: ex.GetType().Name));
            }
        }
    }));
}

await Task.WhenAll(workers);
await monitorTask;
stopwatch.Stop();

//resultados 
var list = results.ToList();
var total = list.Count;
var success = list.Count(r => r.Success);
var failed = total - success;
var avgLatency = list.Count > 0 ? list.Average(r => r.ElapsedMs) : 0;
var maxLatency = list.Count > 0 ? list.Max(r => r.ElapsedMs) : 0;
var minLatency = list.Count > 0 ? list.Min(r => r.ElapsedMs) : 0;
var avgCpu = cpuSamples.Count > 0 ? cpuSamples.Average() : 0;
var avgMem = memSamples.Count > 0 ? memSamples.Average() : 0;

Console.WriteLine();
Console.WriteLine("=== Resultados de la ronda ===");
Console.WriteLine($"Total requests     : {total}");
Console.WriteLine($"Exitosos           : {success}");
Console.WriteLine($"Fallidos           : {failed}");
Console.WriteLine($"Latencia promedio  : {avgLatency:F2} ms");
Console.WriteLine($"Latencia mínima    : {minLatency:F2} ms");
Console.WriteLine($"Latencia máxima    : {maxLatency:F2} ms");
Console.WriteLine($"CPU promedio (app) : {avgCpu:F2} %");
Console.WriteLine($"RAM promedio (app) : {avgMem:F2} MB");
Console.WriteLine($"Duración real      : {stopwatch.Elapsed.TotalSeconds:F2} s");

//csv - guardar resultados
var summaryFile = "results_summary.csv";
var fileExists = File.Exists(summaryFile);
using (var writer = new StreamWriter(summaryFile, append: true))
{
    if (!fileExists)
    {
        writer.WriteLine("label,round,concurrency,total,success,failed,avg_latency_ms,min_latency_ms,max_latency_ms,avg_cpu_percent,avg_mem_mb,duration_s");
    }
    writer.WriteLine($"{config.Label},{config.RoundName},{config.Concurrency},{total},{success},{failed},{avgLatency:F2},{minLatency:F2},{maxLatency:F2},{avgCpu:F2},{avgMem:F2},{stopwatch.Elapsed.TotalSeconds:F2}");
}
Console.WriteLine();
Console.WriteLine($"Resumen agregado a {Path.GetFullPath(summaryFile)}");

var rawFile = $"results_raw_{config.Label}_{config.RoundName}.csv";
using (var writer = new StreamWriter(rawFile))
{
    writer.WriteLine("success,status_code,elapsed_ms,error");
    foreach (var r in list)
    {
        writer.WriteLine($"{r.Success},{r.StatusCode},{r.ElapsedMs:F2},{r.Error}");
    }
}
Console.WriteLine($"Detalle de requests guardado en {Path.GetFullPath(rawFile)}");


static TestConfig ParseArgs(string[] args)
{
    string url = "https://localhost:5001/api/auth/login";
    string method = "POST";
    int concurrency = 5;
    int duration = 10;
    string roundName = "round1";
    string label = "login";
    string? body = "{\"usernameOrEmail\":\"admin\",\"password\":\"Admin123*\"}";
    string? token = null;

    for (int i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--url": url = args[++i]; break;
            case "--method": method = args[++i]; break;
            case "--concurrency": concurrency = int.Parse(args[++i]); break;
            case "--duration": duration = int.Parse(args[++i]); break;
            case "--round": roundName = args[++i]; break;
            case "--label": label = args[++i]; break;
            case "--body": body = args[++i]; break;
            case "--no-body": body = null; break;
            case "--token": token = args[++i]; break;
        }
    }

    return new TestConfig(url, method, concurrency, duration, roundName, label, body, token);
}

record TestConfig(string Url, string Method, int Concurrency, int DurationSeconds, string RoundName, string Label, string? Body, string? Token);
record RequestResult(bool Success, int StatusCode, double ElapsedMs, string? Error);