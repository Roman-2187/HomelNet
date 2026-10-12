using HomeNetCore.Enums.Navigation;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Models;
using HomeNetPresentation.Services;
using HomeNetPresentation.ViewModels;
using Moq;
using NUnit.Framework;
using Assert = Xunit.Assert;

namespace HomeNetTests
{
    public class UserNavigationFlowTests
    {
        [Fact]
        public void When_OpenRegistrationRequested_Then_UserViewModel_ShouldReceive_RegistrationTab()
        {
            // 1. ARRANGE (Подготовка окружения)
            var eventBusMock = new Mock<IEventBus>();
            var loggerMock = new Mock<ILogger>();

            // Списки для хранения колбэков, которые компоненты зарегистрируют в шине
            Action<IUserVm.OpenRegistration> navigatorOpenRegCallback = null;
            Action<IUserVm.ClientTabChanged> userVmTabChangedCallback = null;

            // Перехватываем подписку Навигатора на запрос регистрации
            eventBusMock
                .Setup(bus => bus.Subscribe(It.IsAny<Action<IUserVm.OpenRegistration>>()))
                .Callback<Action<IUserVm.OpenRegistration>>(action => navigatorOpenRegCallback = action);

            // Перехватываем подписку UserViewModel на изменение табов
            eventBusMock
                .Setup(bus => bus.Subscribe(It.IsAny<Action<IUserVm.ClientTabChanged>>()))
                .Callback<Action<IUserVm.ClientTabChanged>>(action => userVmTabChangedCallback = action);

            // Перехватываем момент, когда Навигатор делает EventBus.Publish(...)
            // и СРАЗУ перенаправляем это событие в UserViewModel, имитируя работу реального автобуса!
            eventBusMock
                .Setup(bus => bus.Publish(It.IsAny<object>(), It.IsAny<IUserVm.ClientTabChanged>()))
                .Callback<object, IUserVm.ClientTabChanged>((sender, msg) =>
                {
                    // Если Навигатор опубликовал извещение, мы мгновенно передаем его в UserVM
                    userVmTabChangedCallback?.Invoke(msg);
                });

            // Инициализируем систему (они сами подписываются на мок шины)
            var navigator = new UserNavigationManager(eventBusMock.Object, loggerMock.Object);
            var userViewModel = new UserViewModel(loggerMock.Object, eventBusMock.Object, navigator);

            // Проверяем, что все подписки успешно перехвачены моком
            Assert.NotNull(navigatorOpenRegCallback);
            Assert.NotNull(userVmTabChangedCallback);

            // Создаем заглушку для сообщения "Хочу регистрацию" (например, от нажатия кнопки на панели)
            var openRegMsgMock = new Mock<IUserVm.OpenRegistration>();

            // 2. ACT (Действие — имитируем клик/команду на регистрацию)
            // Вызываем колбэк навигатора, как будто в шину прилетела команда IUserVm.OpenRegistration
            navigatorOpenRegCallback.Invoke(openRegMsgMock.Object);

            // 3. ASSERT (Проверка результата)
            // Проверяем, долетел ли энум Registration до свойства вью-модели пользователя
            Assert.Equal(ClientSubTab.Registration, userViewModel.ActiveClientTab);
        }


        [Fact]
        public void When_OpenAuthRequested_Then_UserViewModel_ShouldReceive_AuthenticationTab()
        {
            // 1. ARRANGE (Подготовка окружения)
            var eventBusMock = new Mock<IEventBus>();
            var loggerMock = new Mock<ILogger>();

            Action<IUserVm.OpenAuth> navigatorOpenAuthCallback = null;
            Action<IUserVm.ClientTabChanged> userVmTabChangedCallback = null;

            // Перехватываем подписку Навигатора на запрос Входа
            eventBusMock
                .Setup(bus => bus.Subscribe(It.IsAny<Action<IUserVm.OpenAuth>>()))
                .Callback<Action<IUserVm.OpenAuth>>(action => navigatorOpenAuthCallback = action);

            // Перехватываем подписку UserViewModel на изменение табов
            eventBusMock
                .Setup(bus => bus.Subscribe(It.IsAny<Action<IUserVm.ClientTabChanged>>()))
                .Callback<Action<IUserVm.ClientTabChanged>>(action => userVmTabChangedCallback = action);

            // Перенаправляем событие изменения таба из Навигатора прямо во Вью-модель
            eventBusMock
                .Setup(bus => bus.Publish(It.IsAny<object>(), It.IsAny<IUserVm.ClientTabChanged>()))
                .Callback<object, IUserVm.ClientTabChanged>((sender, msg) =>
                {
                    userVmTabChangedCallback?.Invoke(msg);
                });

            // Инициализируем компоненты
            var navigator = new UserNavigationManager(eventBusMock.Object, loggerMock.Object);
            var userViewModel = new UserViewModel(loggerMock.Object, eventBusMock.Object, navigator);

            // Проверяем, что подписки успешно перехвачены
            Assert.NotNull(navigatorOpenAuthCallback);
            Assert.NotNull(userVmTabChangedCallback);

            // Создаем заглушку для сообщения "Хочу войти" (имитация клика по кнопке "Вход")
            var openAuthMsgMock = new Mock<IUserVm.OpenAuth>();

            // 2. ACT (Действие)
            // Имитируем, что в шину прилетела команда IUserVm.OpenAuth
            navigatorOpenAuthCallback.Invoke(openAuthMsgMock.Object);

            // 3. ASSERT (Проверка)
            // Проверяем, что во вью-модели теперь стейт Authentication
            Assert.Equal(ClientSubTab.Authentication, userViewModel.ActiveClientTab);
        }

        [Fact]
        public void When_UserSignedIn_Then_UserViewModel_ShouldReceive_MessengerTab()
        {
            // 1. ARRANGE
            var eventBusMock = new Mock<IEventBus>();
            var loggerMock = new Mock<ILogger>();

            Action<IUserVm.UserSignedIn> navigatorSignedInCallback = null;
            Action<IUserVm.ClientTabChanged> userVmTabChangedCallback = null;

            // Перехватываем подписку навигатора
            eventBusMock
                .Setup(bus => bus.Subscribe(It.IsAny<Action<IUserVm.UserSignedIn>>()))
                .Callback<Action<IUserVm.UserSignedIn>>(action => navigatorSignedInCallback = action);

            // Перехватываем подписку UserViewModel
            eventBusMock
                .Setup(bus => bus.Subscribe(It.IsAny<Action<IUserVm.ClientTabChanged>>()))
                .Callback<Action<IUserVm.ClientTabChanged>>(action => userVmTabChangedCallback = action);

            // 🔥 ВСЕЯДНЫЙ ПЕРЕХВАТЧИК: Принимает object и object, чтобы переварить MacroZoneChanged и ClientTabChanged подряд
            eventBusMock
                .Setup(bus => bus.Publish(It.IsAny<object>(), It.IsAny<object>()))
                .Callback<object, object>((sender, msg) =>
                {
                    // Если в шину летит ClientTabChanged — бережно передаем его во вью-модель
                    if (msg is IUserVm.ClientTabChanged tabChangedMsg)
                    {
                        userVmTabChangedCallback?.Invoke(tabChangedMsg);
                    }
                });

            // Инициализируем компоненты
            var navigator = new UserNavigationManager(eventBusMock.Object, loggerMock.Object);
            var userViewModel = new UserViewModel(loggerMock.Object, eventBusMock.Object, navigator);

            // Создаем реального юзера
            var realUser = new UserEntity();

            // 🔥 ЧИСТЫЙ С# ОБЪЕКТ (БЕЗ Moq) — так как это класс/рекорд. 
            // Навигатор без проблем прочитает .User и не споткнется на null!
            var realMessage = new IUserVm.UserSignedIn(realUser);

            // 2. ACT
            // Отправляем реальный рекорд в навигатор
            navigatorSignedInCallback.Invoke(realMessage);

            // 3. ASSERT
            // Теперь цепочка отработает до конца, и вью-модель получит Messenger
            Assert.Equal(ClientSubTab.Messenger, userViewModel.ActiveClientTab);
        }







    }
}
