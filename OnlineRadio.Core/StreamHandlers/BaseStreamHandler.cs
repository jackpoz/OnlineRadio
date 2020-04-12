using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace OnlineRadio.Core.StreamHandlers
{
    abstract class BaseStreamHandler : IDisposable
    {
        public static async Task<BaseStreamHandler> GetStreamHandler(string streamUrl, HttpClient httpClient)
        {
            if (streamUrl.EndsWith(".m3u", StringComparison.InvariantCultureIgnoreCase) || streamUrl.EndsWith(".m3u8", StringComparison.InvariantCultureIgnoreCase))
            {
                // Handle standard m3u streams (not extended m3u) as normal mp3 streams after retrieving the mp3 url
                var m3uStreamUrl = await TryGetStreamUrlFromM3U(streamUrl, httpClient);
                if (!string.IsNullOrEmpty(m3uStreamUrl))
                    return new MP3StreamHandler(m3uStreamUrl, httpClient);
                else
                    return new M3UStreamHandler(streamUrl, httpClient);
            }
            else
                return new MP3StreamHandler(streamUrl, httpClient);
        }

        static async Task<string> TryGetStreamUrlFromM3U(string streamUrl, HttpClient httpClient)
        {
            var result = await httpClient.GetStringAsync(streamUrl);

            var lines = result.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length == 0)
                throw new ArgumentException($"The specified m3u url points to an empty file");

            if (lines.First() == "#EXTM3U")
                return null;

            foreach (var line in lines)
            {
                // Skip comments
                if (line.StartsWith('#'))
                    continue;

                // The first line that is not a comment is the mp3 url
                return line;
            }

            return null;
        }

        protected readonly HttpClient Client;
        protected readonly string StreamUrl;

        public BaseStreamHandler(string streamUrl, HttpClient httpClient)
        {
            this.Client = httpClient;
            this.StreamUrl = streamUrl;
        }

        public abstract Task StartAsync();

        public abstract int GetIceCastMetaInterval();

        public abstract Task<(int bytesRead, byte[] buffer)> ReadAsync();

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls
        private List<IDisposable> objectsToDispose = new List<IDisposable>();

        protected void ToBeDisposed(IDisposable toBeDisposed)
        {
            objectsToDispose.Add(toBeDisposed);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects).
                    foreach (IDisposable toDispose in objectsToDispose)
                        toDispose.Dispose();

                    objectsToDispose.Clear();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
