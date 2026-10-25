using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Repositories;
using HomeNetCore.Interfaces.Services;
using HomeNetOrm.Repositories.Local;
using HomeNetServices.Identity;
using HomeNetServices.Services.Identity;

namespace HomeNet.DI.Modules
{
    public class BusinessServicesModule
    {
        public IUserService UserService { get; }
        public IRegistrationService RegistrationService { get; }
        public IAuthenticateService AuthenticateService { get; }
        public IDeleteService DeleteService { get; }
        public IMessageService MessageService { get; }
        public IDeviceSettingsService DeviceSettingsService { get; } // Наш красавец на месте!

        public BusinessServicesModule(
            IUserRepository userRepo,
            IMessageRepository msgRepo,
            ClientDeviceSettingsRepository deviceSettingsRepo,
            ILogger logger,
            IEventBus eventBus)
        {
            UserService = new UserService(userRepo, logger, eventBus);
            RegistrationService = new RegistrationService(UserService);
            AuthenticateService = new AuthenticateService(UserService);
            DeleteService = new DeleteService(UserService, logger);
            MessageService = new MessageService(msgRepo, eventBus);

            // Ленивая инициализация железки прямо внутри конструктора сервиса
            DeviceSettingsService = new DeviceSettingsService(deviceSettingsRepo, logger);
        }
    }
}
