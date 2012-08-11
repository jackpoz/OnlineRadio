using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineRadio.Core
{
    public class Radio : IDisposable
    {
        public string Url
        {
            get;
            private set;
        }

        public Radio(string Url)
        {
            this.Url = Url;
        }

        bool Start()
        {
            return false;
        }

        public void Dispose()
        {
        }
    }
}
