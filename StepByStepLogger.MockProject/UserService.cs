namespace StepByStepLogger.MockProject
{
    public class UserService
    {
        public string GetUser(int id)
        {
            Console.WriteLine("Inside GetUser");
            User user = BuildUser(id);
            SaveUser(user);
            return user.Name;
        }

        private User BuildUser(int id)
        {
            Console.WriteLine("Inside BuildUser");
            string name = GenerateUserName(id);
            int age = CalculateUserAge(id);
            return new User { Id = id, Name = name, Age = age };
        }

        private string GenerateUserName(int id)
        {
            Console.WriteLine("Inside GenerateUserName");
            return $"User-{id}";
        }

        private int CalculateUserAge(int id)
        {
            Console.WriteLine("Inside CalculateUserAge");
            return id + 20;
        }

        private void SaveUser(User user)
        {
            Console.WriteLine("Inside SaveUser");
            LogUser(user);
            DoSomething();
        }

        public void DoSomething()
        {
            Console.WriteLine("Inside DoSomething");
            DoMoreWork();
        }

        private void DoMoreWork()
        {
            Console.WriteLine("Inside DoMoreWork");
            int result = HelperMethod("TestParameter");
            Console.WriteLine($"Helper result: {result}");
        }

        private int HelperMethod(string input)
        {
            Console.WriteLine("Inside HelperMethod");
            return input.Length;
        }

        private void LogUser(User user)
        {
            Console.WriteLine($"Logging user: {user.Name}, Age: {user.Age}");
        }
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }
}
