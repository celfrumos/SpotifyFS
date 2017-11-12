namespace SpotifyFS
open System
open System.Collections
open System.Collections.Generic
open System.Collections.Specialized
open System.IO
open System.Linq
open System.Net
open System.Net.Sockets
open System.Text
open System.Threading
open System.Threading.Tasks
open System.Web
open SpotifyFS

// offered to the public domain for any use with no restriction
// and also with no warranty of any kind, please enjoy. - David Jeske.

// simple HTTP explanation
// http://www.jmarshall.com/easy/http/


type AuthType =    
    | Implicit
    | Authorization

[<AbstractClass>]
type HttpProcessorBase() =
    abstract member Process: TcpClient -> unit
    abstract member ParseRequest: string -> unit
    abstract member ReadHeaders : seq<string> -> unit
    abstract member HandleGetRequest: unit -> unit
    abstract member HandlePostRequest: unit -> unit
        
    member this.WriteSuccess(?contentType) =
        let contentType = defaultArg contentType "text/html"
        this.OutputStream.WriteLine("HTTP/1.0 200 OK")
        this.OutputStream.WriteLine("Content-Type: " + contentType)
        this.OutputStream.WriteLine("Connection: close")
        this.OutputStream.WriteLine("")
        

    member this.WriteFailure() =        
        this.OutputStream.WriteLine("HTTP/1.0 404 File not found")
        this.OutputStream.WriteLine("Connection: close")
        this.OutputStream.WriteLine("")
        
    member val OutputStream: StreamWriter = null with get,set
    member val HttpProtocolVersionstring= "" with get,set
    member val HttpUrl = "" with get,set

[<AbstractClass>]
type HttpServerBase(port: int) = class
    inherit Object()

    member val Port = port
    member val IsActive = false with get, set 

    abstract member HandleGetRequest: HttpProcessorBase -> unit

    abstract member HandlePostRequest: HttpProcessorBase * StreamReader-> unit
end

[<AutoOpen>]
module ArrayExtensions =
    type Array with
        static member InitWithZeros<'T> size =
            Array.init<'T> size (fun _ -> Unchecked.defaultof<'T>)

open ArrayExtensions


type HttpProcessor(srv: HttpServerBase) =
    inherit HttpProcessorBase()
    let MaxPostSize = 10 * 1024 * 1024 // 10MB
    let [<Literal>] BufSize = 4096
    let _srv  = srv
    let mutable _inputStream: Stream = null
    let mutable _httpHeaders = new Hashtable()
    let mutable _httpMethod = "GET"
    
    member private this.GetIncomingRequest(inputStream: Stream): string[] =
        
        let buffer = Array.InitWithZeros<byte> BufSize
        let read = inputStream.Read(buffer, 0, buffer.Length)

        let inputData = Encoding.ASCII.GetString(buffer.Take(read).ToArray())

        query {
            for s in inputData.Split('\n') do
            where (not <| String.IsNullOrWhiteSpace s)
            select s
        }
        |> Seq.toArray
                
    
    override this.Process(socket: TcpClient) =
        
        // we can't use a StreamReader for input, because it buffers up extra data on us inside it's
        // "processed" view of the world, and we want the data raw after the headers
        _inputStream <- new BufferedStream(socket.GetStream())

        // we probably shouldn't be open a streamwriter for all output from handlers either
        this.OutputStream <- new StreamWriter(new BufferedStream(socket.GetStream()))
        try
            try
                let requestLines = this.GetIncomingRequest(_inputStream)

                this.ParseRequest(requestLines.First())
                this.ReadHeaders(requestLines.Skip(1))

                if (_httpMethod.Equals("GET")) then
                
                    this.HandleGetRequest()
                
                else if (_httpMethod.Equals("POST")) then
                
                    this.HandlePostRequest()
                
            
            with 
            | _ -> this.WriteFailure()
        finally
            this.OutputStream.Flush()
            _inputStream <- null
            this.OutputStream <- null
        

    override this.ParseRequest(request: string) =
        
        let tokens = request.Split(' ')
        if (tokens.Length < 2) then            
            raise(new Exception("Invalid HTTP request line"))
        else
            _httpMethod <- tokens.[0].ToUpper()
            this.HttpUrl <- tokens.[1]
        

    override this.ReadHeaders(requestLines: seq<string>) =
        let lines = Seq.toArray <| query {
            for s in requestLines do
            where (not <| String.IsNullOrWhiteSpace s)
            select s           
        } 
        for line in lines do           
            let separator = line.IndexOf(':')
            if separator = -1 then          
                raise (new Exception("Invalid HTTP header line: " + line))
            else                
                let name = line.Substring(0, separator)
                let mutable pos = separator + 1
                while ((pos < line.Length) && (line.[pos] = ' ')) do               
                    pos <- pos + 1 // strip any spaces
                

                let value = line.Substring(pos, line.Length - pos)
                _httpHeaders.[name] <- value
        ()
        

    override this.HandleGetRequest() =        
        _srv.HandleGetRequest(this)
        

    override this.HandlePostRequest()=
        
        // this post data processing just reads everything into a memory stream.
        // this is fine for smallish things, but for large stuff we should really
        // hand an input stream to the request processor. However, the input stream
        // we hand him needs to let him see the "end of the stream" at this content
        // length, because otherwise he won't know when he's seen it all!

        let ms = new MemoryStream()
        if (_httpHeaders.ContainsKey("Content-Length")) then
            
            let contentLen = Convert.ToInt32(_httpHeaders.["Content-Length"])
            if (contentLen > MaxPostSize) then                
                raise (new Exception(sprintf "POST Content-Length(%i) too big for this simple server" contentLen))
                
            let buf = Array.InitWithZeros<byte> BufSize

            let rec read toRead =
                let numread = _inputStream.Read(buf, 0, Math.Min(BufSize, toRead))
                if (numread = 0) then                    
                    if (toRead = 0) then                        
                        ()                     
                    else                        
                        raise (new Exception("Client disconnected during post"))
                else
                    ms.Write(buf, 0, numread)
                  
                    read (toRead - numread)
                

            read contentLen
                
            ms.Seek(0L, SeekOrigin.Begin) |> ignore
            
        _srv.HandlePostRequest(this, new StreamReader(ms))
        
    interface IDisposable with
        member this.Dispose() = ()
                


[<AbstractClass>]
type HttpServer(Port:int) = class
    inherit HttpServerBase(Port)
    
    let mutable _listener: TcpListener = null            

    interface IDisposable with
        member this.Dispose() =            
            this.IsActive <- false
            _listener.Stop()
            GC.SuppressFinalize(this)                
                
    member private this.AcceptTcpConnection = fun (ar: IAsyncResult) ->
            
        let listener = ar.AsyncState :?> TcpListener
        try
            try
                
                let tcpCLient = listener.EndAcceptTcpClient(ar)
                use processor = new HttpProcessor(this)                    
                processor.Process(tcpCLient)
                                    
            with         
            | :? ObjectDisposedException as e  -> e |> ignore 
            | _ -> reraise()

        finally
                    
            if (this.IsActive) then
                listener.BeginAcceptTcpClient(this.acceptTCP_asDelegate, listener)
                |> ignore
            else
                ()
    
    member private this.acceptTCP_asDelegate = new AsyncCallback(this.AcceptTcpConnection)
            
    member this.Listen() =            
        try                
            _listener <- new TcpListener(IPAddress.Any, this.Port)
            _listener.Start()

            _listener.BeginAcceptTcpClient(this.acceptTCP_asDelegate, _listener)
                
        with
        | :? SocketException as e when e.ErrorCode <> 10004   //Ignore 10004, which is raisen when the thread gets terminated
            -> reraise()
end

type AuthEventArgs(?code,?tokenType,?state,?error,?expiresIn) = class
    inherit EventArgs()   
    
    //Code can be an AccessToken or an Exchange Code
    member val Code         = (defaultArg code "" )       with get,set
    member val TokenType    = (defaultArg tokenType "")   with get,set
    member val State        = (defaultArg state "")       with get,set
    member val Error:string = (defaultArg error null)     with get,set
    member val ExpiresIn    = (defaultArg expiresIn 0.0)  with get,set
end

type AuthEventHandler = IEvent<AuthEventArgs>


type SimpleHttpServer(port, authType: AuthType) = class
    inherit HttpServer(port)
    
    let _type = authType
    
    let _onAuth = new Event<AuthEventArgs>()

    [<CLIEvent>]
    member __.OnAuth = _onAuth.Publish
    
    interface IDisposable with  
        member this.Dispose() = ()

    member this.Dispose() = ()

    override this.HandleGetRequest(p: HttpProcessorBase) =
        
        p.WriteSuccess()
        if (p.HttpUrl = "/favicon.ico") then do()
        else
            let mutable t: Thread = null
            let url = if p.HttpUrl.Length = 1 then p.HttpUrl else p.HttpUrl.Substring(2, p.HttpUrl.Length - 2) 
            let col = HttpUtility.ParseQueryString(url)
            try
                if _type = AuthType.Authorization then           

                    if col.Keys.Get 0 <> "code" then
                
                        p.OutputStream.WriteLine("""<html><body><h1>Spotify Auth canceled!</h1></body></html>""")
                        t <- new Thread(new ThreadStart (fun _ -> _onAuth.Trigger(new AuthEventArgs(State = col.Get 1, Error = col.Get 0))))
                
                    else
                
                        p.OutputStream.WriteLine("""<html><body><h1>Spotify Auth successful!</h1><script>window.close()</script></body></html>""")
                        t <- new Thread(new ThreadStart(fun _ -> _onAuth.Trigger(new AuthEventArgs(Code = col.Get("code"), State = col.Get "s"))))
                
            
                elif (p.HttpUrl = "/") then                
                        p.OutputStream.WriteLine("""<html><body>
                                                    <script>
                                                        let hashes = window.location.hash
                                                        hashes = hashes.replace("#", "&") 
                                                        window.location = hashes
                                                    </script>
                                                    <h1>Spotify Auth successful!<br>Please copy the URL and paste it into the application</h1>
                                                    </body></html>""")
                        p.OutputStream.Flush()
                        p.OutputStream.Close()
                        ()
                else
                    if (col.Keys.Get 0 <> "access_token") then
                
                        p.OutputStream.WriteLine("""<html><body><h1>Spotify Auth canceled!</h1></body></html>""")
                        t <- new Thread(new ThreadStart (fun _ -> _onAuth.Trigger(new AuthEventArgs(Code = col.Get 0, State = col.Get 1))))
                
                    else                
                        p.OutputStream.WriteLine("""<html><body><h1>Spotify Auth successful!</h1><script>window.close()</script></body></html>""")
                        t <- new Thread(new ThreadStart (fun _ -> _onAuth.Trigger(new AuthEventArgs(Code = col.Get 0, TokenType = col.Get 1, ExpiresIn = float(Convert.ToInt32(col.Get 2)), State = col.Get 3))))
                        p.OutputStream.Flush()
                        p.OutputStream.Close()
            finally
                if isNull t 
                then () 
                else t.Start()
        

    override __.HandlePostRequest(p, inputData) = p.WriteSuccess()
        
end

    
