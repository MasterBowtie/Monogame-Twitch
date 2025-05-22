using System;
using System.Collections.Generic;
using System.Threading;
using TwitchAuthExample;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Models;
using TwitchLib.Client;
using TwitchLib.Communication.Clients;
using TwitchLib.Api.Auth;
using System.Threading.Tasks;


// https://github.com/swiftyspiffy/Twitch-Auth-Example

namespace bowtie
{
    public class TwitchSocket
    {
        TwitchAPI api;
        WebServer server;
        List<string> scopes;
        User user;
        AccessToken accessToken;
        TwitchClient client;

        public TwitchSocket()
        {
            api = new TwitchAPI();
            api.Settings.ClientId = Environment.GetEnvironmentVariable("CLIENT_ID");
            server = new WebServer(Environment.GetEnvironmentVariable("REDIRECT_URL"));
            // scopes = ["channel:bot", "user:read:chat"];
            scopes = ["chat:read", "chat:edit"];
        }

        public async void connect()
        {
            while (user == null)
            {
                Thread.Sleep(1000);
            }
            // System.Console.WriteLine($"User: {user.Login}");
            // System.Console.WriteLine($"User: {user.DisplayName}");
            System.Console.WriteLine("Connecting to Twitch");

            ConnectionCredentials credentials = new ConnectionCredentials(user.Login.ToLower(), $"oauth:{api.Settings.AccessToken}");

            ClientOptions clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };

            var customClient = new WebSocketClient(clientOptions);
            this.client = new TwitchClient(customClient);
            client.Initialize(credentials, user.Login);

            // Events
            client.OnLog += onLog;
            client.OnConnected += onConnect;
            client.OnMessageReceived += onMessageReceived;

            // Connect
            client.Connect();
        }

        public bool isConnected()
        {
            // System.Console.WriteLine($"Client {client != null}:{(client != null ? client.IsConnected : false)}");
            return client != null && client.IsConnected;
        }

        public void disconnect()
        {
            client.Disconnect();
        }

        public async void refreshAuthorization(string username, AccessToken accessToken)
        {
            var refresh = await api.Auth.RefreshAuthTokenAsync(accessToken.refresh_token, Environment.GetEnvironmentVariable("CLIENT_SECRET"));
            lock (this)
            {
                api.Settings.AccessToken = refresh.AccessToken;
            }
        }

        public async void getAuthorized()
        {
            List<string> scopeEncoded = [];
            foreach (string scope in scopes)
            {
                scopeEncoded.Add(System.Web.HttpUtility.UrlEncode(scope));
            }

            string clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
            string redirectUri = Environment.GetEnvironmentVariable("REDIRECT_URL");

            var urlString = "https://id.twitch.tv/oauth2/authorize?" +
                   $"client_id={clientId}&" +
                   $"redirect_uri={System.Web.HttpUtility.UrlEncode(redirectUri)}&" +
                   "response_type=code&" +
                   $"scope={String.Join("%20", scopeEncoded)}";

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = urlString,
                    UseShellExecute = true
                });
            }
            catch
            {
                System.Console.WriteLine("Error: Unable to open Authorization");
            }

            var auth = await server.Listen();
            AuthCodeResponse resp = await api.Auth.GetAccessTokenFromCodeAsync(auth.Code, Environment.GetEnvironmentVariable("CLIENT_SECRET"), Environment.GetEnvironmentVariable("REDIRECT_URL"));

            api.Settings.AccessToken = resp.AccessToken;
            User user = (await api.Helix.Users.GetUsersAsync()).Users[0];
            lock (this)
            {
                this.user = user;
            }

            DateTime temp = DateTime.Now + new TimeSpan(resp.ExpiresIn);

            accessToken = new AccessToken()
            {
                access_token = resp.AccessToken,
                refresh_token = resp.RefreshToken,
                expires = temp.Ticks,
                userId = uint.Parse(user.Id)
            };

            // Console.WriteLine($"Authorization success!\n\nUser: {user.DisplayName} (id: {user.Id})\nAccess token: {resp.AccessToken}\nRefresh token: {resp.RefreshToken}\nExpires in: {resp.ExpiresIn}\nScopes: {string.Join(", ", resp.Scopes)}");

            // refresh token
            // var refresh = await api.Auth.RefreshAuthTokenAsync(resp.RefreshToken, Environment.GetEnvironmentVariable("CLIENT_SECRET"));
            // api.Settings.AccessToken = refresh.AccessToken;

            // // confirm new token works
            // temp = (await api.Helix.Users.GetUsersAsync()).Users[0];
            // lock (this)
            // {
            //     user = temp;
            // }

            // print out all the data we've got
            // Console.WriteLine($"Authorization success!\n\nUser: {user.DisplayName} (id: {user.Id})\nAccess token: {refresh.AccessToken}\nRefresh token: {refresh.RefreshToken}\nExpires in: {refresh.ExpiresIn}\nScopes: {string.Join(", ", refresh.Scopes)}");

            System.Console.WriteLine("Authorization success!");
            connect();
        }

        public async Task setAccessToken(AccessToken token)
        {
            this.accessToken = token;
            System.Console.WriteLine($"Access Token: {token.access_token}");
            DateTime expireDate = new DateTime(token.expires);
            if (expireDate < DateTime.Now)
            {
                api.Settings.AccessToken = token.refresh_token;
                System.Console.WriteLine("Expired Token");
            }
            else
            {
                api.Settings.AccessToken = token.access_token;
            }
            User user = (await api.Helix.Users.GetUsersAsync()).Users[0];
            System.Console.WriteLine($"Connected to: {user.DisplayName}");
            lock (this)
            {
                this.user = user;
            }
        }

        public AccessToken getAccessToken()
        {
            if (accessToken == null)
            {
                return null;
            }
            return accessToken;
        }

        private void onLog(object sender, OnLogArgs e)
        {
            // Console.WriteLine($"{e.DateTime.ToString()}: {e.BotUsername} - {e.Data}");
        }
        private void onConnect(object sender, OnConnectedArgs e)
        {
            // Console.WriteLine($"Connected to {e.AutoJoinChannel}");
        }
        private void onMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            Console.WriteLine($"{e.ChatMessage.DisplayName}: {e.ChatMessage.Message}");
        }
    }
    public class AccessToken
    {
        public uint userId;
        public string access_token;
        public string refresh_token;
        public long expires;
    }
}