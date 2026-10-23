using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeNetCore.Models.Entity
{



    public partial class UserEntity : BaseEntity
    {
        // ==========================================
        // 🚀 СВЯЗУЮЩИЙ ХВОСТ ДЛЯ СЕТИ И СИНХРОНИЗАЦИИ
        // ==========================================

        // Текущее устройство пользователя. 
        // Клиент подставляет его из настроек при старте, а сервер по нему сразу 
        // понимает, с какого именно девайса прилетел запрос, без лишних поисков!
        [ObservableProperty] private Guid _deviceId;


      
       


        // ==========================================
        // ОСТАЛЬНЫЕ ПЕРЕПИСЫВАЕМЫЕ ПОЛЯ ПРОФИЛЯ
        // ==========================================
        [ObservableProperty] private string? _firstName = string.Empty;
        [ObservableProperty] private string? _lastName = string.Empty;
        [ObservableProperty] private string? _phoneNumber = string.Empty;
        [ObservableProperty] private string? _email = string.Empty;
        [ObservableProperty] private string? _password = string.Empty;

        [NotMapped][ObservableProperty] private string _confirmPassword = string.Empty;

        public string FullName => $"{FirstName} {LastName}".Trim();

        partial void OnFirstNameChanged(string? value) => OnPropertyChanged(nameof(FullName));
        partial void OnLastNameChanged(string? value) => OnPropertyChanged(nameof(FullName));
    }
}


