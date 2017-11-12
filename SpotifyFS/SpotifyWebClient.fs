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
        inherit IClient()
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
            
       
        //override this.Download(url, ?headers) =            
        //    let (inf, item) = this.DownloadRaw(url, headers)
        //    (inf, if item.Length > 0 then _encoding.GetString(item) else "{}")
            

        override this.DownloadAsync(url, ?headers) = async {

            let! (inf, item) = this.DownloadRawAsync(url, headers)
            return (inf, if item.Length > 0 then _encoding.GetString(item) else "{}")
        }

        //override this.DownloadRaw(url, ?headers) =
        //    let keyValues = defaultArg headers (new Dictionary<string, string>())
        //    use client = new HttpClient()                                   
                            
        //    for headerPair in keyValues do                        
        //        client.DefaultRequestHeaders.TryAddWithoutValidation(headerPair.Key, headerPair.Value) |> ignore
        //    client.
        //    use response = Task.Run((fun () -> client.GetAsync(url))).Result
        //    (new ResponseInfo(response.StatusCode, ConvertHeaders(response.Headers), Async.AwaitTask(response.Content.ReadAsByteArrayAsync()).Result))

        override this.DownloadRawAsync(url, ?headers) = async {
            let mutable code = HttpStatusCode.OK
            
            let headers = defaultArg headers (new Dictionary<string, string>())
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

        //public Tuple<ResponseInfo, T> DownloadJson<T>(string url, Dictionary<string, string> headers = null)
        //{
        //    Tuple<ResponseInfo, string> response = Download(url, headers);
        //    return new Tuple<ResponseInfo, T>(response.Item1, JsonConvert.DeserializeObject<T>(response.Item2, JsonSettings));
        //}

        //override this.DownloadJsonAsync<'T>(url, ?headers) = async {
        //    let! (info, bytes) = this.DownloadAsync(url, headers)
        //    let mmm = JsonProvider<url>vc
        //    return new Tuple<ResponseInfo, T>(response.Item1, JsonConvert.DeserializeObject<T>(response.Item2, JsonSettings));
        //}

        //public Tuple<ResponseInfo, string> Upload(string url, string body, string method, Dictionary<string, string> headers = null)
        //{
        //    Tuple<ResponseInfo, byte[]> data = UploadRaw(url, body, method, headers);
        //    return new Tuple<ResponseInfo, string>(data.Item1, data.Item2.Length > 0 ? _encoding.GetString(data.Item2) : "{}");
        //}

        //public async Task<Tuple<ResponseInfo, string>> UploadAsync(string url, string body, string method, Dictionary<string, string> headers = null)
        //{
        //    Tuple<ResponseInfo, byte[]> data = await UploadRawAsync(url, body, method, headers).ConfigureAwait(false);
        //    return new Tuple<ResponseInfo, string>(data.Item1, data.Item2.Length > 0 ? _encoding.GetString(data.Item2) : "{}");
        //}

        //public Tuple<ResponseInfo, byte[]> UploadRaw(string url, string body, string method, Dictionary<string, string> headers = null)
        //{
        //    using (HttpClient client = new HttpClient())
        //    {
        //        if (headers != null)
        //        {
        //            foreach (KeyValuePair<string, string> headerPair in headers)
        //            {
        //                client.DefaultRequestHeaders.TryAddWithoutValidation(headerPair.Key, headerPair.Value);
        //            }
        //        }

        //        HttpRequestMessage message = new HttpRequestMessage(new HttpMethod(method), url)
        //        {
        //            Content = new StringContent(body, _encoding)
        //        };
        //        using (HttpResponseMessage response = Task.Run(() => client.SendAsync(message)).Result)
        //        {
        //            return new Tuple<ResponseInfo, byte[]>(new ResponseInfo
        //            {
        //                StatusCode = response.StatusCode,
        //                Headers = ConvertHeaders(response.Headers)
        //            }, Task.Run(() => response.Content.ReadAsByteArrayAsync()).Result);
        //        }
        //    }
        //}

        //public async Task<Tuple<ResponseInfo, byte[]>> UploadRawAsync(string url, string body, string method, Dictionary<string, string> headers = null)
        //{
        //    using (HttpClient client = new HttpClient())
        //    {
        //        if (headers != null)
        //        {
        //            foreach (KeyValuePair<string, string> headerPair in headers)
        //            {
        //                client.DefaultRequestHeaders.TryAddWithoutValidation(headerPair.Key, headerPair.Value);
        //            }
        //        }

        //        HttpRequestMessage message = new HttpRequestMessage(new HttpMethod(method), url)
        //        {
        //            Content = new StringContent(body, _encoding)
        //        };
        //        using (HttpResponseMessage response = await client.SendAsync(message))
        //        {
        //            return new Tuple<ResponseInfo, byte[]>(new ResponseInfo
        //            {
        //                StatusCode = response.StatusCode,
        //                Headers = ConvertHeaders(response.Headers)
        //            }, await response.Content.ReadAsByteArrayAsync());
        //        }
        //    }
        //}

        //public Tuple<ResponseInfo, T> UploadJson<T>(string url, string body, string method, Dictionary<string, string> headers = null)
        //{
        //    Tuple<ResponseInfo, string> response = Upload(url, body, method, headers);
        //    return new Tuple<ResponseInfo, T>(response.Item1, JsonConvert.DeserializeObject<T>(response.Item2, JsonSettings));
        //}

        //public async Task<Tuple<ResponseInfo, T>> UploadJsonAsync<T>(string url, string body, string method, Dictionary<string, string> headers = null)
        //{
        //    Tuple<ResponseInfo, string> response = await UploadAsync(url, body, method, headers).ConfigureAwait(false);
        //    return new Tuple<ResponseInfo, T>(response.Item1, JsonConvert.DeserializeObject<T>(response.Item2, JsonSettings));
        //}


    