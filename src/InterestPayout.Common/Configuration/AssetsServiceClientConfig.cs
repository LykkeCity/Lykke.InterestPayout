using System;
using Lykke.SettingsReader.Attributes;

namespace InterestPayout.Common.Configuration
{
    public class AssetsServiceClientConfig
    {
        [HttpCheck("api/isalive")]
        public string ServiceUrl { get; set; }
    }
}
