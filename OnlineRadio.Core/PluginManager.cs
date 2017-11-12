using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace OnlineRadio.Core
{
    class PluginManager : IDisposable
    {
        List<IPlugin> plugins = new List<IPlugin>();

        string path;

        public List<IPlugin> LoadPlugins(string path)
        {
            this.path = path;
            DirectoryInfo dir = new DirectoryInfo(path);
            if (!dir.Exists)
                return plugins;
            Type pluginType = typeof(IPlugin);
            foreach (FileInfo fileInfo in dir.GetFiles("*.dll"))
            {
                try
                {
                    Assembly assembly = Assembly.LoadFrom(fileInfo.FullName);
                    foreach (var type in assembly.GetTypes())
                    {
                        if (!type.IsInterface &&
                            !type.IsAbstract &&
                            pluginType.IsAssignableFrom(type))
                        {
                            IPlugin plugin = (IPlugin)Activator.CreateInstance(type);
                            plugins.Add(plugin);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Radio.Log(ex.Message, this);
                }
            }

            return plugins;
        }

        public void UnloadPlugins()
        {
            foreach (var plugin in plugins)
            {
                IDisposable disposablePlugin = plugin as IDisposable;
                if (disposablePlugin != null)
                    disposablePlugin.Dispose();
            }
            plugins.Clear();
        }

        public void ReloadPlugins()
        {
            UnloadPlugins();
            LoadPlugins(path);
        }

        public void Dispose()
        {
            UnloadPlugins();
        }

        public void OnCurrentSongChanged(object sender, CurrentSongEventArgs args)
        {
            foreach (var plugin in plugins)
                Task.Run(()=>plugin.OnCurrentSongChanged(sender, args));
        }

        public void OnStreamUpdate(object sender, StreamUpdateEventArgs args)
        {
            foreach (var plugin in plugins)
                plugin.OnStreamUpdate(sender, args);
        }

        public void OnStreamOver(object sender, StreamOverEventArgs args)
        {
            foreach (var plugin in plugins)
                plugin.OnStreamOver(sender, args);
        }
    }
}
