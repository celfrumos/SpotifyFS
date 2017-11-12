namespace SpotifyFS
    open System
    open System.Collections.Generic
    open System.Linq
    open System.Text
    open SpotifyFS
    open SpotifyFS.Enums
    open SpotifyFS.Models
        

    /// <summary>
    /// SpotifyAPI URL-Generator
    /// </summary>
    module SpotifyWebBuilder = begin
        let [<Literal>]APIBase = "https://api.spotify.com/v1"
            

        let private getMarket p m = GetOptionalParamString "market" p  m
        let private getCountry p m = GetOptionalParamString "country" p m
        let private getLocale p m = GetOptionalParamString "locale" p  m


        //#region Search
        /// <summary>
        ///     Get Spotify catalog information about artists, albums, tracks or playlists that match a keyword string.
        /// </summary>
        /// <param name="q">The search query's keywords (and optional field filters and operators), for example q=roadhouse+blues.</param>
        /// <param name="type">A list of item types to search across.</param>
        /// <param name="limit">The maximum number of items to . Default: 20. Minimum: 1. Maximum: 50.</param>
        /// <param name="offset">The index of the first result to . Default: 0</param>
        /// <param name="market">An ISO 3166-1 alpha-2 country code or the string from_token.</param>
            
        let SearchItems (market: string) (offset: int) (limit: int) (searchType: SearchType) q =
            let limit = FitInDefaultRange limit
            [   
                sprintf "/search?q=%s" q
                StringAtttributeToQueryString "type" '&' searchType                    
                sprintf "&limit=%i&offset=%i" limit offset 
                getMarket '&' market
            ]
            |> String.Concat    
                        
        let Search = SearchItems "" 0 20

        //#endregion Search

        //#region Albums

        /// <summary>
        ///     Get Spotify catalog information about an album’s tracks. Optional parameters can be used to limit the number of
        ///     tracks ed.
        /// </summary>
        /// <param name="id">The Spotify ID for the album.</param>
        /// <param name="limit">The maximum number of items to . Default: 20. Minimum: 1. Maximum: 50.</param>
        /// <param name="offset">The index of the first track to . Default: 0 (the first object).</param>
        /// <param name="market">An ISO 3166-1 alpha-2 country code. Provide this parameter if you want to apply Track Relinking.</param>
            
        let GetAlbumTracks (market: string) (offset: int) (limit: int) (id: string) =        
            let limit = FitInDefaultRange limit
            [
                sprintf "%s/albums/%s/tracks?limit=%i&offset=%i" APIBase id limit offset
                getMarket '&' market
            ]            
            |> String.Concat  
            
        let AlbumTracks = GetAlbumTracks "" 0 20

        /// <summary>
        ///     Get Spotify catalog information for a single album.
        /// </summary>
        /// <param name="id">The Spotify ID for the album.</param>
        /// <param name="market">An ISO 3166-1 alpha-2 country code. Provide this parameter if you want to apply Track Relinking.</param>
            
        let GetAlbum market id  =    
                
            [
                sprintf "%s/albums/%s" APIBase id
                getMarket '?' market
            ]
            |> String.Concat  


            
        let GetAlbumById = GetAlbum ""
            

        /// <summary>
        ///     Get Spotify catalog information for multiple albums identified by their Spotify IDs.
        /// </summary>
        /// <param name="ids">A list of the Spotify IDs for the albums. Maximum: 20 IDs.</param>
        /// <param name="market">An ISO 3166-1 alpha-2 country code. Provide this parameter if you want to apply Track Relinking.</param>
            
        let GetSeveralAlbums market ([<ParamArray>]ids: string[]) = 
            [
                sprintf "%s/albums" APIBase
                getMarket '?' market
                "&ids=" + (ids |> Array.take 20 |> String.Concat)
            ]
            |> String.Concat
            

        //#endregion Albums

        //#region Artists

        /// <summary>
        ///     Get Spotify catalog information for a single artist identified by their unique Spotify ID.
        /// </summary>
        /// <param name="id">The Spotify ID for the artist.</param>
            
        let GetArtist id = 
            sprintf "%s/artists%s" APIBase id
            

        /// <summary>
        ///     Get Spotify catalog information about artists similar to a given artist. Similarity is based on analysis of the
        ///     Spotify community’s listening history.
        /// </summary>
        /// <param name="id">The Spotify ID for the artist.</param>       
        let GetRelatedArtists id =
            sprintf "%s/artists/%s/related-artists" APIBase id
                

        ///**Description**
        ///  Get Spotify catalog information about an artist’s top tracks by country.
        ///**Parameters**
        ///  * `country` - The country: an ISO 3166-1 alpha-2 country code.
        ///  * `id` - The Spotify ID for the artist.
        ///
        ///**Output Type**
        ///  * `string`
        let GetArtistsTopTracks country id =        
            sprintf "%s/artists/%s/top-tracks?country=%s" APIBase id country
                
            
           
        /// <summary>
        ///     Get Spotify catalog information about an artist’s albums. Optional parameters can be specified in the query string
        ///     to filter and sort the response.
        /// </summary>
        /// <param name="id">The Spotify ID for the artist.</param>
        /// <param name="type">
        ///     A list of keywords that will be used to filter the response. If not supplied, all album types will
        ///     be ed
        /// </param>
        /// <param name="limit">The maximum number of items to . Default: 20. Minimum: 1. Maximum: 50.</param>
        /// <param name="offset">The index of the first album to . Default: 0</param>
        /// <param name="market">
        ///     An ISO 3166-1 alpha-2 country code. Supply this parameter to limit the response to one particular
        ///     geographical market
        /// </param>
            
        let GetArtistsAlbums (market: string) (offset: int) (limit: int)  albType  id=
            
            let limit = FitInDefaultRange limit
            [
                sprintf "%s/artists/%s/albums" APIBase id
                StringAtttributeToQueryString "album_type" '?' albType
                sprintf "&limit=%i&offset=%i" limit offset                    
                getMarket '&' market
            ]
            |> String.Concat
            

        /// <summary>
        ///     Get Spotify catalog information for several artists based on their Spotify IDs.
        /// </summary>
        /// <param name="ids">A list of the Spotify IDs for the artists. Maximum: 50 IDs.</param>
            
        let GetSeveralArtists ids = 
            sprintf "%s/artists?ids=%s" APIBase (ids |> Seq.take 50 |> String.Concat)
                 
            

        //#endregion Artists

        //#region Browse

        /// <summary>
        ///     Get a list of Spotify featured playlists (shown, for example, on a Spotify player’s “Browse” tab).
        /// </summary>
        /// <param name="locale">
        ///     The desired language, consisting of a lowercase ISO 639 language code and an uppercase ISO 3166-1
        ///     alpha-2 country code, joined by an underscore.
        /// </param>
        /// <param name="country">A country: an ISO 3166-1 alpha-2 country code.</param>
        /// <param name="timestamp">A timestamp in ISO 8601 format</param>
        /// <param name="limit">The maximum number of items to . Default: 20. Minimum: 1. Maximum: 50.</param>
        /// <param name="offset">The index of the first item to . Default: 0</param>
        /// <remarks>AUTH NEEDED</remarks>
        let GetFeaturedPlaylists locale country timestamp offset limit=
            
            let limit = FitInDefaultRange limit
            [
                sprintf "%s/browse/featured-playlists?limit=%i&offset=%i" APIBase limit offset  
                getLocale '&' locale                    
                getCountry '&' country
                GetOptionalParamWithDefault "timestamp" '&' (Date (timestamp,"yyyy-MM-ddTHH:mm:ss" )) Empty
            ]
            |> String.Concat
            
            
        /// <summary>
        ///     Get a list of Spotify featured playlists (shown, for example, on a Spotify player’s “Browse” tab).
        /// </summary>
        let FeaturedPlaylists = GetFeaturedPlaylists "" "" (DateTime()) 0

        /// <summary>
        ///     Get a list of  album releases featured in Spotify (shown, for example, on a Spotify player’s “Browse” tab).
        /// </summary>
        /// <param name="country">A country: an ISO 3166-1 alpha-2 country code.</param>
        /// <param name="limit">The maximum number of items to . Default: 20. Minimum: 1. Maximum: 50.</param>
        /// <param name="offset">The index of the first item to . Default: 0</param>
            
        /// <remarks>AUTH NEEDED</remarks>
        let GetAlbumReleases country offset limit =
             
            let limit = FitInDefaultRange limit
            [
                sprintf "%s/browse/-releases?limit=%i&offset=%i" APIBase limit offset
                getCountry '&' country
            ]
            |> String.Concat
            

        /// <summary>
        ///     Get a list of categories used to tag items in Spotify (on, for example, the Spotify player’s “Browse” tab).
        /// </summary>
        /// <param name="country">
        ///     A country: an ISO 3166-1 alpha-2 country code. Provide this parameter if you want to narrow the
        ///     list of ed categories to those relevant to a particular country
        /// </param>
        /// <param name="locale">
        ///     The desired language, consisting of an ISO 639 language code and an ISO 3166-1 alpha-2 country
        ///     code, joined by an underscore
        /// </param>
        /// <param name="limit">The maximum number of categories to . Default: 20. Minimum: 1. Maximum: 50. </param>
        /// <param name="offset">The index of the first item to . Default: 0 (the first object).</param>
            
        /// <remarks>AUTH NEEDED</remarks>
        let GetCategories country locale offset limit=
            
            let limit = FitInDefaultRange limit
            [
                sprintf "%s/browse/categories?limit=%i&offset=%i" APIBase limit offset
                getCountry '&' country
                getLocale '&' locale
            ]
            |> String.Concat
            
            

        /// <summary>
        ///     Get a single category used to tag items in Spotify (on, for example, the Spotify player’s “Browse” tab).
        /// </summary>
        /// <param name="categoryId">The Spotify category ID for the category.</param>
        /// <param name="country">
        ///     A country: an ISO 3166-1 alpha-2 country code. Provide this parameter to ensure that the category
        ///     exists for a particular country.
        /// </param>
        /// <param name="locale">
        ///     The desired language, consisting of an ISO 639 language code and an ISO 3166-1 alpha-2 country
        ///     code, joined by an underscore
        /// </param>
            
        /// <remarks>AUTH NEEDED</remarks>
        let GetCategory categoryId country locale =
            let country = getCountry '?' country
            [                    
                sprintf "%s/browse/categories/%s" APIBase categoryId
                country
                getLocale (if country = "" then '?' else '&') locale
            ]
            |> String.Concat

        /// <summary>
        ///     Get a list of Spotify playlists tagged with a particular category.
        /// </summary>
        /// <param name="categoryId">The Spotify category ID for the category.</param>
        /// <param name="country">A country: an ISO 3166-1 alpha-2 country code.</param>
        /// <param name="limit">The maximum number of items to . Default: 20. Minimum: 1. Maximum: 50.</param>
        /// <param name="offset">The index of the first item to . Default: 0</param>
            
        /// <remarks>AUTH NEEDED</remarks>
        let GetCategoryPlaylists categoryId country offset limit =
            
            let limit = FitInDefaultRange limit
            [                    
                sprintf "%s/browse/categories/%s/playlists?limit=%i&offset=%i" APIBase categoryId limit offset
                getCountry '&' country
            ]
            |> String.Concat




        /// <summary>
        ///     Create a playlist-style listening experience based on seed artists, tracks and genres.
        /// </summary>
        /// <param name="artistSeed">A comma separated list of Spotify IDs for seed artists. 
        /// Up to 5 seed values may be provided in any combination of seed_artists, seed_tracks and seed_genres.
        /// </param>
        /// <param name="genreSeed">A comma separated list of any genres in the set of available genre seeds.
        /// Up to 5 seed values may be provided in any combination of seed_artists, seed_tracks and seed_genres.
        /// </param>
        /// <param name="trackSeed">A comma separated list of Spotify IDs for a seed track.
        /// Up to 5 seed values may be provided in any combination of seed_artists, seed_tracks and seed_genres.
        /// </param>
        /// <param name="target">Tracks with the attribute values nearest to the target values will be preferred.</param>
        /// <param name="min">For each tunable track attribute, a hard floor on the selected track attribute’s value can be provided</param>
        /// <param name="max">For each tunable track attribute, a hard ceiling on the selected track attribute’s value can be provided</param>
        /// <param name="limit">The target size of the list of recommended tracks. Default: 20. Minimum: 1. Maximum: 100.
        /// For seeds with unusually small pools or when highly restrictive filtering is applied, it may be impossible to generate the requested number of recommended tracks.
        /// </param>
        /// <param name="market">An ISO 3166-1 alpha-2 country code. Provide this parameter if you want to apply Track Relinking.
        /// Because min_*, max_* and target_* are applied to pools before relinking, the generated results may not precisely match the filters applied.</param>
            
        /// <remarks>AUTH NEEDED</remarks>
        let GetRecommendations (target: TuneableTrack) (min: TuneableTrack) (max: TuneableTrack) (market: string) (limit: int) artistSeed genreSeed trackSeed = 
            
            let limit = FitInRange(1, 100) limit
            [
                sprintf "%s/recommendations?limit=%A" APIBase limit
                ListToQueryParams "seed_artists" '&' artistSeed
                ListToQueryParams "seed_genres" '&' genreSeed
                ListToQueryParams "seed_tracks" '&' trackSeed
                target.BuildUrlParams("target")
                min.BuildUrlParams("min")
                max.BuildUrlParams("max")
                getMarket '&' market
            ]
            |> String.Concat
            
        let GetRecommendationsDefault = GetRecommendations (TuneableTrack()) (TuneableTrack()) (TuneableTrack()) "" 20 [] [] []

        /// <summary>
        ///     Retrieve a list of available genres seed parameter values for recommendations.
        /// </summary>
            
        /// <remarks>AUTH NEEDED</remarks>
        let GetRecommendationSeedsGenres() = sprintf "%s/recommendations/available-genre-seeds" APIBase
            

        //#endregion Browse

        //#region Follow

        /// <summary>
        ///     Get the current user’s followed artists.
        /// </summary>
        /// <param name="limit">The maximum number of items to . Default: 20. Minimum: 1. Maximum: 50. </param>
        /// <param name="after">The last artist ID retrieved from the previous request.</param>
            
        /// <remarks>AUTH NEEDED</remarks>
        let GetFollowedArtists limit after =                         
                String.Concat [
                    sprintf "%s/me/following" APIBase
                    StringAtttributeToQueryString "type" '?' FollowType.Artist //currently only artist is supported.
                    sprintf "&limit=%i" (FitInDefaultRange limit)
                    GetOptionalParamString "after" '&' after                    
                ]
                

        /// <summary>
        ///     Add the current user as a follower of one or more artists or other Spotify users.
        /// </summary>
        /// <param name="followType">The ID type: either artist or user.</param>
            
        /// <remarks>AUTH NEEDED</remarks>
        let Follow followType =
                StringAtttributeToQueryString "type" '?' followType
                |> sprintf "%s/me/following%s" APIBase
            

        /// <summary>
        ///     Remove the current user as a follower of one or more artists or other Spotify users.
        /// </summary>
        /// <param name="followType">The ID type: either artist or user.</param>
            
        /// <remarks>AUTH NEEDED</remarks>
        let Unfollow followType =
                StringAtttributeToQueryString "type" '?' followType
                |> sprintf "%s/me/following%s" APIBase
            

        /// <summary>
        ///     Check to see if the current user is following one or more artists or other Spotify users.
        /// </summary>
        /// <param name="followType">The ID type: either artist or user.</param>
        /// <param name="ids">A list of the artist or the user Spotify IDs to check</param>
            
        /// <remarks>AUTH NEEDED</remarks>
        let IsFollowing(followType: FollowType, ids: string list) =
                [
                    sprintf "%s/me/following/contains" APIBase 
                    StringAtttributeToQueryString "type" '?' followType
                    ListToQueryParams "ids" '&' ids
                ]
                |> String.Concat

        /// <summary>
        ///     Add the current user as a follower of a playlist.
        /// </summary>
        /// <param name="ownerId">The Spotify user ID of the person who owns the playlist.</param>
        /// <param name="playlistId">
        ///     The Spotify ID of the playlist. Any playlist can be followed, regardless of its /private
        ///     status, as long as you know its playlist ID.
        /// </param>
        /// <param name="show">
        ///     If true the playlist will be included in user's  playlists, if false it will remain
        ///     private.
        /// </param>
            
        /// <remarks>AUTH NEEDED</remarks>
        let FollowPlaylist (ownerId: string) (playlistId: string) =
            
                sprintf "%s/users/%s/playlists/%s/followers" APIBase ownerId playlistId
            

        /// <summary>
        ///     Remove the current user as a follower of a playlist.
        /// </summary>
        /// <param name="ownerId">The Spotify user ID of the person who owns the playlist.</param>
        /// <param name="playlistId">The Spotify ID of the playlist that is to be no longer followed.</param>
            
        /// <remarks>AUTH NEEDED</remarks>
        let UnfollowPlaylist(ownerId: string) (playlistId: string) =
            FollowPlaylist ownerId playlistId
            

        /// <summary>
        ///     Check to see if one or more Spotify users are following a specified playlist.
        /// </summary>
        /// <param name="ownerId">The Spotify user ID of the person who owns the playlist.</param>
        /// <param name="playlistId">The Spotify ID of the playlist.</param>
        /// <param name="ids">A list of Spotify User IDs</param>            
        /// <remarks>AUTH NEEDED</remarks>
        let IsFollowingPlaylist (ownerId: string) (playlistId: string) (ids: string list) =            
            ListToQueryParams "ids" '?' ids
            |> sprintf "%s/users/%s/playlists/%s/followers/contains=%s" APIBase ownerId playlistId
            

        //#endregion Follow

        //#region Library

        /// <summary>
        ///     Save one or more tracks to the current user’s “Your Music” library.
        /// </summary>
            
        /// <remarks>AUTH NEEDED</remarks>
        let SaveTracks = sprintf "%s/me/tracks/" APIBase
            

        /// <summary>
        ///     Get a list of the songs saved in the current Spotify user’s “Your Music” library.
        /// </summary>
        /// <param name="limit">The maximum number of objects to . Default: 20. Minimum: 1. Maximum: 50.</param>
        /// <param name="offset">The index of the first object to . Default: 0 (i.e., the first object)</param>
        /// <param name="market">An ISO 3166-1 alpha-2 country code. Provide this parameter if you want to apply Track Relinking.</param>
            
        /// <remarks>AUTH NEEDED</remarks>
        let GetSavedTracks (limit: int , offset: int , market: string)=            
            getMarket '&' market
            |> sprintf "%s/me/tracks?limit=%i&offset=%i%s" APIBase (FitInDefaultRange limit) offset

            

        /// <summary>
        ///     Remove one or more tracks from the current user’s “Your Music” library.
        /// </summary>
            
        /// <remarks>AUTH NEEDED</remarks>
        let RemoveSavedTracks = sprintf "%s/me/tracks/" APIBase
            

        /// <summary>
        ///     Check if one or more tracks is already saved in the current Spotify user’s “Your Music” library.
        /// </summary>
        /// <param name="ids">A list of the Spotify IDs.</param>
            
        /// <remarks>AUTH NEEDED</remarks>
        let CheckSavedTracks(ids: string list) =       
            ListToQueryParams "ids" '?' ids
            |> sprintf "%s/me/tracks/contains%s" APIBase
            

        /// <summary>
        ///     Save one or more albums to the current user’s "Your Music" library.
        /// </summary>
            
        /// <remarks>AUTH NEEDED</remarks>
        let SaveAlbums = sprintf "%s/me/albums" APIBase
            

        /// <summary>
        ///     Get a list of the albums saved in the current Spotify user’s "Your Music" library.
        /// </summary>
        /// <param name="limit">The maximum number of objects to . Default: 20. Minimum: 1. Maximum: 50.</param>
        /// <param name="offset">The index of the first object to . Default: 0 (i.e., the first object)</param>
        /// <param name="market">An ISO 3166-1 alpha-2 country code. Provide this parameter if you want to apply Track Relinking.</param>
            
        /// <remarks>AUTH NEEDED</remarks>
        let GetSavedAlbums(limit: int , offset: int , market: string)=            
            getMarket '&' market
            |> sprintf "%s/me/albums?limit=%i&offset=%i%s" APIBase (FitInDefaultRange limit) offset
            
                
            

        /// <summary>
        ///     Remove one or more albums from the current user’s "Your Music" library.
        /// </summary>
            
        /// <remarks>AUTH NEEDED</remarks>
        let RemoveSavedAlbums = sprintf "%s/me/albums/" APIBase
            

        /// <summary>
        ///     Check if one or more albums is already saved in the current Spotify user’s "Your Music" library.
        /// </summary>
        /// <param name="ids">A list of the Spotify IDs.</param>
            
        /// <remarks>AUTH NEEDED</remarks>
        let CheckSavedAlbums(ids: string list) =            
                sprintf "%s/me/albums/contains%s" APIBase (ListToQueryParams "ids" '?' ids)
            

        //#endregion Library

        //#region Personalization

        /// <summary>
        ///     Get the current user’s top tracks based on calculated affinity.
        /// </summary>
        /// <param name="timeRange">Over what time frame the affinities are computed. 
        /// Valid values: long_term (calculated from several years of data and including all  data as it becomes available), 
        /// medium_term (approximately last 6 months), short_term (approximately last 4 weeks). </param>
        /// <param name="limit">The number of entities to . Default: 20. Minimum: 1. Maximum: 50</param>
        /// <param name="offset">The index of the first entity to . Default: 0 (i.e., the first track). Use with limit to get the next set of entities.</param>
            
        /// <remarks>AUTH NEEDED</remarks>
        let GetUsersTopTracks (offset: int) (timeRange: TimeRangeType) (limit: int) =                        
            StringAtttributeToQueryString "time_range" '&' timeRange
            |> sprintf "%s/me/top/tracks?limit=%i&offset=%i%s" APIBase (FitInDefaultRange limit) offset
            
                
            

        /// <summary>
        ///     Get the current user’s top artists based on calculated affinity.
        /// </summary>
        /// <param name="timeRange">Over what time frame the affinities are computed. 
        /// Valid values: long_term (calculated from several years of data and including all  data as it becomes available), 
        /// medium_term (approximately last 6 months), short_term (approximately last 4 weeks). </param>
        /// <param name="limit">The number of entities to . Default: 20. Minimum: 1. Maximum: 50</param>
        /// <param name="offset">The index of the first entity to . Default: 0 (i.e., the first track). Use with limit to get the next set of entities.</param>
            
        /// <remarks>AUTH NEEDED</remarks>
        let GetUsersTopArtists (timeRange: TimeRangeType) (limit: int) (offset: int) =                                  
            StringAtttributeToQueryString "time_range" '&' timeRange
            |>sprintf "%s/me/top/artists?limit=%i&offset=%i%s" APIBase (FitInDefaultRange limit) offset
            
                
            

        /// <summary>
        ///     Get tracks from the current user’s recent play history.
        /// </summary>
        /// <param name="limit">The maximum number of items to . Default: 20. Minimum: 1. Maximum: 50. </param>
        /// <param name="after">A Unix timestamp in milliseconds. s all items after (but not including) this cursor position. If after is specified, before must not be specified.</param>
        /// <param name="before">A Unix timestamp in milliseconds. s all items before (but not including) this cursor position. If before is specified, after must not be specified.</param>
            
        /// <remarks>AUTH NEEDED</remarks>
        let GetUsersRecentlyPlayedTracks(limit: int , after: DateTime option, before: DateTime option)=
            
            let limit = FitInDefaultRange limit
            
            [
                sprintf "%s/me/player/recently-played?limit=%i" APIBase limit
            
                (if after.IsSome 
                 then sprintf "&after=%i" after.Value.ToUnixTimeMillisecondsPoly
                 else "")
                (if before.IsSome 
                 then sprintf "&before=%i" before.Value.ToUnixTimeMillisecondsPoly
                 else "")
            ]

        //#endregion

        //#region Playlists

        /// <summary>
        ///     Get a list of the playlists owned or followed by a Spotify user.
        /// </summary>
        /// <param name="userId">The user's Spotify user ID.</param>
        /// <param name="limit">The maximum number of playlists to . Default: 20. Minimum: 1. Maximum: 50. </param>
        /// <param name="offset">The index of the first playlist to . Default: 0 (the first object)</param>
            
        /// <remarks>AUTH NEEDED</remarks>
        let GetUserPlaylists(userId: string) (limit: int) (offset: int) =
            let limit = FitInDefaultRange limit
            sprintf "%s/playlists/users/%s?limit=%i&offset=%i" APIBase userId limit offset
            
            

        /// <summary>
        ///     Get a playlist owned by a Spotify user.
        /// </summary>
        /// <param name="userId">The user's Spotify user ID.</param>
        /// <param name="playlistId">The Spotify ID for the playlist.</param>
        /// <param name="fields">
        ///     Filters for the query: a comma-separated list of the fields to . If omitted, all fields are
        ///     ed.
        /// </param>
        /// <param name="market">An ISO 3166-1 alpha-2 country code. Provide this parameter if you want to apply Track Relinking.</param>
            
        /// <remarks>AUTH NEEDED</remarks>
        let GetPlaylist(userId: string) (playlistId: string) (market: string) =
            getMarket '&' market
            |> sprintf "%s/users/%s/playlists/%s%s" APIBase  userId  playlistId

        /// <summary>
        ///     Get full details of the tracks of a playlist owned by a Spotify user.
        /// </summary>
        /// <param name="userId">The user's Spotify user ID.</param>
        /// <param name="playlistId">The Spotify ID for the playlist.</param>
        /// <param name="fields">
        ///     Filters for the query: a comma-separated list of the fields to . If omitted, all fields are
        ///     ed.
        /// </param>
        /// <param name="limit">The maximum number of tracks to . Default: 100. Minimum: 1. Maximum: 100.</param>
        /// <param name="offset">The index of the first object to . Default: 0 (i.e., the first object)</param>
        /// <param name="market">An ISO 3166-1 alpha-2 country code. Provide this parameter if you want to apply Track Relinking.</param>
            
        /// <remarks>AUTH NEEDED</remarks>
        let GetPlaylistTracks (offset: int ) (market: string) (limit: int) (userId: string) (playlistId: string) =

            let limit = Math.Min(limit, 100)
            getMarket '&' market
            |> sprintf "%s/users/%s/playlists/%s/tracks&limit=%i&offset=%i%s" APIBase userId playlistId limit offset            
            

        /// <summary>
        ///     Create a playlist for a Spotify user. (The playlist will be empty until you add tracks.)
        /// </summary>
        /// <param name="userId">The user's Spotify user ID.</param>
        /// <param name="playlistName">
        ///     The name for the  playlist, for example "Your Coolest Playlist". This name does not need
        ///     to be unique.
        /// </param>
        /// <param name="is">
        ///     default true. If true the playlist will be , if false it will be private. To be able to
        ///     create private playlists, the user must have granted the playlist-modify-private scope.
        /// </param>
            
        /// <remarks>AUTH NEEDED</remarks>
        let CreatePlaylist(userId: string) =

                sprintf "%s/users/%s/playlists" APIBase userId
            

        /// <summary>
        ///     Change a playlist’s name and /private state. (The user must, of course, own the playlist.)
        /// </summary>
        /// <param name="userId">The user's Spotify user ID.</param>
        /// <param name="playlistId">The Spotify ID for the playlist.</param>
            
        /// <remarks>AUTH NEEDED</remarks>
        let UpdatePlaylist(userId: string) (playlistId: string) =
                sprintf "%s/users/%s/playlists/%s" APIBase userId playlistId
            

        /// <summary>
        ///     Replace all the tracks in a playlist, overwriting its existing tracks. This powerful request can be useful for
        ///     replacing tracks, re-ordering existing tracks, or clearing the playlist.
        /// </summary>
        /// <param name="userId">The user's Spotify user ID.</param>
        /// <param name="playlistId">The Spotify ID for the playlist.</param>
            
        /// <remarks>AUTH NEEDED</remarks>
        let ReplacePlaylistTracks(userId: string) (playlistId: string) =

                sprintf "%s/users/%s/playlists/%s/tracks" APIBase userId playlistId
            

        /// <summary>
        ///     Remove one or more tracks from a user’s playlist.
        /// </summary>
        /// <param name="userId">The user's Spotify user ID.</param>
        /// <param name="playlistId">The Spotify ID for the playlist.</param>
        /// <param name="uris">
        ///     array of objects containing Spotify URI strings (and their position in the playlist). A maximum of
        ///     100 objects can be sent at once.
        /// </param>
            
        /// <remarks>AUTH NEEDED</remarks>
        [<Obsolete("Not Used")>]
        let RemovePlaylistTracks(userId: string) (playlistId: string) (uris: DeleteTrackUri list) =
            
            sprintf "%s/users/%s/playlists/%s/tracks" APIBase userId playlistId
            

        /// <summary>
        ///     Add one or more tracks to a user’s playlist.
        /// </summary>
        /// <param name="userId">The user's Spotify user ID.</param>
        /// <param name="playlistId">The Spotify ID for the playlist.</param>
        /// <param name="uris">A list of Spotify track URIs to add</param>
        /// <param name="position">The position to insert the tracks, a zero-based index</param>
            
        /// <remarks>AUTH NEEDED</remarks>
        let AddPlaylistTracks(userId: string) (playlistId: string) (uris: string list) (position: int option) =

            if (position.IsSome) then
                sprintf "?position=%i" position.Value 
            else ""
            |>  sprintf "%s/users/%s/playlists/%s/tracks%s" APIBase userId playlistId
            

        /// <summary>
        ///     Reorder a track or a group of tracks in a playlist.
        /// </summary>
        /// <param name="userId">The user's Spotify user ID.</param>
        /// <param name="playlistId">The Spotify ID for the playlist.</param>
            
        /// <remarks>AUTH NEEDED</remarks>
        let ReorderPlaylist(userId: string) (playlistId: string) =

                sprintf "%s/users/%s/playlists/%s/tracks" APIBase userId playlistId
            

        //#endregion Playlists

        //#region Profiles

        /// <summary>
        ///     Get detailed profile information about the current user (including the current user’s username).
        /// </summary>
            
        /// <remarks>AUTH NEEDED</remarks>
        let GetPrivateProfile = sprintf "%s/me" APIBase
            

        /// <summary>
        ///     Get  profile information about a Spotify user.
        /// </summary>
        /// <param name="userId">The user's Spotify user ID.</param>
            
        let GetProfile(userId: string) = sprintf "%s/users/%s" APIBase userId
            

        //#endregion Profiles

        //#region Tracks

        /// <summary>
        ///     Get Spotify catalog information for multiple tracks based on their Spotify IDs.
        /// </summary>
        /// <param name="ids">A list of the Spotify IDs for the tracks. Maximum: 50 IDs.</param>
        /// <param name="market">An ISO 3166-1 alpha-2 country code. Provide this parameter if you want to apply Track Relinking.</param>
            
        let GetSeveralTracks(ids: string list) (market: string) =
            let market = getMarket '?' market
            
            if String.IsNullOrEmpty(market) then
                sprintf "%s/tracks" APIBase
            else
                sprintf "%s/tracks%s%s" APIBase market (ListToQueryParams "ids" '&' ids)
            

        /// <summary>
        ///     Get Spotify catalog information for a single track identified by its unique Spotify ID.
        /// </summary>
        /// <param name="id">The Spotify ID for the track.</param>
        /// <param name="market">An ISO 3166-1 alpha-2 country code. Provide this parameter if you want to apply Track Relinking.</param>
            
        let GetTrack (market: string) (id: string) =
                    sprintf "%s/tracks/%s%s" APIBase id (getMarket '?' market)
            
        let Track = GetTrack ""

        /// <summary>
        ///     Get a detailed audio analysis for a single track identified by its unique Spotify ID.
        /// </summary>
        /// <param name="id">The Spotify ID for the track.</param>
            
        /// <remarks>AUTH NEEDED</remarks>
        let GetAudioAnalysis(id: string) =
                sprintf "%s/audio-analysis/%s" APIBase id
            

        /// <summary>
        ///     Get audio feature information for a single track identified by its unique Spotify ID.
        /// </summary>
        /// <param name="id">The Spotify ID for the track.</param>
            
        /// <remarks>AUTH NEEDED</remarks>
        let GetAudioFeatures(id: string) =
                sprintf "%s/audio-features/%s" APIBase id
            

        /// <summary>
        ///     Get audio features for multiple tracks based on their Spotify IDs.
        /// </summary>
        /// <param name="ids">A list of Spotify Track-IDs. Maximum: 100 IDs.</param>
            
        /// <remarks>AUTH NEEDED</remarks>
        let GetSeveralAudioFeatures(ids: string list) =
                sprintf "%s/audio-features%s" APIBase (ListToQueryParamsWithLimit "ids" '?' ids 100)
            

        //#endregion Tracks

        //#region Player

        /// <summary>
        ///     Get information about a user’s available devices.
        /// </summary>
            
        let GetDevices = sprintf "%s/me/player/devices" APIBase
            

        /// <summary>
        ///     Get information about the user’s current playback state, including track, track progress, and active device.
        /// </summary>
        /// <param name="market">An ISO 3166-1 alpha-2 country code. Provide this parameter if you want to apply Track Relinking.</param>
            
        let GetPlayback(market: string) =
            sprintf "%s/me/player%s" APIBase (getMarket '?' market)
            

        /// <summary>
        ///     Get the object currently being played on the user’s Spotify account.
        /// </summary>
        /// <param name="market">An ISO 3166-1 alpha-2 country code. Provide this parameter if you want to apply Track Relinking.</param>
            
        let GetPlayingTrack(market: string) =
                  sprintf "%s/me/player/currently-playing%s" APIBase (getMarket '?' market)
            

        /// <summary>
        ///     Transfer playback to a  device and determine if it should start playing.
        /// </summary>
            
        let TransferPlayback = sprintf "%s/me/player" APIBase
            
        let private getDeviceId = GetOptionalParamString "deviceId"

        /// <summary>
        ///     Start a  context or resume current playback on the user’s active device.
        /// </summary>
        /// <param name="deviceId">The id of the device this command is targeting. If not supplied, the user's currently active device is the target.</param>
            
        let ResumePlayback(deviceId: string) =
            sprintf "%s/me/player/play%s" APIBase (getDeviceId '?' deviceId )
            

        /// <summary>
        ///     Pause playback on the user’s account.
        /// </summary>
        /// <param name="deviceId">The id of the device this command is targeting. If not supplied, the user's currently active device is the target.</param>
            
        let PausePlayback(deviceId: string) =
            sprintf "%s/me/player/pause%s" APIBase (getDeviceId '?' deviceId )
            

        /// <summary>
        ///     Skips to next track in the user’s queue.
        /// </summary>
        /// <param name="deviceId">The id of the device this command is targeting. If not supplied, the user's currently active device is the target.</param>
            
        let SkipPlaybackToNext(deviceId: string) =
            sprintf "%s/me/player/next%s" APIBase (getDeviceId '?' deviceId )
            

        /// <summary>
        ///     Skips to previous track in the user’s queue.
        ///     Note that this will ALWAYS skip to the previous track, regardless of the current track’s progress.
        ///     ing to the start of the current track should be performed using the https://api.spotify.com/v1/me/player/seek endpoint.
        /// </summary>
        /// <param name="deviceId">The id of the device this command is targeting. If not supplied, the user's currently active device is the target.</param>
            
        let SkipPlaybackToPrevious(deviceId: string) =
            sprintf "%s/me/player/previous%s" APIBase (getDeviceId '?' deviceId )
            

        /// <summary>
        ///     Seeks to the given position in the user’s currently playing track.
        /// </summary>
        /// <param name="positionMs">The position in milliseconds to seek to. Must be a positive number. 
        /// Passing in a position that is greater than the length of the track will cause the player to start playing the next song.</param>
        /// <param name="deviceId">The id of the device this command is targeting. If not supplied, the user's currently active device is the target.</param>
            
        let SeekPlayback(positionMs: int) (deviceId: string) =
            sprintf "%s/me/player/seek?position_ms=%i%s" APIBase positionMs (getDeviceId '&' deviceId )
            

        /// <summary>
        ///     Set the repeat mode for the user’s playback. Options are repeat-track, repeat-context, and off.
        /// </summary>
        /// <param name="repeatState">track, context or off.</param>
        /// <param name="deviceId">The id of the device this command is targeting. If not supplied, the user's currently active device is the target.</param>
            
        let SetRepeatMode(repeatState: RepeatState) (deviceId: string) =
            
                    sprintf "%s/me/player/repeat%s%s" APIBase (StringAtttributeToQueryString "repeatState" '?' repeatState) (getDeviceId '&' deviceId)
            

        /// <summary>
        ///     Set the volume for the user’s current playback device.
        /// </summary>
        /// <param name="volumePercent">Integer. The volume to set. Must be a value from 0 to 100 inclusive.</param>
        /// <param name="deviceId">The id of the device this command is targeting. If not supplied, the user's currently active device is the target.</param>
            
        let SetVolume(volumePercent: int) (deviceId: string) =
            sprintf "%s/me/player/volume?volume_percent=%i%s" APIBase volumePercent (getDeviceId '&' deviceId)
            

        /// <summary>
        ///     Toggle shuffle on or off for user’s playback.
        /// </summary>
        /// <param name="shuffle">True of False.</param>
        /// <param name="deviceId">The id of the device this command is targeting. If not supplied, the user's currently active device is the target.</param>
            
        let SetShuffle(shuffle: bool) (deviceId: string) =
            sprintf "%s/me/player/shuffle?state=%b%s" APIBase shuffle (getDeviceId '&' deviceId)
        //#endregion
        
    end