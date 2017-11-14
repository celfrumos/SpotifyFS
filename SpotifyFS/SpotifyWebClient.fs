namespace SpotifyFS
open Newtonsoft.Json
open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Net
open System.Net.Http
open System.Net.Http.Headers
open System.Text
open System.Threading.Tasks
open SpotifyFS.Models

    type SpotifyWebClient() =   
        let _encoding = Encoding.UTF8
        
        let ConvertHeaders(headers: HttpResponseHeaders) =
        
            let newHeaders = new WebHeaderCollection()
            
            for headerPair in headers do                        
                for headerValue in headerPair.Value do 
                    newHeaders.Add(headerPair.Key, headerValue)               
            
            newHeaders
            
        member this.Dispose() = 
                GC.SuppressFinalize(this)
            
        
        interface IDisposable with
            member this.Dispose() =            
                GC.SuppressFinalize(this)
            
        member this.DownloadAsync(url, headers) = async {

            let! (inf, item: byte[]) = this.DownloadRawAsync(url, headers)
            return (inf, if item.Length > 0 then _encoding.GetString(item) else "{}")
        }
        
        member this.DownloadRawAsync(url, headers: Dictionary<string, string>) = async {
            let mutable code = HttpStatusCode.OK
            
            use client = new HttpClient()         
                
            for headerPair in headers do            
                client.DefaultRequestHeaders.TryAddWithoutValidation(headerPair.Key, headerPair.Value)  |> ignore          
                
            use! response = Async.AwaitTask(client.GetAsync(Uri(url)))                
            let! responseBytes = Async.AwaitTask(response.Content.ReadAsByteArrayAsync())
                
            let err = match response.StatusCode with
                        | HttpStatusCode.OK | HttpStatusCode.Accepted | HttpStatusCode.Found -> None
                        | e -> Some(Error(e, response.ReasonPhrase))


            let info = new ResponseInfo(response.StatusCode, headers = ConvertHeaders(response.Headers))
            printfn "Info:\n%A" info
            info.Error <- err
            return info, responseBytes
           
        }
        