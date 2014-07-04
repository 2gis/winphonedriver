using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsPhoneJsonWireServer {
    class Parser {
        public static String GetRequestCommand(String request) {
            String[] requestTokens = request.Split(' ');
            String urn = requestTokens[1];
            //take care of the slash in the end of the string
            String[] urnTokens = urn.Split(new String[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
            String command = urnTokens[urnTokens.Length - 1];
            return command;
        }

        public static String GetElementId(String request) {
            String[] requestTokens = request.Split(' ');
            String urn = requestTokens[1];
            //take care of the slash in the end of the string
            String[] urnTokens = urn.Split(new String[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
            String elementId = urnTokens[urnTokens.Length - 2];
            return elementId;
        }

        public static String GetKeysString(String requestContent) {
            String result = String.Empty;
            JsonKeysContent jsonContent = JsonConvert.DeserializeObject<JsonKeysContent>(requestContent);
            String[] value = jsonContent.GetValue();
            foreach (String str in value) {
                result += str;
            }
            return result;
        }

        public static int GetUrnTokensLength(String request) {
            String[] requestTokens = request.Split(' ');
            String urn = requestTokens[1];
            //take care of the slash in the end of the string
            String[] urnTokens = urn.Split(new String[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
            return urnTokens.Length;
        }
    }
}
