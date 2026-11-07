using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNetCore.Interfaces;
using HomeNet.Network.Network.ApiClient; // 🔥 Подключаем наши API-клиенты
using HomeNetCore.Interfaces.Services;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Repositories.Local;
using HomeNetServices.Identity;
using HomeNetServices.Services.Identity;

namespace HomeNet.DI.Modules
{
    public class BusinessServicesModule
    {
        // Оставляем свойство, если оно нужно для других модулей (например, для UI)
        public IUserRepo UserRepository { get; }

        public IRegistrationService RegistrationService { get; }
        public IAuthenticateService AuthenticateService { get; }
        public IDeleteService DeleteService { get; }
        public IMessageService MessageService { get; }
        public IDeviceSettingsService DeviceSettingsService { get; }

        public BusinessServicesModule(
            IUserRepo userRepo,          // Нужен для чтения (кэш)
            IMessageRepo msgRepo,        // Нужен для чтения (кэш)
            ClientDeviceSettingsRepository deviceSettingsRepo, // Локальные настройки
            IUserApiClient userApiClient,   // 🔥 Новый клиент
            IMessageApiClient messageApiClient, // 🔥 Новый клиент
            ICastomLogger logger,
            IEventBus eventBus)
        {
            UserRepository = userRepo;

            // 🔥 Сервисы теперь используют API-клиенты (для записи) и репозитории (для чтения)
            RegistrationService = new RegistrationService(userApiClient);
            AuthenticateService = new AuthenticateService(userApiClient);
            DeleteService = new DeleteService(userApiClient, logger);
            MessageService = new MessageService(messageApiClient, msgRepo); // Передали API и репо для чтения

            // Настройки устройства остаются локальными
            DeviceSettingsService = new DeviceSettingsService(deviceSettingsRepo, logger);
        }
    }
}