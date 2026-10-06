using System;
using System.ComponentModel.DataAnnotations.Schema;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HomeNetCore.Models
{
    public partial class UserEntity : ObservableObject
    {
        // 🎯 МЕНЯЕМ НА GUID: Тулкит сам создаст публичный 'Guid Id'
        [ObservableProperty]
        private Guid _id;

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

        // 🔥 НАШ ХИТРЫЙ НЕВИДИМЫЙ ХВОСТ:
        [NotMapped]
        [ObservableProperty]
        private string _confirmPassword = string.Empty;

        [ObservableProperty]
        private DateTime _createdAt = DateTime.UtcNow;

        // ⏱ НОВАЯ ВРЕМЕННАЯ МЕТКА СИНХРОНИЗАЦИИ (DateTimeOffset)
        // Тулкит сгенерирует публичное свойство 'DateTimeOffset UpdatedAt'
        [ObservableProperty]
        private DateTimeOffset _updatedAt = DateTimeOffset.UtcNow;

        public string FullName => $"{FirstName} {LastName}";
        public string DisplayInfo => $"ID: {Id} - {Email}";

        partial void OnFirstNameChanged(string? value) => OnPropertyChanged(nameof(FullName));
        partial void OnLastNameChanged(string? value) => OnPropertyChanged(nameof(FullName));

        // 🎯 ФИКС: Тулкит автоматически поменяет сигнатуру partial метода под Guid value!
        partial void OnIdChanged(Guid value) => OnPropertyChanged(nameof(DisplayInfo));
        partial void OnEmailChanged(string? value) => OnPropertyChanged(nameof(DisplayInfo));
    }
}


