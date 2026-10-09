using System;
using System.ComponentModel.DataAnnotations.Schema;
using CommunityToolkit.Mvvm.ComponentModel;
using HomeNetCore.Interfaces.Repositories;

namespace HomeNetCore.Models
{
    public partial class UserEntity : ObservableObject, ISyncableEntity
    {
        // 🎯 СИНХРО-ФЛАГ: Теперь это правильное Observable-свойство.
        // Тулкит сгенерирует публичный 'int IsSynced { get; set; }'
        // По умолчанию 0 — новая запись локально не синхронизирована с сервером
        [ObservableProperty]
        private int _isSynced = 0;

        // Идентификатор на GUID-ах для распределенной архитектуры
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

        // Наш хитрый невидимый хвост для валидации при регистрации
        [NotMapped]
        [ObservableProperty]
        private string _confirmPassword = string.Empty;

        // ⏱ ТАТАЛЬНЫЙ DateTimeOffset: Теперь дата создания знает свой часовой пояс.
        // Тулкит сгенерирует публичное свойство 'DateTimeOffset CreatedAt'
        [ObservableProperty]
        private DateTimeOffset _createdAt = DateTimeOffset.UtcNow;

        // Высокоточная временная метка обновления (паттерн Last Write Wins)
        [ObservableProperty]
        private DateTimeOffset _updatedAt = DateTimeOffset.UtcNow;

        // Вычисляемое свойство для UI
        public string FullName => $"{FirstName} {LastName}".Trim();

        // Быстрое обновление FullName при изменении имени или фамилии
        partial void OnFirstNameChanged(string? value) => OnPropertyChanged(nameof(FullName));
        partial void OnLastNameChanged(string? value) => OnPropertyChanged(nameof(FullName));
    }
}
