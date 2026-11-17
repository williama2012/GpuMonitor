using System;
using System.Linq;
using LibreHardwareMonitor.Hardware;

namespace GpuMonitor;

/// <summary>
/// Wraps the LibreHardwareMonitor library to provide easy access to CPU stats.
/// </summary>
public sealed class CpuMonitor : IDisposable
{
    private readonly Computer _computer;

    public CpuMonitor()
    {
        _computer = new Computer();
        //_computer.IsExternalCpuEnabled = true;
        //_computer.Options.RunEvery = 1000;
        _computer.Open();
    }

    /// <summary>
    /// Returns a tuple containing (CPU Load %, CPU Power Watts).
    /// </summary>
    public (float Load, float Power) GetStats()
    {
        var cpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
        if (cpu == null) return (0, 0);

        cpu.Update();

        // Find Load sensor (usually "CPU" or "Total")
        var loadSensor = cpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && 
            (s.Name.Contains("Total", StringComparison.OrdinalIgnoreCase) || s.Name.Contains("CPU", StringComparison.OrdinalIgnoreCase)));

        // Find Power sensor (usually "Package" or "Core")
        var powerSensor = cpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Power && 
            (s.Name.Contains("Package", StringComparison.OrdinalIgnoreCase) || s.Name.Contains("Core", StringComparison.OrdinalIgnoreCase)));

        float load = loadSensor != null ? Convert.ToSingle(loadSensor.Value) : 0;
        float power = powerSensor != null ? Convert.ToSingle(powerSensor.Value) : 0;

        return (load, power);
    }

    public void Dispose()
    {
        _computer.Close();
    }
}
