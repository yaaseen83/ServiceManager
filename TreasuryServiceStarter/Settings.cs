using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceManagerSettings
{
    public class Settings
    {
        // Default value in case the setting is missing from the config file
        public string LogFolderPath { get; set; } = "logs";

        // Default to 30 minutes
        public int ServiceCheckIntervalMinutes { get; set; } = 30;

        // Default to 23 hours
        public int AppRunDurationHours { get; set; } = 23;
    }
}
