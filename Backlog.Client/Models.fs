module Models

open Newtonsoft.Json

type User (id:int, userId:string, name:string, roleType:int, lang:string, mailAddress:string) =
    member this.Id = id
    member this.UserId = userId
    member this.Name = name
    member this.RoleType = roleType
    member this.Lang = lang
    member this.MailAddress = mailAddress

    member this.ToFormValues = seq ["id", this.Id.ToString(); "userId", this.UserId; "name", this.Name; "roleType", this.RoleType.ToString(); "lang", this.Lang; "mailAddress", this.MailAddress]

