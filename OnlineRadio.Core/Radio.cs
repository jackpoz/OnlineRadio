using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace OnlineRadio.Core
{
    public class Radio : IDisposable
    {

        public string Url
        {
            get;
            private set;
        }
        public bool Running
        {
            get;
            set;
        }
        public string Metadata
        {
            get
            {
                return _metadata;
            }
            private set
            {
                if (OnMetadataChanged != null)
                    OnMetadataChanged(_metadata, value);
                _metadata = value;
            }
        }
        string _metadata;

        public event Action<string, string> OnMetadataChanged;

        public Radio(string Url)
        {
            this.Url = Url;
        }

        public void Start()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
            request.Headers.Add("icy-metadata", "1");
            Running = true;
            request.BeginGetResponse(GotResponse, request);
        }

        void GotResponse(IAsyncResult result)
        {
            HttpWebRequest request = (HttpWebRequest)result.AsyncState;
            using (HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(result))
            {
                //get the position of metadata
                int metaInt = Convert.ToInt32(response.GetResponseHeader("icy-metaint"));
                using (Stream socketStream = response.GetResponseStream())
                {
                    byte[] buffer = new byte[16384];
                    int metadataLength = 0;
                    int streamPosition = 0;
                    int index = -1;
                    StringBuilder metadataSb = new StringBuilder();
                    int bytes = 0;

                    while (Running)
                    {
                        bytes = socketStream.Read(buffer, 0, buffer.Length);
                        if (bytes < 0)
                            throw new Exception("Nothing read");

                        if (metadataLength == 0)
                        {
                            if (streamPosition + bytes <= metaInt)
                            {
                                streamPosition += bytes;
                                continue;
                            }

                            index = metaInt - streamPosition;
                            metadataLength = Convert.ToInt32(buffer[index]) * 16;
                            //check if there's any metadata, otherwise skip to next block
                            if (metadataLength == 0)
                            {
                                streamPosition = bytes - index - 1;
                                continue;
                            }
                            index++;
                        }

                        //get the metadata and reset the position
                        for (; index < bytes; index++)
                        {
                            metadataSb.Append(Convert.ToChar(buffer[index]));
                            metadataLength--;
                            if (metadataLength == 0)
                            {
                                Metadata = metadataSb.ToString();
                                metadataSb.Clear();
                                streamPosition = bytes - index - 1;
                                break;
                            }
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            Running = false;
        }
    }
}
