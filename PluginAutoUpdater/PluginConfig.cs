using Exiled.API.Interfaces;
using PluginAutoUpdater.Enums;
using System.Collections.Generic;
using System.ComponentModel;

namespace PluginAutoUpdater
{
    public class PluginConfig : IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public string ApiEndpoint { get; set; } = "https://exiledplugins.kingsplayground.fun/api";
        [Description("Plugin blacklist, this plugins wont be updated automatically (Use the name of the plugin in the config [Prefix]).")]
        public List<string> PluginBlacklist { get; set; } = new List<string>()
        {
            "exampleplugin"
        };

        [Description("Action done after downloading new update.")]
        public UpdateAction UpdateAction { get; set; } = UpdateAction.RestartNowIfEmpty;

    }
}
