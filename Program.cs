namespace GpuMonitor;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Diagnostic mode: print the live GPU table to the console and exit (no window).
        // Lets us prove the NVML read works without a GUI, e.g. from scripts/CI.
        if (args.Any(a => a == "--dump"))
        {
            RunDump();
            return;
        }

        Application.Run(new GpuMonitorForm());
    }

    private static void RunDump()
    {
        using var reader = new GpuReader();
        var (ok, gpus, err) = reader.GetGpus();
        if (!ok)
        {
            Console.Error.WriteLine(err ?? "Error");
            Environment.ExitCode = 1;
            return;
        }

        Console.WriteLine("{0,-8} {1,-45} {2,10} {3,10} {4,7} {5,7}",
            "idx", "name", "vram_used", "vram_free", "core%", "vram%");
        foreach (var g in gpus)
        {
            Console.WriteLine("{0,-8} {1,-45} {2,10:F1} {3,10:F1} {4,7} {5,7}",
                g.Index, g.Name,
                GpuData.BytesToGiB(g.VramUsed),
                GpuData.BytesToGiB(g.VramFree),
                g.CoreLoad,
                g.VramLoad);
        }
    }
}
