// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.
open SpotifyFS
open SpotifyFS.Enums
open System

open SpotifyFS.SuaveServer
open Secret

let ALL_SCOPES =  Scope.UserReadPrivate         ||| Scope.UserReadEmail  ||| Scope.PlaylistReadPrivate ||| Scope.UserLibraryRead ||| 
                    Scope.UserReadPrivate       ||| Scope.UserFollowRead ||| Scope.UserReadBirthdate   ||| Scope.UserTopRead     |||
                    Scope.PlaylistModifyPrivate ||| Scope.PlaylistModifyPublic
                        
let MyServerConfig = { DefaultServerConfig with Scope = ALL_SCOPES; ClientId = CLIENT_ID}


[<EntryPoint>]
let main argv =     
    let myApi = SpotifyWebApi.Create MyServerConfig SpotifyWebApi.DefaultConfig
    let getAlbum = async {
            
            let albumId = "2cVOSmXfC0wZBoj7TmpZl7"
        
            let! alb = myApi.GetAlbumAsync(albumId) 
            match alb with
            | None 
                -> printfn "None returned"
            | Some al 
                -> printfn "Album\n%A" al
         }
    Async.RunSynchronously getAlbum

    Console.ReadLine()
    0 // return an integer exit code
