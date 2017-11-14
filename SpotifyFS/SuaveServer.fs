module SpotifyFS.SuaveServer
    open System
    open System
    open System.IO
    open System.Net.Http
    open System.Net.Http.Headers
    open System.Text    
    open System.Threading

    open Suave
    open Suave.Http
    open Suave.Operators
    open Suave.Filters
    open Suave.Globals
    open Suave.Web
    open Suave.Logging
    open Suave.Sscanf
    open Suave.Writers
    open Suave.Successful
    open Suave.Files
    open Suave.RequestErrors
    open Suave.Logging
    open Suave.Utils
    
    open System.Net

    open Suave.Sockets
    open Suave.Sockets.Control
    open Suave.WebSocket

    open SpotifyFS
    open SpotifyFS.Enums
    open System.Diagnostics
    open System.Collections.Specialized
    
            
    let mutable Authenticated = false

    /// <summary>
    /// Initial Setup of USer Spotify Settings
    /// </summary>
    let AuthenticateAsync clientId redirectUri (scopes: Scope) = async {        

        //if (UserSpotify.Authenticated)
        //    return;
        //else
        try 
            let GetUri(): string =                
                sprintf "https://accounts.spotify.com/authorize/?client_id=%s&response_type=token&redirect_uri=%A&state=%s&scope=%s&show_dialog=%b" 
                    clientId redirectUri "XSS" (StringAttribute.GetOrEmptyString(scopes, " ")) false

            let openPage = async { 
                    return Process.Start(GetUri()).WaitForExit()                        
                }

            do! openPage

        with 
        //| :? SpotifyException -> raise(new SpotifyAuthError())
            
        | e ->                
            //var e = new SpotifyAuthError(ex.Message);
            Console.WriteLine(e.Message + "\n" + e.StackTrace);
            
    }

    //let mutable SpotifyAuth = ""


    let private SetApi (col: NameValueCollection) = 
        {
            AccessToken = col.Get "access_token"
            TokenType = col.Get "token_type"
            ExpiresIn = col.Get "expires_in" |> float
            State = col.Get "state"
        }

    let mutable AuthResponse = {
            AccessToken = null
            TokenType = null
            ExpiresIn = 0.0
            State = "Not authorized"
        }

    let private cts = new CancellationTokenSource()

        //http://localhost:8080/#access_token=BQAiO6B5LW7v3OxgPp_4tgqFBBjw0_o26_X7M93--z_LwP6YzE7n-b0y13uX_Y93Fks7_AoqwNRL4eLfLgncuumnif7cg5DTXlhlJdfnA1Q9c9zIxw18BC_n0cUCAMzRDl-VhlIAVG9i70YkdMtte7eZQMK8CceQv81I_hXjHGPG4Kqhlq3mgcNH58OyVFu2x8fNlwIlBOw4wclRImuo4rz6WPMaXo0_59tkZp6OZkGK_7AP_pXr7Ljp4kBHWW4LPT14hUf50vPUSKUORvxmw_l_fJylaotpTNoDT01pKi06qP9fNaV2vJkRj1OsV-OaouHymw
        //&token_type=Bearer&expires_in=3600&state=XSS
    let ws (webSocket : WebSocket) (context: HttpContext) =
      socket {
        // if `loop` is set to false, the server will stop receiving messages
        let mutable loop = true

        while loop do
          // the server will wait for a message to be received without blocking the thread
          let! msg = webSocket.read()

          match msg with
          // the message has type (Opcode * byte [] * bool)
          //
          // Opcode type:
          //   type Opcode = Continuation | Text | Binary | Reserved | Close | Ping | Pong
          //
          // byte [] contains the actual message
          //
          // the last element is the FIN byte, explained later
          
          | (Text, data, true) ->
            // the message can be converted to a string
            let str = System.Text.Encoding.UTF8.GetString data
            let response =
                if str.StartsWith("query=") then      

                    let col = System.Web.HttpUtility.ParseQueryString(str)
                    AuthResponse <- SetApi col
                    loop <- false
                    sprintf "found"
                    
                else
                     sprintf "response to %s" str
            // the response needs to be converted to a ByteSegment
            let byteResponse =
              response
              |> System.Text.Encoding.ASCII.GetBytes
              |> ByteSegment

            // the `send` function sends a message back to the client
            do! webSocket.send Text byteResponse true
            
            

          | (Close, _, _) ->
            let emptyResponse = [||] |> ByteSegment
            do! webSocket.send Close emptyResponse true

            // after sending a Close message, stop the loop
            loop <- false

          | (code, data, fin) -> 
                printf "Unmatched code: %A" code
                ()

        }
        
    let private GetAuthPage (host: string) (port: int) (path: string) =         
        sprintf """<!DOCTYPE html>
        <meta charset="utf-8" />
        <title>SpotifyAuth</title>
        <script language="javascript" type="text/javascript">
            var wsUri = "ws://%s:%i%s";
            var output;
            let websocket;

            function init()
            {
                output = document.getElementById("output");
                testWebSocket();
            }
            const doSend = (message) => websocket.send(message);
    
            function testWebSocket()
            {
                websocket = new WebSocket(wsUri);
                websocket.onopen = (evt) =>
                {
                    writeToScreen("CONNECTED");
                    
                    //var query = window.location.search.substring(1);
                    var hashes = window.location.hash;
                    hashes = hashes.replace('#','&');
                    //writeToScreen(query);
                    //writeToScreen("hashes: " + hashes);
                    if (hashes)
                        doSend("query=true" + hashes);
                        
                    websocket.close();                    
                    window.close();

                }

                websocket.onmessage = function (evt)
                {
                    //writeToScreen('<span style="color: blue;">RESPONSE: ' + evt.data + '</span>');
                    websocket.close();                    
                    window.close();
                }

                //websocket.onerror = (evt) => writeToScreen('<span style="color: red;">ERROR:</span> ' + evt.data || evt);

            }

            function writeToScreen(message)
            {
                var pre = document.createElement("p");
                pre.style.wordWrap = "break-word";
                pre.innerHTML = message;
                output.appendChild(pre);
            }
            window.addEventListener("load", init, false);
        </script>

        <h2>Spotify Auth</h2>

        <div id="output"></div>""" 
          host port path

       
    let app host port websocketPath: WebPart = 
        choose [        
            path websocketPath >=> handShake ws
            GET >=> choose [ 
                path "/" >=> OK (GetAuthPage host port websocketPath)
                browseHome                
            ]        
            NOT_FOUND "Found no handlers." ]

    type ServerConfig = 
        { ClientId: string; RedirectUri: string; Scope: Scope; WebSocket: WebSocketConfig; }

    and WebSocketConfig =
        { Host: string; Port: int; Path: string}

    let DefaultServerConfig = 
        {
            ClientId = ""
            RedirectUri= "http://localhost:8080"
            Scope = Scope.None
            WebSocket = 
              { 
                 Host= "localhost"
                 Port= 8080
                 Path= "/websocket" 
              }
        }
        
    let GetAuthUri (config: ServerConfig) = 
                sprintf "https://accounts.spotify.com/authorize/?client_id=%s&response_type=token&redirect_uri=%A&state=%s&scope=%s" 
                    config.ClientId config.RedirectUri "xss" (StringAttribute.GetOrEmptyString(config.Scope, " "))


    let AuthenticateSpotify (config: ServerConfig) = // clientId redirectUri scope host port websocketPath =
        
        let startAuth() = async {
            let socketServer = app config.WebSocket.Host config.WebSocket.Port config.WebSocket.Path
            let listening, server = startWebServerAsync { defaultConfig with logger = Targets.create Verbose [||]; cancellationToken = cts.Token } socketServer
            
            Async.Start(server, cts.Token)

            GetAuthUri config
            |> Process.Start
            |> ignore

            while AuthResponse.AccessToken = null do
                do! Async.Sleep 20

            cts.Cancel()

            do! AuthenticateAsync config.ClientId config.RedirectUri config.Scope 
        }
        
        Async.RunSynchronously(startAuth())

        AuthResponse

    