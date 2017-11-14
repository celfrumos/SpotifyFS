namespace SpotifyFS
    open System
    open System.ServiceModel
    open Microsoft.FSharp.Linq
    open Microsoft.FSharp.Data.TypeProviders
    open SpotifyFS
    open FSharp.Data


    module Types =
        open SpotifyWebBuilder

        let [<Literal>]edSheeranId = "6eUKZXaKkcviH0Ku9w2n3V"
    
        let GetArtist id = sprintf "%s/artists%s" APIBase id

        let edSheeranUrl = GetArtist edSheeranId


        type ArtistProvider = JsonProvider<"C:/Users/Josh/source/repos/FSharp/SpotifyFS/SpotifyFS/TypeSamples/FullArtist.json", EmbeddedResource="SpotifyFS.Types, FullArtist.json", RootName="FullArtist"> 
    
        type AlbumProvider = JsonProvider<"C:/Users/Josh/source/repos/FSharp/SpotifyFS/SpotifyFS/TypeSamples/FullAlbum.json", EmbeddedResource="SpotifyFS.Types, FullAlbum.json", RootName="FullAlbum"> 
        
        type Spotify = JsonProvider<"C:/Users/Josh/source/repos/FSharp/SpotifyFS/SpotifyFS/TypeSamples/FullTrack.json", EmbeddedResource="SpotifyFS.Types, FullTrack.json",  RootName="FullTrack">

        type AudioFeaturesProvider = JsonProvider<"C:/Users/Josh/source/repos/FSharp/SpotifyFS/SpotifyFS/TypeSamples/AudioFeatures.json", EmbeddedResource="SpotifyFS.Types, AudioFeatures.json",  RootName="AudioFeatures">
        
        type Spotify.Artist with
            member this.GetFullVersion() = ArtistProvider.AsyncLoad this.Href
            

        type Spotify.FullTrack with
            member this.GetAudioFeatures() = AudioFeaturesProvider.Load(GetAudioFeatures this.Id)
    
        