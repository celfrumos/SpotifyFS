module SpotifyFS.Auth
    open System
    open System.Collections.Specialized
    open SpotifyFS
    open SpotifyFS.Enums
    open SpotifyFS.Enums.Extensions
    open System.Threading
    open SpotifyFS.Models
    open System.Diagnostics
    open System.Net
    open System.Text
    open System.IO
    open Newtonsoft.Json
    open System.Threading.Tasks

    [<Serializable>]
    type SpotifyWebApiException(msg) = inherit Exception(msg)
    
    type OnImplicitResponseReceived = delegate of (Token * string) -> unit

    type ImplicitGrantAuth(ClientId, RedirectUri, State, Scope: Scope, ?showDialog) = class       
    
            //public delegate void OnResponseReceived(Token token, string state);
            let _onResponseReceived = new Event<Token * string>() 
            
            let mutable ShowDialog = (defaultArg showDialog false)
            let mutable _httpThread: Thread = null
            let mutable _httpServer: SimpleHttpServer = (new SimpleHttpServer(8080, AuthType.Authorization))

            [<CLIEvent>]
            member this.OnResponseReceivedEvent =  _onResponseReceived.Publish

            member private this.GetUri(): string =                
                sprintf "https://accounts.spotify.com/authorize/?client_id=%s&response_type=token&redirect_uri=%A&state=%s&scope=%s&show_dialog=%b" 
                    ClientId RedirectUri State (StringAttribute.GetOrEmptyString(Scope, " ")) ShowDialog

            /// <summary>
            ///     Start the auth process (Make sure the internal HTTP-Server ist started)
            /// </summary>
            member this.DoAuth() =            
                this.GetUri()
                |> Process.Start
                

            /// <summary>
            ///     Start the internal HTTP-Server
            /// </summary>
            member this.StartHttpServer(?port) =
                let port = defaultArg port 80
                _httpServer <- new SimpleHttpServer(port, AuthType.Implicit)
                _httpServer.OnAuth.Add(this.HttpServerOnOnAuth)

                _httpThread <- new Thread(new ThreadStart(fun _ -> _httpServer.Listen() |> ignore))
                _httpThread.Start()
            

            member this.HttpServerOnOnAuth(e: AuthEventArgs) =
                let token = new Token(e.Code,  e.TokenType, expiresIn = e.ExpiresIn, error = e.Error)
                _onResponseReceived.Trigger(token, e.State)
            

            /// <summary>
            ///     This will stop the internal HTTP-Server (Should be called after you got the Token)
            /// </summary>
            member this.StopHttpServer() =            
                _httpServer.Dispose()
    end

    type WebAPIFactory(redirectUrl, listeningPort: int, clientId, scope, ?timeout, ?xss) = class
    
        let _timeout = defaultArg timeout (TimeSpan.FromSeconds 20.0)
        
        let _xss = defaultArg xss "XSS"
    

        member this.GetWebApi() = async {
        
            let redirect = new UriBuilder(Uri(redirectUrl))
            redirect.Port <- listeningPort
            let authentication = new ImplicitGrantAuth(clientId, redirect.Uri.OriginalString.TrimEnd('/'), _xss, scope)

            let authenticationWaitFlag = new AutoResetEvent(false);
            let mutable spotifyWebApi = null

            let newEvent = fun (token, state) ->                
                                            spotifyWebApi <- this.HandleSpotifyResponse(state, token)
                                            authenticationWaitFlag.Set() |> ignore

            authentication.OnResponseReceivedEvent.Add(newEvent)

            try
            
                authentication.StartHttpServer(listeningPort)

                authentication.DoAuth() |> ignore

                authenticationWaitFlag.WaitOne(_timeout) |> ignore

                if (spotifyWebApi = null) then
                    raise(new TimeoutException(sprintf "No valid response received for the last %f seconds" _timeout.TotalSeconds))
                else 
                    do()
            
            finally            
                authentication.StopHttpServer()            

            return spotifyWebApi
        }

        member this.HandleSpotifyResponse(state, token: Token) =
            if (state <> _xss) 
                then raise(new SpotifyWebApiException(sprintf "Wrong state '%s' received." state))

            elif (token.Error <> null) 
                then raise(new SpotifyWebApiException(sprintf "Error: %s" token.Error))
            else
                new SpotifyWebApi(true, token.AccessToken, token.TokenType)                
    end

    type AuthorizationCodeAuthResponse = { Code: string; State : string; Error : string}

    type ClientCredentialsAuth(ClientId, ClientSecret, Scope: Scope) = class
    

        /// <summary>
        ///     Starts the auth process and
        /// </summary>
        /// <returns>A new Token</returns>
        member this.DoAuth() = 
        
            use wc = new WebClient()
            
            wc.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(ClientId + ":" + ClientSecret)));

            let col = new NameValueCollection()
            col.Add("grant_type", "client_credentials")
            col.Add("scope", Scope.GetStringAttribute(" "))
            
            let data = 
                try            
                    wc.UploadValues("https://accounts.spotify.com/api/token", "POST", col);
            
                with 

                | :? WebException as e ->            
                    use reader = new StreamReader(e.Response.GetResponseStream())                
                    Encoding.UTF8.GetBytes(reader.ReadToEnd());
                
            
            JsonConvert.DeserializeObject<Token>(Encoding.UTF8.GetString(data));
            
        

        /// <summary>
        ///     Starts the auth process async and
        /// </summary>
        /// <returns>A new Token</returns>
        member this.DoAuthAsync() = async {
            use wc = new WebClient()
            
            wc.Headers.Add("Authorization",
                "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(ClientId + ":" + ClientSecret)));

            let col = new NameValueCollection()
            col.Add("grant_type", "client_credentials")
            col.Add("scope", Scope.GetStringAttribute(" "))
            
            let mutable data = Array.InitWithZeros<byte>(0)

            try            
                let! bytes = Async.AwaitTask(wc.UploadValuesTaskAsync("https://accounts.spotify.com/api/token", "POST", col))
                data <- bytes
            
            with 
            | :? WebException as e ->            
                use reader = new StreamReader(e.Response.GetResponseStream())                
                let! str = Async.AwaitTask(reader.ReadToEndAsync())
                data <- Encoding.UTF8.GetBytes(str)

            return JsonConvert.DeserializeObject<Token>(Encoding.UTF8.GetString(data));    
        }
    end

    //type OnAuthResponseReceived= delegate of AuthorizationCodeAuthResponse -> unit
    //type AuthorizationCodeAuth(ClientId, RedirectUri, State, Scope) =
        
    //        let mutable _httpServer: SimpleHttpServer = null
    //        let mutable _httpThread: Thread = null
    //        //public string ClientId { get; set; }
    //        //public string RedirectUri { get; set; }
    //        //public string State { get; set; }
    //        //public Scope Scope { get; set; }
    //        //public Boolean ShowDialog { get; set; }

    //        /// <summary>
    //        ///     Will be fired once the user authenticated
    //        /// </summary>
    //        //public event OnResponseReceived OnResponseReceivedEvent;

    //        /// <summary>
    //        ///     Start the auth process (Make sure the internal HTTP-Server ist started)
    //        /// </summary>
    //        public void DoAuth()
    //        {
    //            string uri = GetUri();
    //            Process.Start(uri);
    //        }

    //        /// <summary>
    //        ///     Refreshes auth by providing the clientsecret (Don't use this if you're on a client)
    //        /// </summary>
    //        /// <param name="refreshToken">The refresh-token of the earlier gathered token</param>
    //        /// <param name="clientSecret">Your Client-Secret, don't provide it if this is running on a client!</param>
    //        public Token RefreshToken(string refreshToken, string clientSecret)
    //        {
    //            using (WebClient wc = new WebClient())
    //            {
    //                wc.Headers.Add("Authorization",
    //                    "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(ClientId + ":" + clientSecret)));
    //                NameValueCollection col = new NameValueCollection
    //                {
    //                    {"grant_type", "refresh_token"},
    //                    {"refresh_token", refreshToken}
    //                };

    //                string response;
    //                try
    //                {
    //                    byte[] data = wc.UploadValues("https://accounts.spotify.com/api/token", "POST", col);
    //                    response = Encoding.UTF8.GetString(data);
    //                }
    //                catch (WebException e)
    //                {
    //                    using (StreamReader reader = new StreamReader(e.Response.GetResponseStream()))
    //                    {
    //                        response = reader.ReadToEnd();
    //                    }
    //                }
    //                return JsonConvert.DeserializeObject<Token>(response);
    //            }
    //        }

    //        private string GetUri()
    //        {
    //            StringBuilder builder = new StringBuilder("https://accounts.spotify.com/authorize/?");
    //            builder.Append("client_id=" + ClientId);
    //            builder.Append("&response_type=code");
    //            builder.Append("&redirect_uri=" + RedirectUri);
    //            builder.Append("&state=" + State);
    //            builder.Append("&scope=" + Scope.GetStringAttribute(" "));
    //            builder.Append("&show_dialog=" + ShowDialog);
    //            return builder.ToString();
    //        }

    //        /// <summary>
    //        ///     Start the internal HTTP-Server
    //        /// </summary>
    //        public void StartHttpServer(int port = 80)
    //        {
    //            _httpServer = new SimpleHttpServer(port, AuthType.Authorization);
    //            _httpServer.OnAuth += HttpServerOnOnAuth;

    //            _httpThread = new Thread(_httpServer.Listen);
    //            _httpThread.Start();
    //        }

    //        private void HttpServerOnOnAuth(AuthEventArgs e)
    //        {
    //            OnResponseReceivedEvent?.Invoke(new AutorizationCodeAuthResponse()
    //            {
    //                Code = e.Code,
    //                State = e.State,
    //                Error = e.Error
    //            });
    //        }

    //        /// <summary>
    //        ///     This will stop the internal HTTP-Server (Should be called after you got the Token)
    //        /// </summary>
    //        public void StopHttpServer()
    //        {
    //            _httpServer.Dispose();
    //            _httpServer = null;
    //        }

    //        /// <summary>
    //        ///     Exchange a code for a Token (Don't use this if you're on a client)
    //        /// </summary>
    //        /// <param name="code">The gathered code from the response</param>
    //        /// <param name="clientSecret">Your Client-Secret, don't provide it if this is running on a client!</param>
    //        /// <returns></returns>
    //        public Token ExchangeAuthCode(string code, string clientSecret)
    //        {
    //            using (WebClient wc = new WebClient())
    //            {
    //                NameValueCollection col = new NameValueCollection
    //                {
    //                    {"grant_type", "authorization_code"},
    //                    {"code", code},
    //                    {"redirect_uri", RedirectUri},
    //                    {"client_id", ClientId},
    //                    {"client_secret", clientSecret}
    //                };

    //                string response;
    //                try
    //                {
    //                    byte[] data = wc.UploadValues("https://accounts.spotify.com/api/token", "POST", col);
    //                    response = Encoding.UTF8.GetString(data);
    //                }
    //                catch (WebException e)
    //                {
    //                    using (StreamReader reader = new StreamReader(e.Response.GetResponseStream()))
    //                    {
    //                        response = reader.ReadToEnd();
    //                    }
    //                }
    //                return JsonConvert.DeserializeObject<Token>(response);
    //            }
    //        }
    //    }

    