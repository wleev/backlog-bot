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
    public class UserCreationOrder
    {
        [Prompt("Please enter an unique user id")]
        public string Id;

        [Describe("full name of the user")]
        public string Name;

        [Describe("the e-mail address of the user")]
        public string MailAddress;

        [Describe("desired password for the new user")]
        public string Password;

        [Numeric(1, 6)]
        [Describe("the id of the role you'd like to assign this user")]
        public int? Role;

        public UserCreationOrder()
        {
        }

        public UserCreationOrder(User unfinishedUser)
        {
            Id = unfinishedUser.UserId;
            Name = unfinishedUser.Name;
            MailAddress = unfinishedUser.MailAddress;
            Password = unfinishedUser.Password;
            Role = unfinishedUser.RoleType;
        }

        public static IForm<UserCreationOrder> CreateAddForm()
        {
            var formBuilder = new FormBuilder<UserCreationOrder>()
                .Message("There seem to have been some missing fields when you tried to add a user. Let's try to resolve those now.");

            ActiveDelegate<UserCreationOrder> doesntHaveId = (user) => String.IsNullOrEmpty(user.Id);
            ActiveDelegate<UserCreationOrder> doesntHaveName = (user) => String.IsNullOrEmpty(user.Name);
            ActiveDelegate<UserCreationOrder> doesntHaveMail = (user) => String.IsNullOrEmpty(user.MailAddress);
            ActiveDelegate<UserCreationOrder> doesntHavePassword = (user) => String.IsNullOrEmpty(user.Password);
            ActiveDelegate<UserCreationOrder> doesntHaveRole = (user) => !user.Role.HasValue || user.Role < 0;

            ValidateAsyncDelegate<UserCreationOrder> passwordHasMinLength = async (state, response) =>
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
            formBuilder.Confirm("Do you really really want to create this user?");
            formBuilder.Message("I will now try to add this user...");

            //formBuilder.OnCompletionAsync(callback);

            return formBuilder.Build();
        }
    }
}