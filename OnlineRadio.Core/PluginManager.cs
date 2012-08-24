using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

namespace OnlineRadio.Core
{
    class PluginManager : IDisposable
    {
        List<IPlugin> plugins = new List<IPlugin>();

        string path;

        public PluginManager()
        {
        }

        public void LoadPlugins(string path)
        {
            this.path = path;
            DirectoryInfo dir = new DirectoryInfo(path);
            if (!dir.Exists)
                return;
            Type pluginType = typeof(IPlugin);
            foreach (FileInfo fileInfo in dir.GetFiles("*.dll"))
            {
                try
                {
                    Assembly assembly = Assembly.LoadFile(fileInfo.FullName);
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
                    Console.WriteLine(ex.Message);
                }
            }
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
                plugin.OnCurrentSongChanged(sender, args);
        }
    }
}
