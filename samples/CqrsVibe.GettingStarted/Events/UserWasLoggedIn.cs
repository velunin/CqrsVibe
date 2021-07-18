using System;

namespace GettingStartedApp.Events
{
    public class UserWasLoggedIn
    {
        public UserWasLoggedIn(string name, DateTime loggedInAt)
        {
            Name = name;
            LoggedInAt = loggedInAt;
        }

        public string Name { get; }
        
        public DateTime LoggedInAt { get; }
    }
}