using System.Collections.Generic;

namespace HomeNetCore.Models
{
    public class DbSeedData
    {
        // Превратили свойство в метод с параметром по умолчанию (10 штук) 🛸🦾
        public static List<UserEntity> GetGeneratedUsers(int count = 10)
        {
            var firstNames = new[]
            {
                "Александр", "Дмитрий", "Сергей", "Михаил", "Андрей",
                "Виктор", "Алексей", "Игорь", "Евгений", "Артём",
                "Даниил", "Максим", "Кирилл", "Матвей", "Тимофей",
                "Лев", "Марк", "Иван", "Денис", "Егор", "Владимир", "Никита"
            };

            var lastNames = new[]
            {
                "Иванов", "Сидоров", "Морозов", "Соколов", "Фёдоров",
                "Павлов", "Николаев", "Андреев", "Матвеев", "Белов",
                "Орлов", "Соловьёв", "Романов", "Лазарев", "Сергеев",
                "Васильев", "Алексеев", "Иванов"
            };

            var generatedUsers = new List<UserEntity>();

            // Цикл теперь крутится ровно столько раз, сколько ты попросишь
            for (int i = 0; i < count; i++)
            {
                var fName = firstNames[i % firstNames.Length];
                var lName = lastNames[i % lastNames.Length];

                generatedUsers.Add(new UserEntity
                {
                    FirstName = fName,
                    LastName = lName,
                    PhoneNumber = $"+7 916 {100 + i}-{23 + i:D2}-45",
                    // Используем латинский транслит или индекс, чтобы email не ломал СУБД из-за кириллицы
                    Email = $"user_{i}@example.com",
                    Password = $"SecurePass{2025 + i}!"
                });
            }

            // Твой профиль добавляется в самый конец железно
            generatedUsers.Add(new UserEntity
            {
                FirstName = "Admin",
                LastName = "Roman",
                PhoneNumber = "+7 903 678-90-12",
                Email = "roman@example.net",
                Password = "ChangeMe123"
            });

            return generatedUsers;
        }
    }
}
