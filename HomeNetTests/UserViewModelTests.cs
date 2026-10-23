using HomeNetCore.Enums.Navigation;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Models.Entity;
using HomeNetPresentation.Services;
using HomeNetPresentation.ViewModels;
using Moq;
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
            // 1. ARRANGE (Подготовка окружения)
            var eventBusMock = new Mock<IEventBus>();
            var loggerMock = new Mock<ILogger>();

            // Коллекция для хранения ВСЕХ подписчиков на событие успешного входа
            var signedInCallbacks = new List<Action<IUserVm.UserSignedIn>>();
            Action<IUserVm.ClientTabChanged> userVmTabChangedCallback = null;

            // Универсальный перехватчик подписок на UserSignedIn. 
            // Каждый раз, когда кто-то (Навигатор или ЮзерВМ) вызывает Subscribe, мы добавляем его колбэк в список.
            eventBusMock
                .Setup(bus => bus.Subscribe(It.IsAny<Action<IUserVm.UserSignedIn>>()))
                .Callback<Action<IUserVm.UserSignedIn>>(action => signedInCallbacks.Add(action));

            // Перехватываем подписку UserViewModel на изменение вкладок
            eventBusMock
                .Setup(bus => bus.Subscribe(It.IsAny<Action<IUserVm.ClientTabChanged>>()))
                .Callback<Action<IUserVm.ClientTabChanged>>(action => userVmTabChangedCallback = action);

            // Имитируем работу реальной шины: когда Навигатор публикует ClientTabChanged, 
            // мок сразу же перенаправляет это событие во Вью-Модель
            eventBusMock
                .Setup(bus => bus.Publish(It.IsAny<object>(), It.IsAny<object>()))
                .Callback<object, object>((sender, msg) =>
                {
                    if (msg is IUserVm.ClientTabChanged tabChangedMsg)
                    {
                        userVmTabChangedCallback?.Invoke(tabChangedMsg);
                    }
                });

            // Инициализируем компоненты (они внутри себя вызовут Subscribe, и наши списки заполнятся)
            var navigator = new UserNavigationManager(eventBusMock.Object, loggerMock.Object);
            var userViewModel = new UserViewModel(loggerMock.Object, eventBusMock.Object, navigator);

            // Создаем реальные объекты данных для отправки
            var realUser = new UserEntity();
            var realMessage = new IUserVm.UserSignedIn(realUser);

            // 2. ACT (Действие)
            // Имитируем падение события UserSignedIn в шину — оповещаем ВСЕХ зарегистрированных подписчиков
            foreach (var callback in signedInCallbacks)
            {
                callback.Invoke(realMessage);
            }

            // 3. ASSERT (Проверка результата)
            // Теперь Навигатор гарантированно обработал ивент, переключил стейт на Messenger и пнул Вью-Модель!
            Assert.Equal(ClientSubTab.Messenger, userViewModel.ActiveClientTab);
        }








    }
}
