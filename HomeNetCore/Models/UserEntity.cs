using System;
using System.ComponentModel.DataAnnotations.Schema; // 🔥 ОБЯЗАТЕЛЬНО ДЛЯ NOTMAPPED
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

        // 🔥 НАШ ХИТРЫЙ НЕВИДИМЫЙ ХВОСТ:
        [NotMapped] // База данных Postgres эту строчку полностью проигнорирует! 🔐
        [ObservableProperty]
        private string _confirmPassword = string.Empty; // Сгенерирует публичное свойство ConfirmPassword

        [ObservableProperty]
        private DateTime _createdAt = DateTime.UtcNow;

        public string FullName => $"{FirstName} {LastName}";
        public string DisplayInfo => $"ID: {Id} - {Email}";

        partial void OnFirstNameChanged(string? value) => OnPropertyChanged(nameof(FullName));
        partial void OnLastNameChanged(string? value) => OnPropertyChanged(nameof(FullName));
        partial void OnIdChanged(int value) => OnPropertyChanged(nameof(DisplayInfo));
        partial void OnEmailChanged(string? value) => OnPropertyChanged(nameof(DisplayInfo));
    }
}



