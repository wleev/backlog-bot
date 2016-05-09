module Backlog.Client.Models

open Newtonsoft.Json

type User [<JsonConstructor>](id:int, userId:string, name:string, password:string, roleType:int, lang:string, mailAddress:string) =
    new(userId:string, name:string, password:string, roleType:int, mailAddress:string) = User(0, userId, name, password, roleType, "en", mailAddress)

    member val Id = id
    member val UserId = userId
    member val Name = name with get,set
    member val Password = password with get,set
    member val RoleType = roleType with get,set
    member val Lang = lang
    member val MailAddress = mailAddress with get,set

    member this.ToAddFormValues = seq ["userId", this.UserId; "name", this.Name; "password", this.Password; "roleType", this.RoleType.ToString(); "mailAddress", this.MailAddress]
    member this.ToUpdateFormValues = seq ["name", this.Name; "roleType", this.RoleType.ToString(); "mailAddress", this.MailAddress]

