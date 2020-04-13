using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace OnlineRadio.Core.StreamHandlers
{
    class MP3StreamHandler : BaseStreamHandler
    {
        HttpResponseMessage response;
        Stream socketStream;
        byte[] buffer = new byte[16384];

        public MP3StreamHandler(string streamUrl, HttpClient httpClient)
            : base(streamUrl, httpClient)
        {
        }

        public async override Task StartAsync()
        {
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(StreamUrl),
                Method = HttpMethod.Get,
                Headers =
                {
                    { "icy-metadata", "1" }
                }
            };

            response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            ToBeDisposed(response);

            socketStream = await response.Content.ReadAsStreamAsync();
            ToBeDisposed(socketStream);
        }

        public override int GetIceCastMetaInterval()
        {
            if (response.Headers.TryGetValues("icy-metaint", out var icyHeader))
                return Convert.ToInt32(icyHeader.FirstOrDefault() ?? "0");
            else
                return 0;
        }

        public override async Task<(int bytesRead, byte[] buffer)> ReadAsync()
        {
            var bytesRead = await socketStream.ReadAsync(buffer, 0, buffer.Length);

            return (bytesRead, buffer);
        }
    }
}
