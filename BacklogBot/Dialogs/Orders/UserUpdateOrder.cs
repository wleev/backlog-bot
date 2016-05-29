using BacklogBot.Models;
using Microsoft.Bot.Builder.FormFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using static Backlog.Client.Models;

namespace BacklogBot.Dialogs.Orders
{
    [Serializable]
    public class UserUpdateOrder
    {
        [Prompt("Please enter an unique user id")]
        public string Id;

        [Optional]
        [Describe("full name of the user")]
        public string Name;

        [Optional]
        [Describe("the e-mail address of the user")]
        public string MailAddress;

        [Optional]
        [Describe("desired password for the new user")]
        public string Password;

        [Numeric(1, 6)]
        [Optional]
        [Describe("the id of the role you'd like to assign this user")]
        public int? Role;

        public UserUpdateOrder()
        {
        }

        public UserUpdateOrder(User unfinishedUser)
        {
            Id = unfinishedUser.UserId;
            Name = unfinishedUser.Name;
            MailAddress = unfinishedUser.MailAddress;
            Password = unfinishedUser.Password;
            Role = unfinishedUser.RoleType;
        }

        public static IForm<UserUpdateOrder> CreateUpdateForm()
        {
            var formBuilder = new FormBuilder<UserUpdateOrder>()
                .Message("There seem to have been some missing fields when you tried to update a user. Let's try to resolve those now.");

            ActiveDelegate<UserUpdateOrder> doesntHaveId = (user) => String.IsNullOrEmpty(user.Id);
            ActiveDelegate<UserUpdateOrder> doesntHaveName = (user) => String.IsNullOrEmpty(user.Name);
            ActiveDelegate<UserUpdateOrder> doesntHaveMail = (user) => String.IsNullOrEmpty(user.MailAddress);
            ActiveDelegate<UserUpdateOrder> doesntHavePassword = (user) => String.IsNullOrEmpty(user.Password);
            ActiveDelegate<UserUpdateOrder> doesntHaveRole = (user) => !user.Role.HasValue || user.Role < 0;

            ValidateAsyncDelegate<UserUpdateOrder> passwordHasMinLength = async (state, response) =>
            {
                var result = new ValidateResult { IsValid = true };
                var password = response as string;
                result.Value = password;
                if (password.Length < 7)
                {
                    result.Feedback = "Password must be longer than 7 characters";
                    result.IsValid = false;
                }

                return result;
            };

            formBuilder.Field(nameof(Id), doesntHaveId);
            formBuilder.Field(nameof(Name), doesntHaveName);
            formBuilder.Field(nameof(MailAddress), doesntHaveMail);
            formBuilder.Field(nameof(Password), doesntHavePassword, passwordHasMinLength);
            formBuilder.Field(nameof(Role), doesntHaveRole);
            formBuilder.Confirm(async (state) =>
            {
                string updateSummary = "( ";
                bool hasUpdatedField = false;
                if (String.IsNullOrEmpty(state.Name))
                {
                    hasUpdatedField = true;
                    updateSummary += $"{{ Name: {state.Name} }}";
                }
                if (String.IsNullOrEmpty(state.MailAddress))
                {
                    hasUpdatedField = true;
                    updateSummary += $"{{ Name: {state.MailAddress}}}";
                }
                if (String.IsNullOrEmpty(state.Password))
                {
                    hasUpdatedField = true;
                    updateSummary += $"{{ Name: {state.Password}}}";
                }
                if (state.Role > 0)
                {
                    hasUpdatedField = true;
                    updateSummary += $"{{ Name: {state.Role}}}";
                }

                if (hasUpdatedField)
                    return new PromptAttribute($"Are you sure you want to update the user {updateSummary}");
                else
                    return new PromptAttribute($"Are you sure you want to update the user {updateSummary}");
            });
            formBuilder.Message("I will now try to update this user...");

            return formBuilder.Build();
        }
    }
}