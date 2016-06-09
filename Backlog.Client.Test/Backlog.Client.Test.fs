﻿module Backlog.Client.Test

#if DEBUG
System.Linq.Enumerable.Count([]) |> ignore
#endif
// https://github.com/fsharp/FsCheck/blob/master/Docs/Documentation.md
// https://github.com/fsharp/FsUnit
// https://code.google.com/p/unquote/

open Backlog.Client.Api
open Backlog.Client.Models

open FsUnit
open FsCheck
open NUnit.Framework
open Swensen.Unquote

// all tests are failing

// Note on FsCheck tests: The NUnit test runner will still green-light failing tests with Check.Quick
// even though it reports them as failing. Use Check.QuickThrowOnFailure instead.

[<Test>]
let ``Getting update form values for a user should only return the fields that aren't null/None`` ()=
    let user = new User()
    user.Name <- "someName"
    user.RoleType <- 1
    let expected = Seq.sort ["name", "someName"; "roleType", "1"]
    let outcome = user.ToUpdateFormValues |> Seq.sort
    outcome |> should equal expected

    let user = new User()
    user.Name <- "someName"
    user.MailAddress <- "wlee@isabot.net"
    let outcome1 = Seq.sort(["name", "someName"; "mailAddress", "wlee@isabot.net"])
    user.ToUpdateFormValues |> Seq.sort |> should equal outcome1

    let user = new User()
    user.ToUpdateFormValues |> Seq.sort |> should equal []

[<Test>]
let ``Getting user with id 76964 from Backlog space should return user wlee with email address "wlee at isabot.net"`` ()=
    let userResponse = Users.getUserById 76964
    userResponse.StatusCode |> should equal 200
    userResponse.Value.IsSome |> should equal true
    userResponse.Value.Value.MailAddress |> should equal "wlee@isabot.net"

[<Test>]
let ``Getting all users from sample Backlog space should at least contain these users: wlee and agata`` () =
    let usersResponse = Users.getUsers()
    usersResponse.StatusCode |> should equal 200
    usersResponse.Value.IsSome |> should equal true
    usersResponse.Value.Value |> List.map (fun u -> u.UserId) |> should contain "wlee"
    usersResponse.Value.Value |> List.map (fun u -> u.UserId) |> should contain "agata"

[<Test>]
let ``Adding user should return an updated user object with id and after removing it, it should no longer show up in all users list`` () =
    let newUser = User("sampleUserId", "Sample User", "sampleP@ssw0rd123", 2, "sample@user.com")
    let createdResponse = Users.addUser newUser

    createdResponse .StatusCode |> should equal 201
    createdResponse.Value.IsSome |> should equal true
    createdResponse .Value.Value.Id |> should be (greaterThan 0)

    let createdUser = createdResponse.Value.Value

    let hasDeletedUser = Users.deleteUser createdUser.Id
    hasDeletedUser.StatusCode |> should equal 200
    let usersResponse = Users.getUsers()
    usersResponse.Value.Value |> List.map (fun u -> u.UserId) |> should not' (contain "sampleUserId")

[<Test>]
let ``Adding and updating new user should return updated user object, user lists should no longer yield newly created user after deleting it`` () =
    let newUser = User("otherUserId", "Sample User", "sampleP@ssw0rd123", 2, "sample@user.com")
    let createdResponse = Users.addUser newUser
    let usersResponse = Users.getUsers()

    createdResponse.Value.IsSome |> should equal true
    createdResponse.Value.Value.Id |> should be (greaterThan 0)
    usersResponse.Value.Value |> List.map (fun u -> u.UserId) |> should contain "otherUserId"

    let updatingUser = createdResponse.Value.Value
    updatingUser.Name <- "Random username 123#"
    let updatedResponse = Users.updateUser updatingUser

    let usersResponse = Users.getUsers()
    usersResponse.Value.Value |> List.map (fun u -> u.Name) |> should contain "Random username 123#"

    let hasDeletedUser = Users.deleteUser createdResponse.Value.Value.Id
    hasDeletedUser.StatusCode |> should equal 200
    let usersResponse = Users.getUsers()
    usersResponse.Value.Value |> List.map (fun u -> u.UserId) |> should not' (contain "otherUserId")