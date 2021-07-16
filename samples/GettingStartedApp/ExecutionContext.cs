using System;

namespace GettingStartedApp
{
    public class ExecutionContext
    {
        public static User CurrentUser { get; set; }
    }

    public class User
    {
        public string Name { get; set; }

        public DateTime LoggedInAt { get; set; }
    }
}