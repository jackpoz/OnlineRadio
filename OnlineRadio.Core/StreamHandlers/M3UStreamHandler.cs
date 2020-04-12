using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace OnlineRadio.Core.StreamHandlers
{
    class M3UStreamHandler : BaseStreamHandler
    {
        public M3UStreamHandler(string streamUrl, HttpClient httpClient)
            : base(streamUrl, httpClient)
        {
        }

        public async override Task StartAsync()
        {
            throw new NotImplementedException();
        }

        public override int GetIceCastMetaInterval()
        {
            throw new NotImplementedException();
        }

        public override async Task<(int bytesRead, byte[] buffer)> ReadAsync()
        {
            throw new NotImplementedException();
        }
    }
}
