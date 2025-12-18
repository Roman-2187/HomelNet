using System.ComponentModel.DataAnnotations.Schema;
 using CommunityToolkit.Mvvm.ComponentModel;
namespace HomeNetCore.Models
{

   

    public partial class UserEntity : ObservableObject
    {
        [ObservableProperty]      
        private int _id;

        [ObservableProperty]        
        private string? _firstName = string.Empty;

        [ObservableProperty]       
        private string? _lastName = string.Empty;

        [ObservableProperty]      
        private string? _phoneNumber = string.Empty;

        [ObservableProperty]       
        private string? _email = string.Empty;

        [ObservableProperty]       
        private string? _password = string.Empty;

        [ObservableProperty]
       
        private DateTime _createdAt = DateTime.UtcNow;

        public string FullName => $"{FirstName} {LastName}";

        public string DisplayInfo => $"ID: {Id} - {Email}";

        // Ручно уведомляем об изменении FullName при обновлении зависимостей
        partial void OnFirstNameChanged(string? value) => OnPropertyChanged(nameof(FullName));
        partial void OnLastNameChanged(string? value) => OnPropertyChanged(nameof(FullName));

        // Аналогично для DisplayInfo
        partial void OnIdChanged(int value) => OnPropertyChanged(nameof(DisplayInfo));
        partial void OnEmailChanged(string? value) => OnPropertyChanged(nameof(DisplayInfo));
    }



}
