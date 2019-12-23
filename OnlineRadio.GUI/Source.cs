namespace OnlineRadio.GUI
{
    public class Source
    {
        public string Name
        {
            get;
            set;
        }

        public string Url
        {
            get;
            set;
        }

        public bool ArtistTitleOrderInverted
        {
            get;
            set;
        }

        public bool Selected
        {
            get;
            set;
        }

        public Source() { }

        public Source(string Name, string Url, bool ArtistTitleOrderInverted)
        {
            this.Name = Name;
            this.Url = Url;
            this.ArtistTitleOrderInverted = ArtistTitleOrderInverted;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
