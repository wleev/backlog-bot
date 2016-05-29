module Backlog.Client.Models

open Newtonsoft.Json

type User(id : int, userId : string, name : string, password : string, roleType : int, lang : string, mailAddress : string) =
    new(userId : string, name : string, password : string, roleType : int, mailAddress : string) =
        User(0, userId, name, password, roleType, "en", mailAddress)
    new() = User("", "", "", -1, "")
    member val Id = id with get, set
    member val UserId = userId with get, set
    member val Name = name with get, set
    member val Password = password with get, set
    member val RoleType = roleType with get, set
    member val Lang = lang
    member val MailAddress = mailAddress with get, set

    member this.ToAddFormValues =
        seq [ "userId", this.UserId
              "name", this.Name
              "password", this.Password
              "roleType", this.RoleType.ToString()
              "mailAddress", this.MailAddress ]

    member this.ToUpdateFormValues =
        Seq.collect (fun (key, value) ->
            match value with
            | "" -> Seq.empty
            | null -> Seq.empty
            | v -> seq [ key, v ]) [ "name", this.Name
                                     "mailAddress", this.MailAddress
                                     "password", this.Password ]
        |> Seq.append (Seq.collect (fun (key, value) ->
                           match value with
                           | -1 -> Seq.empty
                           | v -> seq [ key, v.ToString() ]) [ "roleType", this.RoleType ])

type User1 =
    { id : int option
      userId : string option
      name : string option
      password : string option
      roleType : int option
      lang : string option
      mailAddress : string option }

let toAddFormValues (user : User1) =
    seq [ "userId", user.userId.Value
          "name", user.name.Value
          "password", user.password.Value
          "roleType", user.roleType.Value.ToString()
          "mailAddress", user.mailAddress.Value ]

let toUpdateFormValues (user : User1) =
    [ "userId", user.userId
      "mailAddress", user.mailAddress ]
    |> List.filter (fun (key, value) -> value.IsSome)
    |> List.map (fun (key, value) -> (key, value.Value))
    |> List.append ([ "roleType", user.roleType ]
                    |> List.filter (fun (key, value) -> value.IsSome)
                    |> List.map (fun (key, value) -> (key, value.ToString())))

let toUpdateFormValues1 (user : User1) =
    Seq.collect (fun (key, value) ->
        match value with
        | Some v -> seq [ key, v ]
        | None -> Seq.empty) [ "userId", user.userId
                               "mailAddress", user.mailAddress ]
    |> Seq.append (Seq.collect (fun (key, value) ->
                       match value with
                       | Some v -> seq [ key, v.ToString() ]
                       | None -> Seq.empty) [ "roleType", user.roleType ])