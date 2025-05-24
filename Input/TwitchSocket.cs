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
using Microsoft.Xna.Framework;
using System.Text.RegularExpressions;


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
        List<Message> messages = new List<Message>();

        public TwitchSocket()
        {
            api = new TwitchAPI();
            api.Settings.ClientId = Environment.GetEnvironmentVariable("CLIENT_ID");
            server = new WebServer(Environment.GetEnvironmentVariable("REDIRECT_URL"));
            // scopes = ["channel:bot", "user:read:chat", "chat:edit"];
            scopes = ["chat:read"];
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
            var refresh = await api.Auth.RefreshAuthTokenAsync(accessToken.refreshToken, Environment.GetEnvironmentVariable("CLIENT_SECRET"));
            lock (this)
            {
                api.Settings.AccessToken = refresh.AccessToken;
            }
        }

        public async Task getAuthorized()
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

            DateTime temp = DateTime.Now + new TimeSpan(0, 0, resp.ExpiresIn);

            accessToken = new AccessToken()
            {
                accessToken = resp.AccessToken,
                refreshToken = resp.RefreshToken,
                expires = temp.Ticks,
                userId = uint.Parse(user.Id)
            };

            System.Console.WriteLine("Authorization success!");
            connect();
        }

        public async Task<bool> setAccessToken(AccessToken token)
        {
            this.accessToken = token;
            bool refreshed = false;
            System.Console.WriteLine($"Access Token: {token.accessToken}");
            DateTime expireDate = new DateTime(token.expires);
            if (expireDate < DateTime.Now)
            {
                System.Console.WriteLine("Expired Token");
                var refresh = await api.Auth.RefreshAuthTokenAsync(token.refreshToken, Environment.GetEnvironmentVariable("CLIENT_SECRET"));
                api.Settings.AccessToken = refresh.AccessToken;
                var tempUser = (await api.Helix.Users.GetUsersAsync()).Users[0];



                DateTime temp = DateTime.Now + new TimeSpan(0, 0, refresh.ExpiresIn);

                accessToken = new AccessToken()
                {
                    accessToken = refresh.AccessToken,
                    refreshToken = refresh.RefreshToken,
                    expires = temp.Ticks,
                    userId = uint.Parse(tempUser.Id)
                };
                refreshed = true;
            }
            else
            {
                var validate = api.Auth.ValidateAccessTokenAsync(token.accessToken);
                api.Settings.AccessToken = token.accessToken;
            }
            User user = (await api.Helix.Users.GetUsersAsync()).Users[0];
            System.Console.WriteLine($"Connected to: {user.DisplayName}");
            lock (this)
            {
                this.user = user;
            }

            return refreshed;
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


            // Convert to individual values to get correct Color
            string hex = e.ChatMessage.ColorHex.Substring(1); // remove the '#'

            byte r = Convert.ToByte(hex.Substring(0, 2), 16);
            byte g = Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = Convert.ToByte(hex.Substring(4, 2), 16);

            // Remove any characters that will break chat
            string safe = Regex.Replace(e.ChatMessage.Message, @"[^\u0020-\u007E]", "?");

            Message newMessage = new Message
            {
                displayName = e.ChatMessage.DisplayName,
                message = safe,
                bits = e.ChatMessage.Bits,
                color = new Color(r, g, b)
            };
            messages.Insert(0, newMessage);
        }

        public List<Message> GetMessages()
        {
            List<Message> tempMsg = this.messages;
            this.messages = new List<Message>();
            return tempMsg;
        }
    }
    public class AccessToken
    {
        public uint userId;
        public string accessToken;
        public string refreshToken;
        public long expires;
    }

    public class Message
    {
        public string displayName;
        public string message;
        public int bits;
        public Color color;
    }
}