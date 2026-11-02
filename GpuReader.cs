using System.Text;

namespace GpuMonitor;

/// <summary>
/// Owns the NVML lifecycle: one <c>nvmlInit</c> for the process lifetime,
/// repeated reads on demand, one <c>nvmlShutdown</c> on dispose.
/// NVML is designed for repeated queries, so we don't re-init every tick.
/// </summary>
internal sealed class GpuReader : IDisposable
{
    private bool _initialized;
    private string? _error;

    public bool IsReady => _initialized;
    public string? Error => _error;

    private void EnsureInitialized()
    {
        if (_initialized)
            return;

        int rc = Nvml.nvmlInit();
        if (rc != Nvml.Success)
        {
            _error = $"NVML initialization failed (code {rc}). Is an NVIDIA driver installed?";
            return;
        }
        _initialized = true;
    }

    public (bool ok, IReadOnlyList<GpuData> gpus, string? error) GetGpus()
    {
        EnsureInitialized();
        if (!_initialized)
            return (false, Array.Empty<GpuData>(), _error);

        if (Nvml.nvmlDeviceGetCount(out var count) != Nvml.Success)
            return (false, Array.Empty<GpuData>(), "nvmlDeviceGetCount failed.");

        var gpus = new List<GpuData>((int)count);
        for (uint i = 0; i < count; i++)
        {
            if (Nvml.nvmlDeviceGetHandleByIndex(i, out var handle) != Nvml.Success)
                continue;

            var name = new StringBuilder(256);
            Nvml.nvmlDeviceGetName(handle, name, 256);
            Nvml.nvmlDeviceGetMemoryInfo(handle, out var mem);
            Nvml.nvmlDeviceGetUtilizationRates(handle, out var core, out var vram);

            gpus.Add(new GpuData((int)i, name.ToString(), mem.total, mem.free, mem.used, core, vram));
        }

        return (true, gpus, null);
    }

    public void Dispose()
    {
        if (_initialized)
            Nvml.nvmlShutdown();
        _initialized = false;
    }
}
