using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Services;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Repositories.Local;
using HomeNetServices.Identity;
using HomeNetServices.Services.Identity;

namespace HomeNet.DI.Modules
{
    public class BusinessServicesModule
    {
      
        public IUserRepo UserRepository { get; }
        public IRegistrationService RegistrationService { get; }
        public IAuthenticateService AuthenticateService { get; }
        public IDeleteService DeleteService { get; }
        public IMessageService MessageService { get; }
        public IDeviceSettingsService DeviceSettingsService { get; } // Наш красавец на месте!

        public BusinessServicesModule(
            IUserRepo userRepo,
            IMessageRepo msgRepo,
            ClientDeviceSettingsRepository deviceSettingsRepo,
            ICastomLogger logger,
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
