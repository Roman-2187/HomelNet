using System;
using System.ComponentModel;

namespace HomeNetCore.Interfaces.ViewModels
{
    /// <summary>
    /// Единый общий знаменатель для всех экранов приложения SiberNet.
    /// Позволяет главному окну держать их в одном свойстве без дженерик-ада.
    /// </summary>
    public interface IFormViewModel : INotifyPropertyChanged, IDisposable
    {
        string StatusMessage { get; set; }
        string SubmitButtonText { get; set; }
    }
}
