using Backlog.Client;
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

        public bool TryParseUser(LuisResult result, out User user)
        {
            user = new User();

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

            user.UserId = id?.Entity;
            user.Name = name?.Entity;
            user.MailAddress = email?.Entity;
            user.RoleType = BacklogRole.FindMatchingRoleByName(role?.Entity).Id;
            user.Password = password?.Entity;

            return hasParsed;
        }

        public bool TryUpdateUser(LuisResult result, out User user)
        {
            user = new User();

            EntityRecommendation id, name, email, role, password;

            if (result.TryFindEntity(ENTITY_USER_ID, out id))
                user.UserId = id.Entity;

            if (result.TryFindEntity(ENTITY_USER_NAME, out name))
                user.Name = name.Entity;

            if (result.TryFindEntity(ENTITY_USER_EMAIL, out email))
                user.MailAddress = email.Entity;

            if (result.TryFindEntity(ENTITY_USER_ROLE, out role))
                user.RoleType = BacklogRole.FindMatchingRoleByName(role.Entity).Id;

            if (result.TryFindEntity(ENTITY_USER_PASSWORD, out password))
                user.Password = password.Entity;

            //check if any updates were passed and if we have a user id
            return user.ToUpdateFormValues.Count() > 0 && !String.IsNullOrEmpty(user.UserId);
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
                var usersResult = Api.Users.getUsers();
                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine("# List of all users");
                foreach (var user in usersResult.Value.Value)
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
            User newUser;
            bool extraInputRequired = false;
            try
            {
                if (TryParseUser(result, out newUser))
                {
                    var createdUser = CreateUser(newUser);
                    await context.PostAsync($"You created a new user with id {createdUser.UserId} (internal id: {createdUser.Id}).");
                }
                else
                {
                    extraInputRequired = true;
                    var creationOrder = new UserCreationOrder(newUser);
                    var formDialog = new FormDialog<UserCreationOrder>(creationOrder, UserCreationOrder.CreateAddForm, FormOptions.PromptInStart);
                    context.Call(formDialog, UserCreationOrderCompleted);
                }
            }
            catch (RoleNotFoundException)
            {
                await context.PostAsync("Could not find the specified role.");
            }
            catch (Exception e)
            {
                await context.PostAsync(e.Message);
                //await context.PostAsync("I messed up! Please forgive me benevolent human overlord...");
            }
            finally
            {
                if (!extraInputRequired)
                    context.Wait(MessageReceived);
            }
        }

        private async Task UserCreationOrderCompleted(IDialogContext context, IAwaitable<UserCreationOrder> result)
        {
            try
            {
                var order = await result;
                var newUser = new User(order.Id, order.Name, order.Password, order.Role.Value, order.MailAddress);
                var createdUser = CreateUser(newUser);
                await context.PostAsync($"You created a new user with id {createdUser.UserId} (internal id: {createdUser.Id}).");
            }
            catch (OperationCanceledException)
            {
                await context.PostAsync("You canceled the form!");
            }
            catch (Exception e)
            {
                await context.PostAsync(e.Message);
            }

            context.Wait(MessageReceived);
        }

        private User CreateUser(User user)
        {
            var userOption = Api.Users.addUser(user);

            if (userOption.StatusCode == 404)
                throw new Exception($"Couldnt find user with id: {user.UserId}");
            //need to handle this better
            if (userOption.Value.IsNone())
                throw new Exception("Placeholder exception for failing to create user.");

            return userOption.Value.Value;
        }

        [LuisIntent(INTENT_USER_UPDATE)]
        public async Task UpdateBacklogUser(IDialogContext context, LuisResult result)
        {
            User updateUser;
            bool extraInputRequired = false;
            try
            {
                if (TryUpdateUser(result, out updateUser))
                {
                    var updatedUser = UpdateUser(updateUser);
                    await context.PostAsync($"You updated user with id {updatedUser.Id}.");
                }
                else
                {
                    extraInputRequired = true;
                    var updateOrder = new UserUpdateOrder(updateUser);
                    var formDialog = new FormDialog<UserUpdateOrder>(updateOrder, UserUpdateOrder.CreateUpdateForm, FormOptions.PromptInStart);
                    context.Call(formDialog, UserUpdateOrderCompleted);
                }
            }
            catch (Exception e)
            {
                await context.PostAsync(e.Message);
                // await context.PostAsync("I messed up! Please forgive me benevolent human overlord...");
            }
            finally
            {
                if (!extraInputRequired)
                    context.Wait(MessageReceived);
            }
        }

        private async Task UserUpdateOrderCompleted(IDialogContext context, IAwaitable<UserUpdateOrder> result)
        {
            try
            {
                var order = await result;
                var newUser = new User(order.Id, order.Name, order.Password, order.Role.GetValueOrDefault(-1), order.MailAddress);
                var updatedUser = UpdateUser(newUser);
                await context.PostAsync($"You updated the user with id {updatedUser.UserId} (internal id: {updatedUser.Id}).");
            }
            catch (OperationCanceledException)
            {
                await context.PostAsync("You canceled the form!");
            }
            catch (Exception e)
            {
                await context.PostAsync(e.Message);
            }

            context.Wait(MessageReceived);
        }

        private User UpdateUser(User user)
        {
            var userOption = Api.Users.updateUser(user);

            if (userOption.StatusCode == 404)
                throw new Exception($"Couldnt find user with id: {user.UserId}");
            //need to handle this better
            if (userOption.Value.IsNone())
                throw new Exception("Placeholder exception for failing to create user.");

            return userOption.Value.Value;
        }

        [LuisIntent(INTENT_USER_REMOVE)]
        public async Task RemoveBacklogUser(IDialogContext context, LuisResult result)
        {
            EntityRecommendation userId;
            bool extraInputRequired = false;
            try
            {
                if (result.TryFindEntity(ENTITY_USER_ID, out userId))
                {
                    var didDelete = DeleteUser(userId.Entity);
                    if (didDelete)
                        await context.PostAsync($"You successfully deleted the user with id : {userId.Entity}");
                    else
                        await context.PostAsync($"Failed to delete user.");
                }
                else
                {
                    extraInputRequired = true;
                    var deleteOrder = new UserDeleteOrder();
                    var formDialog = new FormDialog<UserDeleteOrder>(deleteOrder, UserDeleteOrder.CreateDeleteForm, FormOptions.PromptInStart);
                    context.Call(formDialog, UserDeleteOrderCompleted);
                }
            }
            catch (Exception e)
            {
                await context.PostAsync(e.Message);
                //await context.PostAsync("I messed up! Please forgive me benevolent human overlord...");
            }
            finally
            {
                if (!extraInputRequired)
                    context.Wait(MessageReceived);
            }
        }

        private async Task UserDeleteOrderCompleted(IDialogContext context, IAwaitable<UserDeleteOrder> result)
        {
            try
            {
                var order = await result;

                var didDelete = DeleteUser(order.Id);
                if (didDelete)
                    await context.PostAsync($"You successfully deleted the user with id : {order.Id}");
                else
                    await context.PostAsync($"Failed to delete user.");
            }
            catch (OperationCanceledException)
            {
                await context.PostAsync("You canceled the form!");
                return;
            }
            catch (Exception e)
            {
                await context.PostAsync(e.Message);
            }
            context.Wait(MessageReceived);
        }

        private bool DeleteUser(string userId)
        {
            var users = Api.Users.getUsers();
            if (users.Value.IsNone())
                throw new Exception("Placeholder exception for failing to update user.");

            var deleteUser = users.Value.Value.SingleOrDefault(u => u.UserId.Equals(userId));

            if (deleteUser == null)
                throw new Exception("Placeholder exception for failing to update user.");

            var isDeleted = Api.Users.deleteUser(deleteUser.Id);

            if (isDeleted.StatusCode == 404)
                throw new Exception($"Couldnt find user with id: {userId}");

            return true;
        }

        [LuisIntent("None")]
        public async Task NothingUnderstood(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("Sorry my supreme commander, I did not understand your command.");
            context.Wait(MessageReceived);
        }
    }
}