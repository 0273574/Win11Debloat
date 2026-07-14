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
    /// Analizator systemu do zbierania informacji o konfiguracji Windows
    /// Dostarcza szczegółową analizę sprzętu i oprogramowania
    /// </summary>
    public class SystemAnalyzer
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Inicjalizacja analizatora systemu
        /// </summary>
        public SystemAnalyzer()
        {
            _logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();
        }

        /// <summary>
        /// Przeanalizuj pełną konfigurację systemu
        /// </summary>
        public async Task<SystemAnalysis> AnalyzeSystem()
        {
            _logger.Information("Rozpoczynanie analizy systemu...");
            
            // Zbierz wszystkie informacje o systemie
            var analysis = new SystemAnalysis
            {
                Timestamp = DateTime.UtcNow,
                OSInfo = GetOSInfo(),
                HardwareInfo = GetHardwareInfo(),
                InstalledApplications = GetInstalledApplications(),
                SystemSettings = await GetSystemSettings(),
                PerformanceMetrics = GetPerformanceMetrics()
            };

            _logger.Information("Analiza systemu zakończona");
            return analysis;
        }

        /// <summary>
        /// Pobierz informacje o systemie operacyjnym
        /// </summary>
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
                _logger.Error(ex, "Błąd podczas pobierania informacji o systemie operacyjnym");
                return new OSInfo();
            }
        }

        /// <summary>
        /// Pobierz informacje o sprzęcie
        /// </summary>
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
                _logger.Error(ex, "Błąd podczas pobierania informacji o sprzęcie");
                return new HardwareInfo();
            }
        }

        /// <summary>
        /// Pobierz listę zainstalowanych aplikacji z rejestru
        /// </summary>
        private List<ApplicationInfo> GetInstalledApplications()
        {
            var apps = new List<ApplicationInfo>();
            try
            {
                // Ścieżka rejestru Windows z zainstalowanymi aplikacjami
                const string registryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(registryPath))
                {
                    if (key == null) return apps;

                    // Iteruj przez wszystkie zainstalowane aplikacje
                    foreach (var subKeyName in key.GetSubKeyNames())
                    {
                        using (var subKey = key.OpenSubKey(subKeyName))
                        {
                            var displayName = subKey?.GetValue("DisplayName")?.ToString();
                            var displayVersion = subKey?.GetValue("DisplayVersion")?.ToString();
                            var installDate = subKey?.GetValue("InstallDate")?.ToString();
                            
                            // Dodaj aplikację jeśli ma nazwę
                            if (!string.IsNullOrWhiteSpace(displayName))
                            {
                                apps.Add(new ApplicationInfo
                                {
                                    Name = displayName,
                                    Version = displayVersion ?? "Nieznana",
                                    InstallDate = installDate,
                                    RegistryKey = subKeyName
                                });
                            }
                        }
                    }
                }
                
                _logger.Information("Znaleziono {Count} zainstalowanych aplikacji", apps.Count);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Błąd podczas pobierania zainstalowanych aplikacji");
            }

            return apps.OrderBy(a => a.Name).ToList();
        }

        /// <summary>
        /// Pobierz ustawienia systemowe
        /// </summary>
        private async Task<Dictionary<string, object>> GetSystemSettings()
        {
            var settings = new Dictionary<string, object>();
            
            try
            {
                settings["NazwaKomputera"] = Environment.MachineName;
                settings["NazwaUzytkownika"] = Environment.UserName;
                settings["DomenaUzytkownika"] = Environment.UserDomainName;
                settings["KatalogSystemowy"] = Environment.SystemDirectory;
                settings["ZlicznikSystemowy"] = Environment.TickCount;
                settings["WersjaOS"] = Environment.OSVersion.VersionString;
                settings["LiczbaProcesorow"] = Environment.ProcessorCount;
                
                _logger.Debug("Zebrano {Count} ustawień systemowych", settings.Count);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Błąd podczas pobierania ustawień systemowych");
            }

            return await Task.FromResult(settings);
        }

        /// <summary>
        /// Pobierz metryki wydajności systemu
        /// </summary>
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
                
                _logger.Debug("Metryki wydajności: Pamięć={Memory}MB, DostępnaMemoria={Available}GB", 
                    metrics.ProcessMemoryMB, metrics.AvailableMemoryGB);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Błąd podczas pobierania metryk wydajności");
            }

            return metrics;
        }

        /// <summary>
        /// Pobierz całkowitą dostępną pamięć w GB
        /// </summary>
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

        /// <summary>
        /// Pobierz dostępną pamięć RAM w GB
        /// </summary>
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

        /// <summary>
        /// Pobierz informacje o dyskach
        /// </summary>
        private List<DriveInfoData> GetDriveInfo()
        {
            var drives = new List<DriveInfoData>();
            try
            {
                // Zbierz informacje o wszystkich dyskach
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
                _logger.Error(ex, "Błąd podczas pobierania informacji o dyskach");
            }

            return drives;
        }
    }

    /// <summary>
    /// Kompletna analiza systemu
    /// </summary>
    public class SystemAnalysis
    {
        public DateTime Timestamp { get; set; }
        public OSInfo OSInfo { get; set; }
        public HardwareInfo HardwareInfo { get; set; }
        public List<ApplicationInfo> InstalledApplications { get; set; }
        public Dictionary<string, object> SystemSettings { get; set; }
        public PerformanceMetrics PerformanceMetrics { get; set; }
    }

    /// <summary>
    /// Informacje o systemie operacyjnym
    /// </summary>
    public class OSInfo
    {
        public string PlatformId { get; set; }
        public string VersionString { get; set; }
        public bool IsWindows { get; set; }
        public int ProcessorCount { get; set; }
        public string SystemDirectory { get; set; }
    }

    /// <summary>
    /// Informacje o sprzęcie
    /// </summary>
    public class HardwareInfo
    {
        public int ProcessorCount { get; set; }
        public double TotalMemoryGB { get; set; }
        public double AvailableMemoryGB { get; set; }
        public List<DriveInfoData> DriveInfo { get; set; } = new();
    }

    /// <summary>
    /// Informacje o aplikacji
    /// </summary>
    public class ApplicationInfo
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string InstallDate { get; set; }
        public string RegistryKey { get; set; }
    }

    /// <summary>
    /// Informacje o dysku
    /// </summary>
    public class DriveInfoData
    {
        public string Name { get; set; }
        public string VolumeLabel { get; set; }
        public long TotalSizeGB { get; set; }
        public long AvailableFreeSpaceGB { get; set; }
        public string DriveFormat { get; set; }
    }

    /// <summary>
    /// Metryki wydajności systemu
    /// </summary>
    public class PerformanceMetrics
    {
        public long ProcessMemoryMB { get; set; }
        public double TotalMemoryGB { get; set; }
        public double AvailableMemoryGB { get; set; }
        public TimeSpan UpTime { get; set; }
    }
}
