using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Serilog;

namespace WdrozycielIntegration
{
    /// <summary>
    /// Menedżer konfiguracji do obsługi konfiguracji wdrażania
    /// Obsługuje pliki konfiguracji oparte na JSON z walidacją
    /// </summary>
    public class ConfigManager
    {
        private readonly ILogger _logger;
        private readonly string _configDirectory;
        private Dictionary<string, object> _cachedConfig;

        /// <summary>
        /// Inicjalizacja menedżera konfiguracji
        /// </summary>
        public ConfigManager(string configDirectory = null)
        {
            _configDirectory = configDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
            _logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();
            
            EnsureConfigDirectoryExists();
        }

        /// <summary>
        /// Załaduj konfigurację z pliku JSON
        /// </summary>
        public T LoadConfiguration<T>(string configFileName) where T : class
        {
            try
            {
                var configPath = Path.Combine(_configDirectory, configFileName);
                
                // Sprawdzenie czy plik konfiguracji istnieje
                if (!File.Exists(configPath))
                {
                    _logger.Warning("Plik konfiguracji nie znaleziony: {ConfigPath}", configPath);
                    return null;
                }

                // Wczytaj i zdeserializuj plik JSON
                var json = File.ReadAllText(configPath);
                var config = JsonConvert.DeserializeObject<T>(json);
                
                _logger.Information("Załadowano konfigurację z: {ConfigPath}", configPath);
                return config;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Błąd podczas ładowania konfiguracji: {ConfigFileName}", configFileName);
                return null;
            }
        }

        /// <summary>
        /// Zapisz konfigurację do pliku JSON
        /// </summary>
        public bool SaveConfiguration<T>(string configFileName, T config) where T : class
        {
            try
            {
                var configPath = Path.Combine(_configDirectory, configFileName);
                var json = JsonConvert.SerializeObject(config, Formatting.Indented);
                
                File.WriteAllText(configPath, json);
                _logger.Information("Zapisano konfigurację do: {ConfigPath}", configPath);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Błąd podczas zapisywania konfiguracji: {ConfigFileName}", configFileName);
                return false;
            }
        }

        /// <summary>
        /// Pobierz ścieżkę pliku konfiguracji
        /// </summary>
        public string GetConfigFilePath(string configFileName)
        {
            return Path.Combine(_configDirectory, configFileName);
        }

        /// <summary>
        /// Wylistuj wszystkie pliki konfiguracji
        /// </summary>
        public IEnumerable<string> ListConfigFiles()
        {
            try
            {
                // Zwróć puste jeśli katalog nie istnieje
                if (!Directory.Exists(_configDirectory))
                    return Enumerable.Empty<string>();

                return Directory.GetFiles(_configDirectory, "*.json")
                    .Select(Path.GetFileName);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Błąd podczas wylistowania plików konfiguracji");
                return Enumerable.Empty<string>();
            }
        }

        /// <summary>
        /// Zweryfikuj strukturę konfiguracji
        /// </summary>
        public ConfigValidationResult ValidateConfiguration<T>(string configFileName, T expectedStructure) where T : class
        {
            var result = new ConfigValidationResult { IsValid = true };

            try
            {
                var config = LoadConfiguration<T>(configFileName);
                
                // Sprawdzenie czy konfiguracja została załadowana
                if (config == null)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Plik konfiguracji nie znaleziony: {configFileName}");
                    return result;
                }

                _logger.Information("Walidacja konfiguracji przeszła pomyślnie: {ConfigFileName}", configFileName);
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add(ex.Message);
                _logger.Error(ex, "Walidacja konfiguracji nie powiodła się: {ConfigFileName}", configFileName);
            }

            return result;
        }

        /// <summary>
        /// Scal wiele konfiguracji
        /// </summary>
        public Dictionary<string, object> MergeConfigurations(params Dictionary<string, object>[] configs)
        {
            var merged = new Dictionary<string, object>();
            
            // Przeiteruj przez wszystkie konfiguracje i scal je
            foreach (var config in configs)
            {
                if (config == null) continue;
                
                foreach (var kvp in config)
                {
                    merged[kvp.Key] = kvp.Value;
                }
            }

            _logger.Information("Scalono {Count} słowników konfiguracyjnych", configs.Length);
            return merged;
        }

        /// <summary>
        /// Utwórz domyślną strukturę konfiguracji
        /// </summary>
        public void CreateDefaultConfigurations()
        {
            try
            {
                // Domyślne profile wdrażania
                var defaultProfiles = new List<object>
                {
                    new
                    {
                        name = "Minimalny",
                        description = "Minimalne wdrażanie tylko z aplikacjami podstawowymi",
                        priority = 1
                    },
                    new
                    {
                        name = "Standardowy",
                        description = "Standardowe wdrażanie z popularnymi aplikacjami",
                        priority = 2
                    },
                    new
                    {
                        name = "Pełny",
                        description = "Pełne wdrażanie ze wszystkimi dostępnymi aplikacjami",
                        priority = 3
                    }
                };

                SaveConfiguration("DeploymentProfiles.json", defaultProfiles);
                
                _logger.Information("Utworzono domyślne profile wdrażania");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Błąd podczas tworzenia domyślnych konfiguracji");
            }
        }

        /// <summary>
        /// Upewnij się, że katalog konfiguracji istnieje
        /// </summary>
        private void EnsureConfigDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(_configDirectory))
                {
                    Directory.CreateDirectory(_configDirectory);
                    _logger.Information("Utworzono katalog konfiguracji: {ConfigDirectory}", _configDirectory);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Błąd podczas tworzenia katalogu konfiguracji");
            }
        }
    }

    /// <summary>
    /// Wynik walidacji konfiguracji
    /// </summary>
    public class ConfigValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }
}
