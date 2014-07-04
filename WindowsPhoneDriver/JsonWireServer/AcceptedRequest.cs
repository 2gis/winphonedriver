using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace WindowsPhoneJsonWireServer {
    class AcceptedRequest {
        public String request { get; set; }
        public Dictionary<String, String> headers { get; set; }
        public String content { get; set; }

        public AcceptedRequest() {
            this.request = String.Empty;
            this.headers = new Dictionary<string, string>();
            this.content = String.Empty;
        }

        public async Task AcceptRequest(DataReader reader) {

            this.request = await StreamReadLine(reader);

            //read HTTP headers
            this.headers = await ReadHeaders(reader);

            //read request contents
            uint contentLength = GetContentLength(headers);
            String content = String.Empty;
            if (contentLength != 0) {
                this.content = await ReadContent(reader, contentLength);
            }
        }

        private async Task<Dictionary<String, String>> ReadHeaders(DataReader reader) {
            var headers = new Dictionary<string, string>();
            String header;
            while (!String.IsNullOrEmpty(header = await StreamReadLine(reader))) {
                String[] splitHeader;
                splitHeader = header.Split(':');
                headers.Add(splitHeader[0], splitHeader[1].Trim(' '));
            }
            return headers;
        }

        private uint GetContentLength(Dictionary<String, String> headers) {
            uint contentLength = 0;
            String contentLengthString;
            bool hasContentLength = headers.TryGetValue("Content-Length", out contentLengthString);
            if (hasContentLength) {
                contentLength = Convert.ToUInt32(contentLengthString);
            }
            return contentLength;
        }

        private async Task<String> ReadContent(DataReader reader, uint contentLength) {
            await reader.LoadAsync(contentLength);
            String content = reader.ReadString(contentLength);
            return content;
        }

        private static async Task<String> StreamReadLine(DataReader reader) {
            int next_char;
            String data = "";
            while (true) {
                await reader.LoadAsync(1);
                next_char = reader.ReadByte();
                if (next_char == '\n') { break; }
                if (next_char == '\r') { continue; }
                data += Convert.ToChar(next_char);
            }
            return data;
        }
    }
}
