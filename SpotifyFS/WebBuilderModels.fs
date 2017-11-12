namespace SpotifyFS
    open System
    open System.Reflection
    open System.Collections.Generic
    open System.Linq
    open System.Text
    open Newtonsoft.Json
    open SpotifyFS
    open System.Net
    open System.Net.Http.Headers;
    open System.Threading.Tasks;
    open SpotifyFS
    open SpotifyFS.Enums
   
    module Models =
        type TuneableTrack() =   
        
            [<String("acousticness")>]
            member val Acousticness  =  -1.0 with get,set

            [<String("danceability")>]
            member val Danceability  =  -1.0 with get,set

            [<String("duration_ms")>]
            member val DurationMs  =  -1 with get,set

            [<String("energy")>]
            member val Energy =  -1.0 with get,set

            [<String("instrumentalness")>]
            member val Instrumentalness =  -1.0 with get,set

            [<String("key")>]
            member val Key = -1 with get,set

            [<String("liveness")>]
            member val Liveness = -1.0 with get,set

            [<String("loudness")>]
            member val Loudness  = -1.0 with get,set

            [<String("mode")>]
            member val  Mode = -1 with get,set

            [<String("popularity")>]
            member val  Popularity = -1 with get,set

            [<String("speechiness")>]
            member val  Speechiness = -1.0 with get,set

            [<String("tempo")>]
            member val Tempo = -1.0 with get,set

            [<String("time_signature")>]
            member val TimeSignature = -1 with get,set

            [<String("valence")>]
            member val Valence = -1.0 with get,set
        
            member this.BuildUrlParams prefix =
                [
                    for info in this.GetType().GetProperties() do
                
                        let value = info.GetValue(this)
                        let name = info.GetCustomAttribute<StringAttribute>().Text
                        match Double.TryParse(string value) with
                        | (valid, num) when valid && num > 0.0 
                            -> yield (sprintf "%s_%A=%A" prefix name value);
                        | _ -> yield ""
                ]
                |> List.filter (fun s -> s <> "")
                |> function urlParams ->
                            if urlParams.Length > 0 then 
                                "&" + (String.Join("&", urlParams))
                            else 
                                ""
        /// <summary>
        ///     Delete-Track Wrapper
        /// </summary>
        /// <param name="uri">An Spotify-URI</param>
        /// <param name="positions">Optional positions</param>    
        type DeleteTrackUri(uri, [<ParamArray>] positions:  int[])  =

            [<JsonProperty("uri")>]
            member __.Uri = uri

            [<JsonProperty("positions")>]
            member __.Positions = positions

            member this.ShouldSerializePositions() = this.Positions.Length > 0
        
    
       type Error(status: HttpStatusCode, ?message) = class
    
            [<JsonProperty("status")>]
            member val Status = status

            [<JsonProperty("message")>]
            member val Member = defaultArg message "" with get,set
    
        end
               
        type Token(?accessTok, ?tokenType, ?expiresIn, ?refreshToken, ?error, ?errorDescr) = class            

            [<JsonProperty("access_token")>]
            member val AccessToken = (defaultArg accessTok "") with get, set

            [<JsonProperty("token_type")>]
            member val TokenType = (defaultArg tokenType "") with get, set

            [<JsonProperty("expires_in")>]
            member val ExpiresIn = (defaultArg expiresIn 0.0) with get, set

            [<JsonProperty("refresh_token")>]
            member val RefreshToken = (defaultArg refreshToken "") with get, set

            [<JsonProperty("error")>]
            member val Error = (defaultArg error "") with get, set

            [<JsonProperty("error_description")>]
            member val ErrorDescription = (defaultArg errorDescr "") with get, set

            member val CreateDate = DateTime.Now with get, set

            /// <summary>
            ///     Checks if the token has expired
            /// </summary>
            /// <returns></returns>
            member this.IsExpired =
                this.CreateDate.Add(TimeSpan.FromSeconds this.ExpiresIn) <= DateTime.Now
        end

        type ResponseInfo(statusCode: HttpStatusCode, ?error, ?headers) =
            member val StatusCode = statusCode
            member val Error: Error option = error with get,set
            member val Headers = defaultArg headers (WebHeaderCollection()) with get,set

            //static member Empty = new ResponseInfo()
   
        type stringMap = Dictionary<string, string>
        
        //type ResponseInfo * 'T = ResponseInfo * 'T
        //type Async<ResponseInfo * 'T> = Async<ResponseInfo * 'T>
        
       // type string * string option -> ResponseInfo * 'T> = string * string option - ResponseInfo * 'T
        //type string * string option -> Async<ResponseInfo * 'T> = string * string option -> Async<ResponseInfo * 'T
 
        [<AbstractClass>]
        type IClient() =            
            member val JsonSettings = new JsonSerializerSettings() with get, set
    
            /// <summary>
            ///     Downloads data from an URL and returns it
            /// </summary>
            /// <param name="url">An URL</param>
            /// <returns></returns>
            //abstract member Download: string * stringMap option -> ResponseInfo * string

            /// <summary>
            ///     Downloads data async from an URL and returns it
            /// </summary>
            /// <param name="url"></param>
            /// <returns></returns>
            abstract member DownloadAsync: string * stringMap option -> Async<ResponseInfo * string>

            /// <summary>
            ///     Downloads data from an URL and returns it
            /// </summary>
            /// <param name="url">An URL</param>
            /// <returns></returns>
            //abstract member DownloadRaw : string * stringMap option -> ResponseInfo * byte[]

            /// <summary>
            ///     Downloads data async from an URL and returns it
            /// </summary>
            /// <param name="url"></param>
            /// <returns></returns>
            abstract member DownloadRawAsync : string * stringMap option -> Async<ResponseInfo * byte[]>

            ///// <summary>
            /////     Downloads data from an URL and converts it to an object
            ///// </summary>
            ///// <typeparam name="T">The Type which the object gets converted to</typeparam>
            ///// <param name="url">An URL</param>
            ///// <returns></returns>
            ////abstract member DownloadJson<'T> : string * stringMap -> ResponseInfo * 'T

            ///// <summary>
            /////     Downloads data async from an URL and converts it to an object
            ///// </summary>
            ///// <typeparam name="T">The Type which the object gets converted to</typeparam>
            ///// <param name="url">An URL</param>
            ///// <returns></returns>
            //abstract member DownloadJsonAsync<'T> : string * stringMap option -> Async<ResponseInfo * 'T>

            ///// <summary>
            /////     Uploads data from an URL and returns the response
            ///// </summary>
            ///// <param name="url">An URL</param>
            ///// <param name="body">The Body-Data (most likely a JSON String)</param>
            ///// <param name="method">The Upload-method (POST,DELETE,PUT)</param>
            ///// <returns></returns>
            ////abstract member Upload : string * string * string * stringMap -> ResponseInfo * string

            ///// <summary>
            /////     Uploads data async from an URL and returns the response
            ///// </summary>
            ///// <param name="url">An URL</param>
            ///// <param name="body">The Body-Data (most likely a JSON String)</param>
            ///// <param name="method">The Upload-method (POST,DELETE,PUT)</param>
            ///// <returns></returns>
            //abstract member UploadAsync : string * string * string * stringMap option  -> Async<ResponseInfo * string>

            ///// <summary>
            /////     Uploads data from an URL and returns the response
            ///// </summary>
            ///// <param name="url">An URL</param>
            ///// <param name="body">The Body-Data (most likely a JSON String)</param>
            ///// <param name="method">The Upload-method (POST,DELETE,PUT)</param>
            ///// <returns></returns>
            ////abstract member UploadRaw : string * string * string * stringMap -> ResponseInfo * byte[]

            ///// <summary>
            /////     Uploads data async from an URL and returns the response
            ///// </summary>
            ///// <param name="url">An URL</param>
            ///// <param name="body">The Body-Data (most likely a JSON String)</param>
            ///// <param name="method">The Upload-method (POST,DELETE,PUT)</param>
            ///// <returns></returns>
            //abstract member UploadRawAsync : string * string * string option  * stringMap option  -> Async<ResponseInfo * string>

            ///// <summary>
            /////     Uploads data from an URL and converts the response to an object
            ///// </summary>
            ///// <typeparam name="T">The Type which the object gets converted to</typeparam>
            ///// <param name="url">An URL</param>
            ///// <param name="body">The Body-Data (most likely a JSON String)</param>
            ///// <param name="method">The Upload-method (POST,DELETE,PUT)</param>
            ///// <returns></returns>
            ////abstract member UploadJson<'T> : string * string * string * stringMap -> ResponseInfo * 'T

            ///// <summary>
            /////     Uploads data async from an URL and converts the response to an object
            ///// </summary>
            ///// <typeparam name="T">The Type which the object gets converted to</typeparam>
            ///// <param name="url">An URL</param>
            ///// <param name="body">The Body-Data (most likely a JSON String)</param>
            ///// <param name="method">The Upload-method (POST,DELETE,PUT)</param>
            ///// <returns></returns>
            //abstract member UploadJsonAsync<'T> : string * string * string option * stringMap option  -> Async<ResponseInfo * 'T>
