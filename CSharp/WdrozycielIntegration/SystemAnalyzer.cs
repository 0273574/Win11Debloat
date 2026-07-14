using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using Serilog;

namespace WdrozycielIntegration
{
    /// <summary>
    /// System analyzer for gathering Windows system information
    /// Provides detailed hardware and software analysis
    /// </summary>
    public class SystemAnalyzer
    {
        private readonly ILogger _logger;

        public SystemAnalyzer()
        {
            _logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();
        }

        /// <summary>
        /// Analyze complete system configuration
        /// </summary>
        public async Task<SystemAnalysis> AnalyzeSystem()
        {
            _logger.Information("Starting system analysis...");
            
            var analysis = new SystemAnalysis
            {
                Timestamp = DateTime.UtcNow,
                OSInfo = GetOSInfo(),
                HardwareInfo = GetHardwareInfo(),
                InstalledApplications = GetInstalledApplications(),
                SystemSettings = await GetSystemSettings(),
                PerformanceMetrics = GetPerformanceMetrics()
            };

            _logger.Information("System analysis completed");
            return analysis;
        }

        private OSInfo GetOSInfo()
        {
            try
            {
                var osVersion = Environment.OSVersion;
                return new OSInfo
                {
                    PlatformId = osVersion.Platform.ToString(),
                    VersionString = osVersion.VersionString,
                    IsWindows = osVersion.Platform == PlatformID.Win32NT,
                    ProcessorCount = Environment.ProcessorCount,
                    SystemDirectory = Environment.SystemDirectory
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error getting OS info");
                return new OSInfo();
            }
        }

        private HardwareInfo GetHardwareInfo()
        {
            try
            {
                var hardware = new HardwareInfo
                {
                    ProcessorCount = Environment.ProcessorCount,
                    TotalMemoryGB = GetTotalMemory(),
                    AvailableMemoryGB = GetAvailableMemory(),
                    DriveInfo = GetDriveInfo()
                };
                return hardware;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error getting hardware info");
                return new HardwareInfo();
            }
        }

        private List<ApplicationInfo> GetInstalledApplications()
        {
            var apps = new List<ApplicationInfo>();
            try
            {
                const string registryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(registryPath))
                {
                    if (key == null) return apps;

                    foreach (var subKeyName in key.GetSubKeyNames())
                    {
                        using (var subKey = key.OpenSubKey(subKeyName))
                        {
                            var displayName = subKey?.GetValue("DisplayName")?.ToString();
                            var displayVersion = subKey?.GetValue("DisplayVersion")?.ToString();
                            var installDate = subKey?.GetValue("InstallDate")?.ToString();
                            
                            if (!string.IsNullOrWhiteSpace(displayName))
                            {
                                apps.Add(new ApplicationInfo
                                {
                                    Name = displayName,
                                    Version = displayVersion ?? "Unknown",
                                    InstallDate = installDate,
                                    RegistryKey = subKeyName
                                });
                            }
                        }
                    }
                }
                
                _logger.Information("Found {Count} installed applications", apps.Count);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error getting installed applications");
            }

            return apps.OrderBy(a => a.Name).ToList();
        }

        private async Task<Dictionary<string, object>> GetSystemSettings()
        {
            var settings = new Dictionary<string, object>();
            
            try
            {
                settings["ComputerName"] = Environment.MachineName;
                settings["UserName"] = Environment.UserName;
                settings["UserDomainName"] = Environment.UserDomainName;
                settings["SystemDirectory"] = Environment.SystemDirectory;
                settings["TickCount"] = Environment.TickCount;
                settings["OSVersion"] = Environment.OSVersion.VersionString;
                settings["ProcessorCount"] = Environment.ProcessorCount;
                
                _logger.Debug("Gathered {Count} system settings", settings.Count);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error getting system settings");
            }

            return await Task.FromResult(settings);
        }

        private PerformanceMetrics GetPerformanceMetrics()
        {
            var metrics = new PerformanceMetrics();
            
            try
            {
                var process = Process.GetCurrentProcess();
                metrics.ProcessMemoryMB = process.WorkingSet64 / (1024 * 1024);
                metrics.TotalMemoryGB = GetTotalMemory();
                metrics.AvailableMemoryGB = GetAvailableMemory();
                metrics.UpTime = DateTime.Now - process.StartTime;
                
                _logger.Debug("Performance metrics: Memory={Memory}MB, AvailableMemory={Available}GB", 
                    metrics.ProcessMemoryMB, metrics.AvailableMemoryGB);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error getting performance metrics");
            }

            return metrics;
        }

        private double GetTotalMemory()
        {
            try
            {
                var query = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem");
                var total = query.Get().Cast<ManagementObject>().FirstOrDefault()?["TotalVisibleMemorySize"];
                return total != null ? Convert.ToDouble(total) / (1024 * 1024) : 0;
            }
            catch
            {
                return 0;
            }
        }

        private double GetAvailableMemory()
        {
            try
            {
                var query = new ManagementObjectSearcher("SELECT FreePhysicalMemory FROM Win32_OperatingSystem");
                var available = query.Get().Cast<ManagementObject>().FirstOrDefault()?["FreePhysicalMemory"];
                return available != null ? Convert.ToDouble(available) / (1024 * 1024) : 0;
            }
            catch
            {
                return 0;
            }
        }

        private List<DriveInfoData> GetDriveInfo()
        {
            var drives = new List<DriveInfoData>();
            try
            {
                foreach (var drive in DriveInfo.GetDrives())
                {
                    if (drive.IsReady)
                    {
                        drives.Add(new DriveInfoData
                        {
                            Name = drive.Name,
                            VolumeLabel = drive.VolumeLabel,
                            TotalSizeGB = drive.TotalSize / (1024L * 1024 * 1024),
                            AvailableFreeSpaceGB = drive.AvailableFreeSpace / (1024L * 1024 * 1024),
                            DriveFormat = drive.DriveFormat
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error getting drive info");
            }

            return drives;
        }
    }

    public class SystemAnalysis
    {
        public DateTime Timestamp { get; set; }
        public OSInfo OSInfo { get; set; }
        public HardwareInfo HardwareInfo { get; set; }
        public List<ApplicationInfo> InstalledApplications { get; set; }
        public Dictionary<string, object> SystemSettings { get; set; }
        public PerformanceMetrics PerformanceMetrics { get; set; }
    }

    public class OSInfo
    {
        public string PlatformId { get; set; }
        public string VersionString { get; set; }
        public bool IsWindows { get; set; }
        public int ProcessorCount { get; set; }
        public string SystemDirectory { get; set; }
    }

    public class HardwareInfo
    {
        public int ProcessorCount { get; set; }
        public double TotalMemoryGB { get; set; }
        public double AvailableMemoryGB { get; set; }
        public List<DriveInfoData> DriveInfo { get; set; } = new();
    }

    public class ApplicationInfo
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string InstallDate { get; set; }
        public string RegistryKey { get; set; }
    }

    public class DriveInfoData
    {
        public string Name { get; set; }
        public string VolumeLabel { get; set; }
        public long TotalSizeGB { get; set; }
        public long AvailableFreeSpaceGB { get; set; }
        public string DriveFormat { get; set; }
    }

    public class PerformanceMetrics
    {
        public long ProcessMemoryMB { get; set; }
        public double TotalMemoryGB { get; set; }
        public double AvailableMemoryGB { get; set; }
        public TimeSpan UpTime { get; set; }
    }
}
