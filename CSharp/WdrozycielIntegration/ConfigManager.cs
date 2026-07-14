using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Serilog;

namespace WdrozycielIntegration
{
    /// <summary>
    /// Configuration manager for handling deployment configurations
    /// Supports JSON-based configuration files with validation
    /// </summary>
    public class ConfigManager
    {
        private readonly ILogger _logger;
        private readonly string _configDirectory;
        private Dictionary<string, object> _cachedConfig;

        public ConfigManager(string configDirectory = null)
        {
            _configDirectory = configDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
            _logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();
            
            EnsureConfigDirectoryExists();
        }

        /// <summary>
        /// Load configuration from JSON file
        /// </summary>
        public T LoadConfiguration<T>(string configFileName) where T : class
        {
            try
            {
                var configPath = Path.Combine(_configDirectory, configFileName);
                
                if (!File.Exists(configPath))
                {
                    _logger.Warning("Configuration file not found: {ConfigPath}", configPath);
                    return null;
                }

                var json = File.ReadAllText(configPath);
                var config = JsonConvert.DeserializeObject<T>(json);
                
                _logger.Information("Loaded configuration from: {ConfigPath}", configPath);
                return config;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error loading configuration: {ConfigFileName}", configFileName);
                return null;
            }
        }

        /// <summary>
        /// Save configuration to JSON file
        /// </summary>
        public bool SaveConfiguration<T>(string configFileName, T config) where T : class
        {
            try
            {
                var configPath = Path.Combine(_configDirectory, configFileName);
                var json = JsonConvert.SerializeObject(config, Formatting.Indented);
                
                File.WriteAllText(configPath, json);
                _logger.Information("Saved configuration to: {ConfigPath}", configPath);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error saving configuration: {ConfigFileName}", configFileName);
                return false;
            }
        }

        /// <summary>
        /// Get configuration file path
        /// </summary>
        public string GetConfigFilePath(string configFileName)
        {
            return Path.Combine(_configDirectory, configFileName);
        }

        /// <summary>
        /// List all configuration files
        /// </summary>
        public IEnumerable<string> ListConfigFiles()
        {
            try
            {
                if (!Directory.Exists(_configDirectory))
                    return Enumerable.Empty<string>();

                return Directory.GetFiles(_configDirectory, "*.json")
                    .Select(Path.GetFileName);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error listing configuration files");
                return Enumerable.Empty<string>();
            }
        }

        /// <summary>
        /// Validate configuration structure
        /// </summary>
        public ConfigValidationResult ValidateConfiguration<T>(string configFileName, T expectedStructure) where T : class
        {
            var result = new ConfigValidationResult { IsValid = true };

            try
            {
                var config = LoadConfiguration<T>(configFileName);
                
                if (config == null)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Configuration file not found: {configFileName}");
                    return result;
                }

                // Additional validation can be implemented here
                _logger.Information("Configuration validation passed: {ConfigFileName}", configFileName);
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add(ex.Message);
                _logger.Error(ex, "Configuration validation failed: {ConfigFileName}", configFileName);
            }

            return result;
        }

        /// <summary>
        /// Merge configurations
        /// </summary>
        public Dictionary<string, object> MergeConfigurations(params Dictionary<string, object>[] configs)
        {
            var merged = new Dictionary<string, object>();
            
            foreach (var config in configs)
            {
                if (config == null) continue;
                
                foreach (var kvp in config)
                {
                    merged[kvp.Key] = kvp.Value;
                }
            }

            _logger.Information("Merged {Count} configuration dictionaries", configs.Length);
            return merged;
        }

        /// <summary>
        /// Create default configuration structure
        /// </summary>
        public void CreateDefaultConfigurations()
        {
            try
            {
                // Default Deployment Profiles
                var defaultProfiles = new List<object>
                {
                    new
                    {
                        name = "Minimal",
                        description = "Minimal deployment with core applications only",
                        priority = 1,
                        systemSettings = new { }
                        {
                            telemetryEnabled = false,
                            darkModeEnabled = true
                        },
                        applications = new List<object>()
                    },
                    new
                    {
                        name = "Standard",
                        description = "Standard deployment with common applications",
                        priority = 2,
                        systemSettings = new { }
                        {
                            telemetryEnabled = false,
                            darkModeEnabled = true,
                            updatesBehavior = "delayed"
                        },
                        applications = new List<object>()
                    },
                    new
                    {
                        name = "Full",
                        description = "Full deployment with all available applications",
                        priority = 3,
                        systemSettings = new { }
                        {
                            telemetryEnabled = false,
                            darkModeEnabled = true,
                            updatesBehavior = "immediate"
                        },
                        applications = new List<object>()
                    }
                };

                SaveConfiguration("DeploymentProfiles.json", defaultProfiles);
                
                _logger.Information("Created default deployment profiles");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error creating default configurations");
            }
        }

        private void EnsureConfigDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(_configDirectory))
                {
                    Directory.CreateDirectory(_configDirectory);
                    _logger.Information("Created configuration directory: {ConfigDirectory}", _configDirectory);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error ensuring config directory exists");
            }
        }
    }

    public class ConfigValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }
}
