using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public bool Selected
        {
            get;
            set;
        }

        public Source() { }

        public Source(string Name, string Url)
        {
            this.Name = Name;
            this.Url = Url;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
