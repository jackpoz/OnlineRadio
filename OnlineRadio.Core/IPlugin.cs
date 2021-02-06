namespace OnlineRadio.Core
{
    public interface IPlugin
    {
        string Name
        {
            get;
        }

        void OnCurrentSongChanged(object sender, CurrentSongEventArgs args);

        void OnStreamStart(object sender, StreamStartEventArgs args);

        void OnStreamUpdate(object sender, StreamUpdateEventArgs args);

        void OnStreamOver(object sender, StreamOverEventArgs args);

        void OnVolumeUpdate(object sender, VolumeUpdateEventArgs args);
    }

    public abstract class BasePlugin : IPlugin
    {
        public string Name
        {
            get 
            { 
                return "BasePlugin";
            }
        }

        public void OnCurrentSongChanged(object sender, CurrentSongEventArgs args)
        {
            // Do nothing
        }

        public void OnStreamStart(object sender, StreamStartEventArgs args)
        {
            // Do nothing
        }

        public void OnStreamUpdate(object sender, StreamUpdateEventArgs args)
        {
            // Do nothing
        }

        public void OnStreamOver(object sender, StreamOverEventArgs args)
        {
            // Do nothing
        }

        public void OnVolumeUpdate(object sender, VolumeUpdateEventArgs args)
        {
            // Do nothing
        }
    }
}
