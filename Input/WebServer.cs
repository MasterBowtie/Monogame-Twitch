using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;


namespace TwitchAuthExample
{
    public class WebServer
    {
        private HttpListener listener;

        public WebServer(string uri)
        {
            listener = new HttpListener();
            listener.Prefixes.Add(uri);
        }

        public async Task<Authorization> Listen()
        {
            listener.Start();
            return await onRequest();
        }

        private async Task<Authorization> onRequest()
        {
            while (listener.IsListening)
            {
                var ctx = await listener.GetContextAsync();
                var req = ctx.Request;
                var resp = ctx.Response;

                using (var writer = new StreamWriter(resp.OutputStream))
                {

                    // foreach (var key in req.QueryString.AllKeys)
                    // {
                    //     System.Console.WriteLine($"{key} = {req.QueryString[key]}");
                    //     writer.WriteLine($"{key} = {req.QueryString[key]}");
                    // }
                    if (req.QueryString.AllKeys.Any("code".Contains))
                    {
                        // writer.WriteLine(req.QueryString["code"]);
                        writer.WriteLine("Authorization started! Check your application!");
                        // writer.Flush();
                        return new Authorization(req.QueryString["code"]);
                    }
                    else
                    {
                        writer.WriteLine("No code found in query string!");
                        writer.Flush();
                    }
                }
            }
            return null;
        }
    }
    public class Authorization
    {
        public string Code { get; }
        
        public Authorization(string code)
        {
            Code = code;
        }
    }
}
