using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            throw new NotImplementedException();
        }

        public void UnloadPlugins()
        {
            throw new NotImplementedException();
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
