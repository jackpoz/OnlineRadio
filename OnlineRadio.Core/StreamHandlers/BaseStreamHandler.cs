using System;
using System.Collections.Generic;
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
                return new M3UStreamHandler(streamUrl, httpClient);
            }
            else
                return new MP3StreamHandler(streamUrl, httpClient);
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
