namespace SpotifyFS
module Enums =
    open System
    open System.Reflection
    open System.Linq
    open System.Collections.Generic

    [<AttributeUsage(AttributeTargets.All, AllowMultiple = false)>]
    type StringAttribute(text) = inherit Attribute() with
        member __.Text = text

        static member Get(item: obj, separator: string) : string option =  

            let itemType = item.GetType() 
            let getFieldStringAttributes (field: MemberInfo) = field.GetCustomAttributes(typeof<StringAttribute>, false).OfType<StringAttribute>()

            let getPropertyStringAttribute propName =

                itemType.GetField(string propName)
                |> getFieldStringAttributes
                |> Seq.tryHead

            let getStringAttributes a =             
                    a
                    |> Seq.map getPropertyStringAttribute                    
                    |> Seq.map (function Some x -> x.Text  |_ -> "")
                    |> Seq.toList                

            match item with
            | :? Enum as en ->  
                    let arr = Enum.GetValues(itemType) |> Seq.cast<Enum> |> Seq.toArray
                    
                    let filtered = arr |> Seq.filter (fun v -> en.HasFlag(v))  
                    
                    let attributes = filtered |> getStringAttributes
                    attributes
                    |> function attrs ->
                                match attrs.Length with
                                | 0  -> None
                                | _  -> Some(String.Join(separator, attrs))

            | _ -> itemType.GetCustomAttributes(typeof<StringAttribute>, false) 
                       |> Seq.tryHead
                       |> function
                          | Some x -> Some (String.Join(separator, x))
                          | None -> None
            
        static member GetOrEmptyString(item, sep) =
                (StringAttribute.Get(item, sep)
                |> function 
                    | Some x -> x
                    | _ -> "" )
    
        
    type AcceptedParamTypes = 
        | Word of String
        | Date of DateTime * string
        | Empty

    let GetOptionalParam label p (value: AcceptedParamTypes) =
        match value with
        | Word str when  not <| String.IsNullOrEmpty str 
            -> sprintf "%c%s%s" p label str                
        | Date (date, fmStr) 
            -> sprintf "%c%s%s" p label (date.ToString(fmStr))
        | Empty | _ 
            -> ""

    let GetOptionalParamString label p value =
        GetOptionalParam label p (if String.IsNullOrEmpty(value) then Empty else Word value)

    let FitInRange (min: int, max: int) value = Math.Max(min, Math.Min(value, max))
    let FitInDefaultRange = FitInRange (1, 50)

    let GetOptionalParamWithDefault label p value defaultVal =
        match value with
        | Word str when String.IsNullOrEmpty str
            -> GetOptionalParam label p defaultVal
        | Word _
            -> GetOptionalParam label p value

        | Date (date, _) when date = DateTime()
            -> GetOptionalParam label p defaultVal
        | Date _ as d ->
            GetOptionalParam label p d

        | Empty 
            -> GetOptionalParam label p defaultVal

    let StringAtttributeToQueryString label p item =
            GetOptionalParam label p (StringAttribute.Get(item, ",")
                                        |> function 
                                        | Some x -> Word x
                                        | _ -> Empty )
                                             
    let ListToQueryParams label p (items: string list) =
            GetOptionalParam label p (if items.Length > 0 
                                        then Word( String.Join(",", items) )
                                        else Empty)

    let ListToQueryParamsWithLimit label p (items: string list) limit =
            GetOptionalParam label p (if items.Length > 0 
                                        then Word( String.Join(",", items |> List.take limit) )
                                        else Empty)

    type DateTime with
            member time.ToUnixTimeMillisecondsPoly =        
                int <| time.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
     
       
    [<Flags>]
    type SearchType =            
        | [<String("artist")>] 
          Artist = 1       
        | [<String("album")>]
          Album = 2        
        | [<String("track")>]
          Track = 4        
        | [<String("playlist")>]
          Playlist = 8        
        | [<String("track,album,artist,playlist")>]
          All = 16
         

    [<Flags>]
    type AlbumType =
       | [<String("album")>]
         Album = 1
       | [<String("single")>]
         Single = 2
       | [<String("compilation")>]
         Compilation = 4
       | [<String("appears_on")>]
         AppearsOn = 8
       | [<String("album,single,compilation,appears_on")>]
         All = 16


    [<Flags>]
    type Scope =    
        | [<String("")>]
          None = 1

        | [<String("playlist-modify-public")>]
          PlaylistModifyPublic = 2

        | [<String("playlist-modify-private")>]
          PlaylistModifyPrivate = 4

        | [<String("playlist-read-private")>]
          PlaylistReadPrivate = 8

        | [<String("streaming")>]
          Streaming = 16

        | [<String("user-read-private")>]
          UserReadPrivate = 32

        | [<String("user-read-email")>]
          UserReadEmail = 64

        | [<String("user-library-read")>]
          UserLibraryRead = 128

        | [<String("user-library-modify")>]
          UserLibraryModify = 256

        | [<String("user-follow-modify")>]
          UserFollowModify = 512

        | [<String("user-follow-read")>]
          UserFollowRead = 1024

        | [<String("user-read-birthdate")>]
          UserReadBirthdate = 2048

        | [<String("user-top-read")>]
          UserTopRead = 4096 



    [<Flags>]
    type FollowType =
        | [<String("artist")>]
          Artist = 1

        | [<String("user")>]
          User = 2      

    [<Flags>]
    type TimeRangeType =
        
        | [<String("long_term")>]
          LongTerm = 1

        | [<String("medium_term")>]
          MediumTerm = 2

        | [<String("short_term")>]
          ShortTerm = 4
          
    [<Flags>]
    type RepeatState =
    
       | [<String("track")>]
         Track = 1

       | [<String("context")>]
         Context = 2

       | [<String("off")>]
         Off = 4
    
    [<AutoOpen>]
    module Extensions = 
        
        type Scope with 
            member this.GetStringAttribute(sep): string =
                StringAttribute.Get(this, sep)
                |> function 
                   | Some x -> x
                   | _ -> "" 
                                             