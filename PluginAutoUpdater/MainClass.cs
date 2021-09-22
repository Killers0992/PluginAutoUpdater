using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Interfaces;
using Exiled.Loader;
using PluginAutoUpdater.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Utf8Json;
using Utf8Json.Resolvers;

namespace PluginAutoUpdater
{
    public class MainClass : Plugin<PluginConfig>
    {
        public override string Name { get; } = "PluginAutoUpdater";
        public override string Author { get; } = "Killers0992";
        public override string Prefix { get; } = "pluginautoupdater";
        public override Version Version { get; } = new Version(1, 0, 2);
        public override PluginPriority Priority { get; } = PluginPriority.Last;

        string CalculateMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        public UpdatePluginResponse GetPluginListByURL(string url, CheckUpdatesModel model)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/json";

            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                streamWriter.Write(Encoding.UTF8.GetString(Utf8Json.JsonSerializer.Serialize(model)));
            }

            var webResponse = request.GetResponse();
            var webStream = webResponse.GetResponseStream();
            var responseReader = new StreamReader(webStream);
            var response = responseReader.ReadToEnd().Trim();
            UpdatePluginResponse deserializedClass = Utf8Json.JsonSerializer.Deserialize<UpdatePluginResponse>(response);
            responseReader.Close();
            return deserializedClass;
        }

        public override void OnEnabled()
        {
            var plugins = Loader.Plugins
                .Where(p => p != null)
                .ToDictionary<IPlugin<IConfig>, string>(p => CalculateMD5(p.GetPath()));
            Log.Info("Get updates from ApiEndpoint...");
            var updates = GetPluginListByURL($"{Config.ApiEndpoint}/checkpluginupdates", new CheckUpdatesModel()
            {
                ExiledVersion = Loader.Version.ToString(),
                SLVersion = GameCore.Version.VersionString,
                PluginHashes = plugins.Keys.ToList()
            });
            if (updates == null)
                return;
            int updatedPlugins = 0;
            foreach (var plugin in updates.pluginsToUpdate)
            {
                var linkedPlugin = plugins[plugin.Key];
                if (Config.PluginBlacklist.Contains(linkedPlugin.Prefix))
                {
                    Log.Info($"Skip update for plugin \"{linkedPlugin.Name}\".");
                    continue;
                }
                Log.Info($"Updating plugin \"{linkedPlugin.Name}\" from version {linkedPlugin.Version} to {plugin.Value.newVersion}.");
                File.Copy(linkedPlugin.GetPath(), $"{linkedPlugin.GetPath()}.bak");

                File.Delete(linkedPlugin.GetPath());
                using (var client = new WebClient())
                {
                    client.DownloadFile(plugin.Value.downloadURL, linkedPlugin.GetPath());
                }
                Log.Info($"{linkedPlugin.Name} was updated successfully!");
                updatedPlugins++;
                File.Delete($"{linkedPlugin.GetPath()}.bak");
            }
            Log.Info("Checking plugins updates ended.");
            /*if (updatedPlugins != 0)
            {
                Server.Restart();
            } */
        }
    }
}
