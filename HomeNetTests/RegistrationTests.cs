using HomeNetCore.Enums;
using HomeNetCore.Interfaces;
using HomeNetCore.Models;
using HomeNetCore.Models.Validation;
using HomeNetOrm.Interfaces;
using HomeNetPresentation.ViewModels;
using HomeNetServices.Routing;
using HomeNetServices.Services.Identity;
using NSubstitute; // Ставим, если генерируем фейковый сервис на лету 🦾

namespace HomeNet.Tests
{
    public class RegistrationTests
    {
        #region Заглушка логгера для тестов формы
        private class TestLogger : ILogger
        {
            public void SetOutput(Action<string, LogLevel> output) { }
            public void Log(LogLevel level, string message, string memberName = "", string filePath = "", int lineNumber = 0, params object[] args)
            {
                System.Diagnostics.Debug.WriteLine($"[{level}] {message}");
            }
        }
        #endregion

        #region 🛸 ЧАСТЬ 1: ТЕСТЫ ВАЛИДАТОРА СТРОК (ЯДРО)

        [Theory]
        [InlineData("user@example.com", true)]
        [InlineData("valid.email+test@domain.ru", true)]
        [InlineData("invalid-email.com", false)]
        [InlineData("@missinguser.com", false)]
        [InlineData("spaces in@email.com", false)]
        public void IsValidEmailFormat_ShouldValidateCorrectly(string email, bool expectedResult)
        {
            // Арранж
            var validator = new ValidationFormat();

            // Акт
            bool result = validator.IsValidEmailFormat(email);

            // Ассерт
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData("Password123", true)] // Буквы + цифры, >= 8 символов
        [InlineData("Short1", false)]     // Меньше 8 символов
        [InlineData("NoDigitsOnlyLetters", false)] // Нет цифр
        [InlineData("1234567890", false)] // Нет букв
        public void ValidatePasswordFormat_ShouldValidateCorrectly(string password, bool expectedResult)
        {
            // Арранж
            var validator = new ValidationFormat();

            // Акт
            bool result = validator.ValidatePasswordFormat(password);

            // Ассерт
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData("Роман", true)]
        [InlineData("Ufo", true)]
        [InlineData("Ян", false)] // Меньше 3 букв подряд
        [InlineData("123", false)] // Цифры вместо имени
        public void ValidateUserNameFormat_ShouldValidateCorrectly(string userName, bool expectedResult)
        {
            // Арранж
            var validator = new ValidationFormat();

            // Акт
            bool result = validator.ValidateUserNameFormat(userName);

            // Ассерт
            Assert.Equal(expectedResult, result);
        }
        #endregion

        #region 🎭 ЧАСТЬ 2: ТЕСТЫ ПОВЕДЕНИЯ REGISTRATION_VIEWMODEL (ПРЕЗЕНТАЦИЯ)

        [Fact] // 🎯 Тест реактивного закрытия формы при успешной регистрации
        public async Task RegistrationViewModel_ShouldCloseFormAndHideControl_AfterSuccessfulRegistration()
        {
            // 1. АРРАНЖ
            var testEventBus = new EventBus();
            var testLogger = new TestLogger();

            // Используем NSubstitute, чтобы на лету сгенерировать фейковый сервис регистрации
            var mockRegisterService = Substitute.For<IRegisterService>(/* передай сюда зависимости сервиса, если нужны, либо сделай пустой мок */);

            var testUser = new UserEntity { Id = 42, FirstName = "Роман", Email = "roman@sibernet.ru" };
            var fakeVerdict = new RegistrationVerdict(true, new List<ValidationResult>(), testUser);

            // Настраиваем фейковый сервис: при любом вызове метода RegisterUserAsync возвращать успешный вердикт
            mockRegisterService.RegisterUserAsync(Arg.Any<UserEntity>()).Returns(Task.FromResult(fakeVerdict));

            var viewModel = new RegistrationViewModel(mockRegisterService, testEventBus, testLogger);

            // Принудительно ставим форму в состояние "видима" перед тестом
            viewModel.IsControlVisible = true;

            // 2. АКТ
            // Имитируем заполнение данных пользователем
            viewModel.UserData.Email = "roman@sibernet.ru";

            // Дергаем команду регистрации
            await viewModel.RegisterCommand.ExecuteAsync(null);

            // 📢 ВАЖНО: В коде конструктора зашита задержка перед закрытием формы: Task.Delay(1000).
            // Чтобы асинхронный поток в подписке на UserAddedMessage успел отработать, ждем 1200мс.
            await Task.Delay(1200);

            // 3. АССЕРТ
            Assert.False(viewModel.IsControlVisible); // Форма должна автоматически схлопнуться в false! 🧼🦾
            Assert.Empty(viewModel.StatusMessage);    // Поля должны быть сброшены методом CloseForm()
            Assert.Equal("Введите email например 'User@example.com'", viewModel.ValidationResults[TypeField.EmailType].Message); // Вернулись дефолтные подсказки
        }

        [Fact] // 🎯 Тест отмены регистрации
        public void CancelCommand_ShouldClearDataAndHideControlImmediately()
        {
            // 1. АРРАНЖ
            var testEventBus = new EventBus();
            var testLogger = new TestLogger();
            var mockRegisterService = Substitute.For<IRegisterService>();

            var viewModel = new RegistrationViewModel(mockRegisterService, testEventBus, testLogger);
            viewModel.IsControlVisible = true;
            viewModel.StatusMessage = "Какая-то старая ошибка";

            // 2. АКТ
            viewModel.CancelCommand.Execute(null); // Жмем отмену

            // 3. АССЕРТ
            Assert.False(viewModel.IsControlVisible); // Схлопнулась мгновенно без задержек
            Assert.Empty(viewModel.StatusMessage);    // Ошибка стерлась
        }
        #endregion
    }
}
