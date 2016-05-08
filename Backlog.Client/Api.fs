module Backlog.Client.Api

open Models
open Net

type Http = 
    static member Request ( url: string, requestType : RequestType, ?data: seq<string * string>) =
        let dataVal = match data with Some d -> d | None -> Seq.empty<string * string>
        match requestType with
        | Get -> Some ( HttpGet url )
        | Delete -> Some ( HttpDelete url )
        | Post -> Some ( HttpPost url dataVal)
        | Patch -> Some ( HttpPost url dataVal)
        | _ -> None

    

module Users = 
    let baseUrl = "https://boop.backlogtool.com/api/v2/users"
    
    let getUsers =
        match Http.Request(baseUrl, Get) with 
        | Some response -> HttpBodyToEntity<list<User>> response.Body
        | None -> Some []

    let getUserById userId =
        let getUrl = baseUrl + "/" + userId.ToString()
        match Http.Request(getUrl, Get) with
         | Some response -> HttpBodyToEntity<User> response.Body
         | None -> None

    let addUser (user:User) =
        match Http.Request(baseUrl, Post, user.ToFormValues) with
        | Some response -> HttpBodyToEntity<User> response.Body
        | None -> None

    let updateUser (user:User) =
        let updateUrl = baseUrl + "/" + user.Id.ToString()
        match Http.Request(updateUrl, Patch, user.ToFormValues) with
        | Some response -> HttpBodyToEntity<User> response.Body
        | None -> None

    let deleteUser id =
        let deleteUrl = baseUrl + "/" + id.ToString()
        match Http.Request(deleteUrl, Delete) with
        | Some response -> response.StatusCode.Equals 200
        | _ -> false