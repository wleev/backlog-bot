module Backlog.Client.Api

open Models
open Net

type Http =
    static member Request(url : string, requestType : RequestType, ?data : seq<string * string>) =
        let dataVal =
            match data with
            | Some d -> d
            | None -> Seq.empty<string * string>
        match requestType with
        | Get -> Some(HttpGet url)
        | Delete -> Some(HttpDelete url)
        | Post -> Some(HttpPost url dataVal)
        | Patch -> Some(HttpPatch url dataVal)

type Response<'T>(statusCode : int, value : 'T option) =
    member this.StatusCode = statusCode
    member this.Value = value

module Users =
    let baseUrl = "https://boop.backlogtool.com/api/v2/users"

    let getUsers() =
        match Http.Request(baseUrl, Get) with
        | Some response -> new Response<User list>(response.StatusCode, HttpBodyToEntity<list<User>>(response.Body))
        | None -> new Response<User list>(-1, None)

    let getUserById (id : int) =
        let getUrl = baseUrl + "/" + id.ToString()
        match Http.Request(getUrl, Get) with
        | Some response -> new Response<User>(response.StatusCode, HttpBodyToEntity<User> response.Body)
        | None -> new Response<User>(-1, None)

    let addUser (user : User) =
        match Http.Request(baseUrl, Post, user.ToAddFormValues) with
        | Some response -> new Response<User>(response.StatusCode, HttpBodyToEntity<User> response.Body)
        | None -> new Response<User>(-1, None)

    let updateUser (user : User) =
        let updateUrl = baseUrl + "/" + user.Id.ToString()
        match Http.Request(updateUrl, Patch, user.ToUpdateFormValues) with
        | Some response -> new Response<User>(response.StatusCode, HttpBodyToEntity<User> response.Body)
        | None -> new Response<User>(-1, None)

    let deleteUser (id : int) =
        let deleteUrl = baseUrl + "/" + id.ToString()
        match Http.Request(deleteUrl, Delete) with
        | Some response -> new Response<int>(response.StatusCode, Some id)
        | _ -> new Response<int>(-1, Some id)