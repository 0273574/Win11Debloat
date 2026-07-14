using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Serilog;

namespace WdrozycielIntegration
{
    /// <summary>
    /// Zaawansowany menedżer wdrażania zintegrowany z Win11Debloat
    /// Obsługuje wieloprofilowe konfiguracje i przepływy wdrażania
    /// </summary>
    public class DeploymentManager
    {
        private readonly ILogger _logger;
        private readonly string _configPath;
        private Dictionary<string, DeploymentProfile> _profiles;

        /// <summary>
        /// Inicjalizacja menedżera wdrażania
        /// </summary>
        public DeploymentManager(string configPath = null)
        {
            _configPath = configPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
            _logger = new LoggerConfiguration()
                .WriteTo.File(Path.Combine(_configPath, "deployment-{Date}.log"))
                .WriteTo.Console()
                .CreateLogger();
            
            _profiles = new Dictionary<string, DeploymentProfile>();
            LoadProfiles();
        }

        /// <summary>
        /// Załaduj profile wdrażania z konfiguracji JSON
        /// </summary>
        public void LoadProfiles()
        {
            try
            {
                var profilesFile = Path.Combine(_configPath, "DeploymentProfiles.json");
                if (!File.Exists(profilesFile))
                {
                    _logger.Warning("Plik profili nie znaleziony: {ProfilesFile}", profilesFile);
                    return;
                }

                // Wczytaj JSON i deserializuj do listy profili
                var json = File.ReadAllText(profilesFile);
                var profilesList = JsonConvert.DeserializeObject<List<DeploymentProfile>>(json);
                
                _profiles = profilesList?.ToDictionary(p => p.Name) ?? new Dictionary<string, DeploymentProfile>();
                _logger.Information("Załadowano {Count} profili wdrażania", _profiles.Count);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Błąd podczas ładowania profili wdrażania");
            }
        }

        /// <summary>
        /// Pobierz dostępne profile wdrażania
        /// </summary>
        public IEnumerable<string> GetAvailableProfiles() => _profiles.Keys;

        /// <summary>
        /// Pobierz konkretny profil wdrażania
        /// </summary>
        public DeploymentProfile GetProfile(string profileName)
        {
            if (_profiles.TryGetValue(profileName, out var profile))
            {
                return profile;
            }
            throw new KeyNotFoundException($"Profil '{profileName}' nie znaleziony");
        }

        /// <summary>
        /// Zweryfikuj konfigurację profilu wdrażania
        /// </summary>
        public ValidationResult ValidateProfile(string profileName)
        {
            try
            {
                var profile = GetProfile(profileName);
                var result = new ValidationResult { IsValid = true };

                // Sprawdzenie nazwy profilu
                if (string.IsNullOrWhiteSpace(profile.Name))
                    result.AddError("Nazwa profilu nie może być pusta");
                
                // Sprawdzenie aplikacji
                if (profile.Applications == null || !profile.Applications.Any())
                    result.AddWarning("Brak aplikacji zdefiniowanych w profilu");

                // Sprawdzenie ustawień systemowych
                if (profile.SystemSettings == null || !profile.SystemSettings.Any())
                    result.AddWarning("Brak ustawień systemowych zdefiniowanych w profilu");

                _logger.Information("Walidacja profilu '{ProfileName}': {Valid}", profileName, result.IsValid);
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Błąd podczas walidacji profilu: {ProfileName}", profileName);
                return new ValidationResult 
                { 
                    IsValid = false,
                    Errors = new[] { ex.Message }
                };
            }
        }

        /// <summary>
        /// Wykonaj profil wdrażania
        /// </summary>
        public async Task<DeploymentResult> ExecuteProfile(string profileName, bool dryRun = false)
        {
            var result = new DeploymentResult { ProfileName = profileName };
            
            try
            {
                _logger.Information("Rozpoczęcie wdrażania dla profilu: {ProfileName} (DryRun: {DryRun})", profileName, dryRun);
                
                // Pobierz i zweryfikuj profil
                var profile = GetProfile(profileName);
                var validation = ValidateProfile(profileName);
                
                if (!validation.IsValid)
                {
                    result.Success = false;
                    result.Errors = validation.Errors.ToList();
                    return result;
                }

                // Stosuj ustawienia systemowe
                await ApplySystemSettings(profile, dryRun, result);
                
                // Wdrażaj aplikacje
                await DeployApplications(profile, dryRun, result);

                result.Success = true;
                result.Timestamp = DateTime.UtcNow;
                
                _logger.Information("Wdrażanie ukończone pomyślnie dla profilu: {ProfileName}", profileName);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Errors.Add(ex.Message);
                _logger.Error(ex, "Wdrażanie nie powiodło się dla profilu: {ProfileName}", profileName);
            }

            return result;
        }

        /// <summary>
        /// Zastosuj ustawienia systemowe z profilu
        /// </summary>
        private async Task ApplySystemSettings(DeploymentProfile profile, bool dryRun, DeploymentResult result)
        {
            _logger.Information("Stosowanie {Count} ustawień systemowych", profile.SystemSettings.Count);
            
            foreach (var setting in profile.SystemSettings)
            {
                if (dryRun)
                {
                    _logger.Information("[SYMULACJA] Byłyby zastosowane ustawienia: {Setting}", setting.Key);
                    result.AppliedSettings.Add($"[SYMULACJA] {setting.Key}");
                }
                else
                {
                    // Rzeczywista implementacja byłaby tutaj
                    result.AppliedSettings.Add(setting.Key);
                    _logger.Debug("Zastosowano ustawienie: {Setting} = {Value}", setting.Key, setting.Value);
                }
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// Wdrażaj aplikacje z profilu
        /// </summary>
        private async Task DeployApplications(DeploymentProfile profile, bool dryRun, DeploymentResult result)
        {
            _logger.Information("Wdrażanie {Count} aplikacji", profile.Applications.Count);
            
            foreach (var app in profile.Applications)
            {
                if (dryRun)
                {
                    _logger.Information("[SYMULACJA] Byłaby wdrożona: {App}", app.Name);
                    result.DeployedApplications.Add($"[SYMULACJA] {app.Name}");
                }
                else
                {
                    // Rzeczywista implementacja byłaby tutaj
                    result.DeployedApplications.Add(app.Name);
                    _logger.Debug("Wdrożona aplikacja: {App} (Wersja: {Version})", app.Name, app.Version);
                }
            }
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// Profil wdrażania z ustawieniami systemowymi i aplikacjami
    /// </summary>
    public class DeploymentProfile
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("systemSettings")]
        public Dictionary<string, object> SystemSettings { get; set; } = new();

        [JsonProperty("applications")]
        public List<Application> Applications { get; set; } = new();

        [JsonProperty("priority")]
        public int Priority { get; set; } = 0;
    }

    /// <summary>
    /// Informacje o aplikacji
    /// </summary>
    public class Application
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("installPath")]
        public string InstallPath { get; set; }

        [JsonProperty("required")]
        public bool Required { get; set; } = true;
    }

    /// <summary>
    /// Wynik walidacji profilu
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();

        public void AddError(string error) 
        { 
            Errors.Add(error);
            IsValid = false;
        }

        public void AddWarning(string warning) => Warnings.Add(warning);
    }

    /// <summary>
    /// Wynik wykonanego wdrażania
    /// </summary>
    public class DeploymentResult
    {
        public string ProfileName { get; set; }
        public bool Success { get; set; }
        public DateTime Timestamp { get; set; }
        public List<string> AppliedSettings { get; set; } = new();
        public List<string> DeployedApplications { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }
}
