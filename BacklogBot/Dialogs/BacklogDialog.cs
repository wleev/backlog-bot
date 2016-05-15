﻿using Backlog.Client;
using BacklogBot.Dialogs.Orders;
using BacklogBot.Extensions;
using BacklogBot.Models;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using static Backlog.Client.Models;

namespace BacklogBot.Dialogs
{
    [LuisModel("26b510af-c1c2-46b8-adb1-010ac29d93a0", "848346513fe644c0850fb1bf6669cac7")]
    [Serializable]
    public class BacklogDialog : LuisDialog<UserCreationOrder>
    {
        public const string ENTITY_USER_ID = "entity.backlog.user.id";
        public const string ENTITY_USER_NAME = "entity.backlog.user.name";
        public const string ENTITY_USER_PASSWORD = "entity.backlog.user.password";
        public const string ENTITY_USER_ROLE = "entity.backlog.user.role";
        public const string ENTITY_USER_EMAIL = "entity.backlog.user.email";

        public const string INTENT_DISK_USAGE = "intent.backlog.diskusage";
        public const string INTENT_USER_ALL = "intent.backlog.user.all";
        public const string INTENT_USER_CREATE = "intent.backlog.user.create";
        public const string INTENT_USER_REMOVE = "intent.backlog.user.delete";
        public const string INTENT_USER_UPDATE = "intent.backlog.user.update";

        public bool TryParseUser(LuisResult result, out BacklogUser user)
        {
            user = new BacklogUser();

            EntityRecommendation id;
            EntityRecommendation name;
            EntityRecommendation email;
            EntityRecommendation role;
            EntityRecommendation password;

            var hasParsed = true;
            hasParsed &= result.TryFindEntity(ENTITY_USER_ID, out id);
            hasParsed &= result.TryFindEntity(ENTITY_USER_NAME, out name);
            hasParsed &= result.TryFindEntity(ENTITY_USER_EMAIL, out email);
            hasParsed &= result.TryFindEntity(ENTITY_USER_ROLE, out role);
            hasParsed &= result.TryFindEntity(ENTITY_USER_PASSWORD, out password);

            user.Id = id?.Entity;
            user.Name = name?.Entity;
            user.Email = email?.Entity;
            user.Role = role?.Entity;
            user.Password = password?.Entity;

            //var roleObj = Role.FindMatchingRoleByName(role.Entity);
            //if (roleObj != null)
            //    throw new RoleNotFoundException();

            //var userToAdd = new Models.User(id.Entity, name.Entity, password.Entity, roleObj.Id, email.Entity);
            //var userResult = Api.Users.addUser(userToAdd);
            //if (userResult.IsSome())
            //    user = userResult.Value;

            return hasParsed;
        }

        public bool TryGetUser(LuisResult result, out BacklogUser user)
        {
            user = null;

            EntityRecommendation id;

            var hasParsed = true;
            hasParsed &= result.TryFindEntity(ENTITY_USER_ID, out id);

            //where we should be calling the api
            if (hasParsed)
                user = new BacklogUser { Id = id.Entity };

            return user != null;
        }

        public bool TryRemoveUser(LuisResult result)
        {
            EntityRecommendation id;

            var hasParsed = true;
            hasParsed &= result.TryFindEntity(ENTITY_USER_ID, out id);

            //where we should be calling the api to remove user

            return hasParsed;
        }

        public bool TryUpdateUser(LuisResult result, out BacklogUser user)
        {
            BacklogUser currentUser;
            TryGetUser(result, out currentUser);

            user = currentUser;

            EntityRecommendation name;
            EntityRecommendation email;
            EntityRecommendation role;
            EntityRecommendation password;

            if (result.TryFindEntity(ENTITY_USER_NAME, out name))
                user.Name = name.Entity;

            if (result.TryFindEntity(ENTITY_USER_EMAIL, out email))
                user.Email = email.Entity;

            if (result.TryFindEntity(ENTITY_USER_ROLE, out role))
                user.Role = role.Entity;

            if (result.TryFindEntity(ENTITY_USER_PASSWORD, out password))
                user.Password = password.Entity;

            return !currentUser.Equals(user);
        }

        [LuisIntent(INTENT_DISK_USAGE)]
        public async Task GetDiskUsage(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("You requested disk usage statistics.");
            context.Wait(MessageReceived);
        }

        [LuisIntent(INTENT_USER_ALL)]
        public async Task GetAllUsers(IDialogContext context, LuisResult result)
        {
            try
            {
                var users = Api.Users.getUsers();
                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine("# List of all users");
                foreach (var user in users.Value)
                {
                    messageBuilder.AppendLine($"* {user.UserId} - {user.Name} - {user.MailAddress} - {user.RoleType}");
                }
                messageBuilder.AppendLine("---");
                messageBuilder.AppendLine("Those were all the users!");

                await context.PostAsync(messageBuilder.ToString());
            }
            catch (Exception e)
            {
                await context.PostAsync("I messed up! Please forgive me benevolent human overlord...");
            }
            finally
            {
                context.Wait(MessageReceived);
            }
        }

        [LuisIntent(INTENT_USER_CREATE)]
        public async Task CreateBacklogUser(IDialogContext context, LuisResult result)
        {
            BacklogUser newUser;
            bool extraInputRequired = false;
            try
            {
                if (TryParseUser(result, out newUser))
                {
                    var createdUser = CreateUser(newUser.ToApiUser());
                    await context.PostAsync($"You created a new user with id {createdUser.UserId} (internal id: {createdUser.Id}).");
                }
                else
                {
                    extraInputRequired = true;
                    var creationOrder = new UserCreationOrder(newUser);
                    var formDialog = new FormDialog<UserCreationOrder>(creationOrder, UserCreationOrder.CreateForm, FormOptions.PromptInStart);
                    context.Call(formDialog, UserCreationOrderCompleted);
                }
            }
            catch (RoleNotFoundException)
            {
                await context.PostAsync("Could not find the specified role.");
            }
            catch (Exception e)
            {
                await context.PostAsync("I messed up! Please forgive me benevolent human overlord...");
            }
            finally
            {
                if (!extraInputRequired)
                    context.Wait(MessageReceived);
            }
        }

        private async Task UserCreationOrderCompleted(IDialogContext context, IAwaitable<UserCreationOrder> result)
        {
            var order = await result;
            var newUser = new User(order.Id, order.Name, order.Password, order.Role.Value, order.MailAddress);
            var createdUser = CreateUser(newUser);
            await context.PostAsync($"You created a new user with id {createdUser.UserId} (internal id: {createdUser.Id}).");

            context.Wait(MessageReceived);
        }

        private User CreateUser(User user)
        {
            var userOption = Api.Users.addUser(user);

            //need to handle this better
            if (userOption.IsNone())
                throw new Exception("Placeholder exception for failing to create user.");

            return userOption.Value;
        }

        [LuisIntent(INTENT_USER_UPDATE)]
        public async Task UpdateBacklogUser(IDialogContext context, LuisResult result)
        {
            BacklogUser updatedUser;
            if (TryUpdateUser(result, out updatedUser))
            {
                await context.PostAsync($"You updated user with id {updatedUser.Id}.");
            }
            else
            {
                await context.PostAsync("You did not pass any fields to be updated.");
            }

            context.Wait(MessageReceived);
        }

        [LuisIntent(INTENT_USER_REMOVE)]
        public async Task RemoveBacklogUser(IDialogContext context, LuisResult result)
        {
            if (TryRemoveUser(result))
            {
                await context.PostAsync($"You removed user with id.");
            }
            else
            {
                await context.PostAsync("Could not find user with given id.");
            }

            context.Wait(MessageReceived);
        }

        [LuisIntent("None")]
        public async Task NothingUnderstood(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("Sorry my supreme commander, I did not understand your command.");
            context.Wait(MessageReceived);
        }
    }
}