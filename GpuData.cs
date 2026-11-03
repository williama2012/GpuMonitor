namespace GpuMonitor;

/// <summary>Snapshot of one video card, in raw NVML units.</summary>
internal sealed class GpuData
{
    public int Index { get; }
    public string Name { get; }
    public ulong VramTotal { get; } // bytes
    public ulong VramFree { get; }  // bytes
    public ulong VramUsed { get; }  // bytes
    public uint CoreLoad { get; }  // % — nvmlDeviceGetUtilizationRates gpu
    public uint VramLoad { get; }  // % — nvmlDeviceGetSamples UTILITY_MEMORY (util-rates mem is 0 on some drivers)

    public GpuData(int index, string name, ulong total, ulong free, ulong used, uint core, uint vram)
    {
        Index = index;
        Name = name;
        VramTotal = total;
        VramFree = free;
        VramUsed = used;
        CoreLoad = core;
        VramLoad = vram;
    }

    public static double BytesToGiB(ulong bytes) => bytes / 1024d / 1024d / 1024d;
}
