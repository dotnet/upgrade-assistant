using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using AspNetMigrator.Portability.Analyzers;
using Microsoft.Extensions.Logging;

namespace AspNetMigrator.Portability
{
    internal class PortabilityComponents
    {
        private static string _componentConfigPath = "./Config/PortabilityComponentConfiguration.json";

        public static Dictionary<string, string>? GetPortabilityComponentsConfig(ILogger<PortabilityServiceAnalyzer> logger)
        {
            try
            {
                logger.LogInformation("Reading Portability Component Config from {path}", _componentConfigPath);
                string json = File.ReadAllText(_componentConfigPath);
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error Reading Portability Component Config");
                return new Dictionary<string, string>();
            }
        }
    }
}
