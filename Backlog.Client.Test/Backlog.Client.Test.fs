module Backlog.Client.Test

// https://github.com/fsharp/FsCheck/blob/master/Docs/Documentation.md
// https://github.com/fsharp/FsUnit
// https://code.google.com/p/unquote/

open Backlog.Client.Api

open FsUnit
open FsCheck
open NUnit.Framework
open Swensen.Unquote

// all tests are failing

// Note on FsCheck tests: The NUnit test runner will still green-light failing tests with Check.Quick 
// even though it reports them as failing. Use Check.QuickThrowOnFailure instead.

[<Test>]
let ``Getting user with id 76964 from Backlog space should return user wlee with email address "wlee at isabot.net"`` ()=
    let user = Users.getUserById 76964
    user.IsSome |> should equal true
    user.Value.MailAddress |> should equal "wlee@isabot.net"

[<Test>]
let ``Getting all users from sample Backlog space should return a list of 2 users: wlee and agata`` () =
    let users = Users.getUsers
    users.IsSome |> should equal true
    users.Value.Length |> should equal 2
    users.Value |> List.map (fun u -> u.UserId) |> should contain "wlee"
    users.Value |> List.map (fun u -> u.UserId) |> should contain "agata"
