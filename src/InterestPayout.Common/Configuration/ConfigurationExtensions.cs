using System;
using System.IO;
using System.Reflection;
using Lykke.SettingsReader;
using Microsoft.Extensions.Configuration;

namespace InterestPayout.Common.Configuration
{
    public static class ConfigurationExtensions
    {
        private const string SettingsUrlDefaultKey = "RemoteSettingsUrls__0";
        private const string DefaultLocalConfigFile = "appsettings.json";
        
        public static IReloadingManagerWithConfiguration<AppConfig> LoadSettings(this IConfiguration configRoot)
        {
            if (configRoot == null)
                throw new ArgumentNullException(nameof(configRoot));

            var settingsUrl = configRoot[SettingsUrlDefaultKey];
            if (!string.IsNullOrWhiteSpace(settingsUrl))
                return configRoot.LoadSettings<AppConfig>(key: SettingsUrlDefaultKey);
            
            var currentAssembly = Assembly.GetEntryAssembly();
            if (currentAssembly == null)
                throw new ArgumentNullException(nameof(currentAssembly));
            
            var executionPath=  Path.GetDirectoryName(currentAssembly.Location);
            if(string.IsNullOrEmpty(executionPath))
                throw new ArgumentNullException(nameof(executionPath));
            
            return new LocalSettingsReloadingManager<AppConfig>(
                Path.Combine(executionPath, DefaultLocalConfigFile),
                null,
                true);
        }
    }
}
