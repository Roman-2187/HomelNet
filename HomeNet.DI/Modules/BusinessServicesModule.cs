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
      
        public IUserRepository UserRepository { get; }
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
           UserRepository = userRepo;
            RegistrationService = new RegistrationService(userRepo);
            AuthenticateService = new AuthenticateService(userRepo);
            DeleteService = new DeleteService(userRepo, logger);
            MessageService = new MessageService(msgRepo, eventBus);

            // Ленивая инициализация железки прямо внутри конструктора сервиса
            DeviceSettingsService = new DeviceSettingsService(deviceSettingsRepo, logger);
        }
    }
}
