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
    public class UserDeleteOrder
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

        public UserDeleteOrder()
        {
        }

        public UserDeleteOrder(User unfinishedUser)
        {
            Id = unfinishedUser.UserId;
        }

        public static IForm<UserDeleteOrder> CreateDeleteForm()
        {
            var formBuilder = new FormBuilder<UserDeleteOrder>()
                .Message("There seem to have been some missing fields when you tried to delete a user. Let's try to resolve those now.");

            ActiveDelegate<UserDeleteOrder> doesntHaveId = (user) => String.IsNullOrEmpty(user.Id);

            formBuilder.Field(nameof(Id), doesntHaveId);
            formBuilder.Confirm("Do you really really want to delete this user?");
            formBuilder.Message("I will now try to delete this user...");

            //formBuilder.OnCompletionAsync(callback);

            return formBuilder.Build();
        }
    }
}