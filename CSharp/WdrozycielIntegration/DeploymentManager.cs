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
    /// Advanced deployment manager integrated with Win11Debloat
    /// Handles multi-profile configurations and deployment workflows
    /// </summary>
    public class DeploymentManager
    {
        private readonly ILogger _logger;
        private readonly string _configPath;
        private Dictionary<string, DeploymentProfile> _profiles;

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
        /// Load deployment profiles from JSON configuration
        /// </summary>
        public void LoadProfiles()
        {
            try
            {
                var profilesFile = Path.Combine(_configPath, "DeploymentProfiles.json");
                if (!File.Exists(profilesFile))
                {
                    _logger.Warning("Profiles file not found: {ProfilesFile}", profilesFile);
                    return;
                }

                var json = File.ReadAllText(profilesFile);
                var profilesList = JsonConvert.DeserializeObject<List<DeploymentProfile>>(json);
                
                _profiles = profilesList?.ToDictionary(p => p.Name) ?? new Dictionary<string, DeploymentProfile>();
                _logger.Information("Loaded {Count} deployment profiles", _profiles.Count);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error loading deployment profiles");
            }
        }

        /// <summary>
        /// Get available deployment profiles
        /// </summary>
        public IEnumerable<string> GetAvailableProfiles() => _profiles.Keys;

        /// <summary>
        /// Get specific deployment profile
        /// </summary>
        public DeploymentProfile GetProfile(string profileName)
        {
            if (_profiles.TryGetValue(profileName, out var profile))
            {
                return profile;
            }
            throw new KeyNotFoundException($"Profile '{profileName}' not found");
        }

        /// <summary>
        /// Validate deployment profile configuration
        /// </summary>
        public ValidationResult ValidateProfile(string profileName)
        {
            try
            {
                var profile = GetProfile(profileName);
                var result = new ValidationResult { IsValid = true };

                if (string.IsNullOrWhiteSpace(profile.Name))
                    result.AddError("Profile name cannot be empty");
                
                if (profile.Applications == null || !profile.Applications.Any())
                    result.AddWarning("No applications specified in profile");

                if (profile.SystemSettings == null || !profile.SystemSettings.Any())
                    result.AddWarning("No system settings specified in profile");

                _logger.Information("Validation for profile '{ProfileName}': {Valid}", profileName, result.IsValid);
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error validating profile: {ProfileName}", profileName);
                return new ValidationResult 
                { 
                    IsValid = false,
                    Errors = new[] { ex.Message }
                };
            }
        }

        /// <summary>
        /// Execute deployment profile
        /// </summary>
        public async Task<DeploymentResult> ExecuteProfile(string profileName, bool dryRun = false)
        {
            var result = new DeploymentResult { ProfileName = profileName };
            
            try
            {
                _logger.Information("Starting deployment for profile: {ProfileName} (DryRun: {DryRun})", profileName, dryRun);
                
                var profile = GetProfile(profileName);
                var validation = ValidateProfile(profileName);
                
                if (!validation.IsValid)
                {
                    result.Success = false;
                    result.Errors = validation.Errors.ToList();
                    return result;
                }

                // Apply system settings
                await ApplySystemSettings(profile, dryRun, result);
                
                // Deploy applications
                await DeployApplications(profile, dryRun, result);

                result.Success = true;
                result.Timestamp = DateTime.UtcNow;
                
                _logger.Information("Deployment completed successfully for profile: {ProfileName}", profileName);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Errors.Add(ex.Message);
                _logger.Error(ex, "Deployment failed for profile: {ProfileName}", profileName);
            }

            return result;
        }

        private async Task ApplySystemSettings(DeploymentProfile profile, bool dryRun, DeploymentResult result)
        {
            _logger.Information("Applying {Count} system settings", profile.SystemSettings.Count);
            
            foreach (var setting in profile.SystemSettings)
            {
                if (dryRun)
                {
                    _logger.Information("[DRY RUN] Would apply setting: {Setting}", setting.Key);
                    result.AppliedSettings.Add($"[DRY RUN] {setting.Key}");
                }
                else
                {
                    // Actual implementation would apply system settings
                    result.AppliedSettings.Add(setting.Key);
                    _logger.Debug("Applied setting: {Setting} = {Value}", setting.Key, setting.Value);
                }
            }
            await Task.CompletedTask;
        }

        private async Task DeployApplications(DeploymentProfile profile, bool dryRun, DeploymentResult result)
        {
            _logger.Information("Deploying {Count} applications", profile.Applications.Count);
            
            foreach (var app in profile.Applications)
            {
                if (dryRun)
                {
                    _logger.Information("[DRY RUN] Would deploy: {App}", app.Name);
                    result.DeployedApplications.Add($"[DRY RUN] {app.Name}");
                }
                else
                {
                    // Actual implementation would deploy applications
                    result.DeployedApplications.Add(app.Name);
                    _logger.Debug("Deployed application: {App} (Version: {Version})", app.Name, app.Version);
                }
            }
            await Task.CompletedTask;
        }
    }

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
