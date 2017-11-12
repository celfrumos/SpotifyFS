// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.
open SpotifyFS
open SpotifyFS.Enums
open SpotifyFS.Auth
open System

module UserSpotify =
        
        let mutable WebAPI: SpotifyWebApi = null
        
        let [<Literal>] REDIRECT_URI = "http://localhost"
        let [<Literal>] PORT = 8000
        let [<Literal>] ClientId = "4d8b5c54f513430f8b0f486974c89402"

        let mutable Authenticated = false

        /// <summary>
        /// Initial Setup of USer Spotify Settings
        /// </summary>
        let AuthenticateAsync() = async {        

            //if (UserSpotify.Authenticated)
            //    return;
            //else
            try
            
                //let clientId = clientId ?? Secrets.Spotify.ClientId;
                let scopes =  Scope.UserReadPrivate ||| Scope.UserReadEmail  ||| Scope.PlaylistReadPrivate ||| Scope.UserLibraryRead ||| 
                              Scope.UserReadPrivate ||| Scope.UserFollowRead ||| Scope.UserReadBirthdate   ||| Scope.UserTopRead     |||
                              Scope.PlaylistModifyPrivate ||| Scope.PlaylistModifyPublic

                let webApiFactory = new WebAPIFactory(REDIRECT_URI, PORT, ClientId, scope = scopes, timeout = TimeSpan.FromMinutes(10.0))

                if (WebAPI = null) then
                    let! api = webApiFactory.GetWebApi()
                    WebAPI <- api
                    Authenticated <- true
                else 
                    do()

            with 
            //| :? SpotifyException -> raise(new SpotifyAuthError())
            
            | e ->                
                //var e = new SpotifyAuthError(ex.Message);
                Console.WriteLine(e.Message + "\n" + e.StackTrace);
            
        }

[<EntryPoint>]
let main argv =     
    let task = async{
        do! UserSpotify.AuthenticateAsync()
        let api = UserSpotify.WebAPI
        let albumId = "2cVOSmXfC0wZBoj7TmpZl7"
        
        let! alb = api.GetAlbumAsync(albumId) 
        match alb with
        | None 
            -> printfn "None returned"
        | Some al 
            -> printfn "Album\n%A" al
    }

    Async.RunSynchronously(task)
    
    printfn "%A" argv
    Console.ReadLine()
    0 // return an integer exit code
