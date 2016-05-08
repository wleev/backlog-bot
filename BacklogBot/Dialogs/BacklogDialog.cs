using Backlog.Client;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;


namespace BacklogBot.Dialogs
{
    [LuisModel("26b510af-c1c2-46b8-adb1-010ac29d93a0", "848346513fe644c0850fb1bf6669cac7")]
    [Serializable]
    public class BacklogDialog : LuisDialog<object>
    {
        public const string ENTITY_USER_ID = "entity.backlog.user.id";
        public const string ENTITY_USER_NAME = "entity.backlog.user.name";
        public const string ENTITY_USER_PASSWORD = "entity.backlog.user.password";
        public const string ENTITY_USER_ROLE = "entity.backlog.user.role";
        public const string ENTITY_USER_EMAIL = "entity.backlog.user.email";

        public const string INTENT_DISK_USAGE = "intent.backlog.diskusage";
        public const string INTENT_USER_CREATE = "intent.backlog.user.create";
        public const string INTENT_USER_REMOVE = "intent.backlog.user.delete";
        public const string INTENT_USER_UPDATE = "intent.backlog.user.update";

        [Serializable]
        public sealed class BacklogUser : IEquatable<BacklogUser>
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
            public string Role { get; set; }
            public string Password { get; set; }

            public override string ToString()
            {
                return  $"{Name} ({Id})";
            }
            
            public override int GetHashCode()
            {
                return $"{Email}{Id}".GetHashCode();
            }

            public bool Equals(BacklogUser other)
            {
                return Id.Equals(other.Id)
                    && Email.Equals(other.Email)
                    && Name.Equals(other.Name)
                    && Role.Equals(other.Role);
            }
        }

        public bool TryCreateUser(LuisResult result, out BacklogUser user)
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

            if (hasParsed)
            {
                user.Id = id.Entity;
                user.Name = name.Entity;
                user.Email = email.Entity;
                user.Role = role.Entity;
                user.Password = password.Entity;
            }

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

            if(result.TryFindEntity(ENTITY_USER_EMAIL, out email))
                user.Email = email.Entity;

            if (result.TryFindEntity(ENTITY_USER_ROLE, out role))
                user.Role = role.Entity;

            if(result.TryFindEntity(ENTITY_USER_PASSWORD, out password))
                user.Password = password.Entity;

            return !currentUser.Equals(user);
        }

        [LuisIntent(INTENT_DISK_USAGE)]
        public async Task GetDiskUsage(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("You requested disk usage statistics.");
            context.Wait(MessageReceived);
        }

        [LuisIntent(INTENT_USER_CREATE)]
        public async Task CreateBacklogUser(IDialogContext context, LuisResult result)
        {
            BacklogUser newUser;
            if(TryCreateUser(result, out newUser))
            {
                await context.PostAsync($"You created a new user with id {newUser.Id}.");
            }
            else
            {
                await context.PostAsync("You did not pass all required fields for a new user to be created.");
            }

            context.Wait(MessageReceived);
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
    }
}