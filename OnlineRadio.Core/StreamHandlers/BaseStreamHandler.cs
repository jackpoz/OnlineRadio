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
            if (streamUrl.EndsWith(".m3u", StringComparison.InvariantCultureIgnoreCase))
            {
                // Handle standard m3u streams (not extended m3u) as normal mp3 streams after retrieving the mp3 url
                var m3uStreamUrl = await GetStreamUrlFromM3U(streamUrl, httpClient);
                return new MP3StreamHandler(m3uStreamUrl, httpClient);                    
            }
            else if (streamUrl.EndsWith(".m3u8", StringComparison.InvariantCultureIgnoreCase))
                return new M3U8StreamHandler(streamUrl, httpClient);
            else
                return new MP3StreamHandler(streamUrl, httpClient);
        }

        static async Task<string> GetStreamUrlFromM3U(string streamUrl, HttpClient httpClient)
        {
            var result = await httpClient.GetStringAsync(streamUrl).ConfigureAwait(false);

            var lines = result.Split(new [] { Environment.NewLine, "\n" }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length == 0)
                throw new ArgumentException("The specified m3u url points to an empty file");

            if (lines.First() == "#EXTM3U")
                throw new NotSupportedException("Extended m3u files are not supported");

            foreach (var line in lines)
            {
                // Skip comments
                if (line.StartsWith('#'))
                    continue;

                // The first line that is not a comment is the mp3 url
                return line;
            }

            throw new InvalidDataException("The specified m3u points to a document with no stream url");
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

        public abstract string GetCodec();

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
