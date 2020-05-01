using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OnlineRadio.Core.StreamHandlers
{
    class M3U8StreamHandler : BaseStreamHandler
    {
        private const string CodecPattern = @"CODECS=""(?<codec>[^.]+).+""";

        string subStreamUrl;
        Queue<string> chunkStreamUrls = new Queue<string>();
        string codec;

        HttpResponseMessage response;
        Stream socketStream;
        byte[] buffer = new byte[16384];

        public M3U8StreamHandler(string streamUrl, HttpClient httpClient)
            : base(streamUrl, httpClient)
        {
        }

        public async override Task StartAsync()
        {
            subStreamUrl = await GetSubStreamUrlAsync();
            await RefreshChunkStreamUrlsAsync();
            await ReadNextChunkAsync();
        }

        async Task<string> GetSubStreamUrlAsync()
        {
            var result = await Client.GetStringAsync(StreamUrl);

            var lines = result.Split(new[] { Environment.NewLine, "\n" }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length == 0)
                throw new ArgumentException("The specified m3u8 url points to an empty file");

            if (lines.First() != "#EXTM3U")
                throw new ArgumentException("The specified m3u8 url is not an extended m3u file");

            foreach (var line in lines.Skip(1))
            {
                // Skip comments
                if (line.StartsWith('#'))
                {
                    var match = Regex.Match(line, CodecPattern);
                    if (match.Success)
                        codec = match.Groups["codec"].Value;
                    continue;
                }

                // The first line that is not a comment is the mp3 url
                return line;
            }

            throw new ArgumentException("The specified m3u url ");
        }

        async Task RefreshChunkStreamUrlsAsync()
        {
            var result = await Client.GetStringAsync(subStreamUrl);

            var lines = result.Split(new[] { Environment.NewLine, "\n" }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.First() != "#EXTM3U")
                throw new ArgumentException("The specified m3u8 sub stream url is not an extended m3u file");

            foreach (var line in lines.Skip(1))
            {
                // Skip comments
                if (line.StartsWith('#'))
                    continue;

                if (!chunkStreamUrls.Contains(line))
                    chunkStreamUrls.Enqueue(line);
            }

            if (chunkStreamUrls.Count == 0)
                throw new ArgumentException("No chunk stream urls found");
        }

        async Task ReadNextChunkAsync()
        {
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(chunkStreamUrls.Peek()),
                Method = HttpMethod.Get
            };

            response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            socketStream = await response.Content.ReadAsStreamAsync();
        }

        async Task OnChunkFinishedReadingAsync()
        {
            chunkStreamUrls.Dequeue();

            // Buffer the list of chunks if we have only 1 left
            if (chunkStreamUrls.Count <= 1)
                await RefreshChunkStreamUrlsAsync();

            await ReadNextChunkAsync();
        }

        public override int GetIceCastMetaInterval()
        {
            return 0;
        }

        public override async Task<(int bytesRead, byte[] buffer)> ReadAsync()
        {
            var bytesRead = await socketStream.ReadAsync(buffer, 0, buffer.Length);

            if (bytesRead == 0)
            {
                await OnChunkFinishedReadingAsync();
                return await ReadAsync();
            }

            return (bytesRead, buffer);
        }

        public override string GetCodec()
        {
            return codec;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    response?.Dispose();
                    socketStream?.Dispose();
                }

                disposedValue = true;
            }

            base.Dispose(disposing);
        }
        #endregion
    }
}
