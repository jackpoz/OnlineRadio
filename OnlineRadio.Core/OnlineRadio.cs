using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineRadio.Core
{
    public class OnlineRadio
    {
        string Url
        {
            get;
            private set;
        }

        public OnlineRadio(string Url)
        {
            this.Url = Url;
        }
    }
}
