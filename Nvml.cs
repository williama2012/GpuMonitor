using System.Runtime.InteropServices;
using System.Text;

namespace GpuMonitor;

/// <summary>
/// Thin P/Invoke layer over the NVIDIA Management Library (NVML).
/// NVML is the same backend that <c>nvidia-smi</c> uses, so the values match it.
/// No NuGet dependency: we load nvml.dll straight from System32.
/// </summary>
internal static class Nvml
{
    public const int Success = 0; // NVML_SUCCESS

    [StructLayout(LayoutKind.Sequential)]
    internal struct Sample
    {
        public ulong sampleTime;   // CPU timestamp, microseconds
        public uint sampleValue;   // first member of the nvmlValue_t union (unsigned int)
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MemoryInfo
    {
        public ulong total; // bytes
        public ulong free;  // bytes
        public ulong used;  // bytes
    }

    internal const uint SamplingTypeUtilityMemory = 1; // NVML_SAMPLING_TYPE_UTILITY_MEMORY

    [DllImport("nvml.dll", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int nvmlInit();

    [DllImport("nvml.dll", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int nvmlDeviceGetCount(out uint deviceCount);

    [DllImport("nvml.dll", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int nvmlDeviceGetHandleByIndex(uint deviceIndex, out IntPtr device);

    [DllImport("nvml.dll", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int nvmlDeviceGetName(IntPtr device, StringBuilder name, uint length);

    [DllImport("nvml.dll", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int nvmlDeviceGetMemoryInfo(IntPtr device, out MemoryInfo memory);

    [DllImport("nvml.dll", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int nvmlDeviceGetUtilizationRates(IntPtr device, out uint gpuUtilization, out uint memoryUtilization);

    [DllImport("nvml.dll", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int nvmlDeviceGetSamples(IntPtr device, uint type, ref ulong lastSeenTimeStamp, ref uint sampleValType, ref uint sampleCount, out Sample samples);

    [DllImport("nvml.dll", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int nvmlShutdown();
}
