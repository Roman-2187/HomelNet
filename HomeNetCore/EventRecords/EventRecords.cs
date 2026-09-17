using HomeNetCore.Interfaces;
using HomeNetCore.Models;
using System;
using System.Collections.Generic;

namespace HomeNetCore.Events
{
    #region Navigation and UI Visibility Events
    /// <summary> Сигнал от верхней кнопки-тумблера "🛠️ Админка". </summary>
    public record AdminMenuVisibilityChangedMessage(bool IsVisible);

    /// <summary> Универсальный переключатель видимости диалоговых окон. </summary>
    public record FormVisibilityChangedMessage(Type FormType, bool IsVisible);

    /// <summary> 🔥 СИГНАЛ НАВИГАЦИИ: Клик по другу в списке дашборда. </summary>
    public record FriendSelectedMessage(UserEntity Friend);

    /// <summary> Сигнал управления отображением центральной таблицы пользователей. </summary>
    public record UserTableVisibilityChangedMessage(bool IsVisible);
    #endregion

    #region Data and Database Events
    /// <summary> Запрос от интерфейса на принудительное обновление и синхронизацию таблиц. </summary>
    public record RequestStatusRefreshMessage();

    /// <summary> Сигнал о регистрации и добавлении нового пользователя в СУБД. </summary>
    public record UserAddedMessage(UserEntity User);

    /// <summary> Сигнал об успешном удалении пользователя из базы данных. </summary>
    public record UserDeletedMessage(int UserId);

    /// <summary> Сигнал о полной перезагрузке и обновлении списка пользователей из SQLite. </summary>
    public record UsersListRefreshedMessage(IReadOnlyList<UserEntity> Users);
    #endregion

    #region Chat and Network Communication Events
    /// <summary> 🔥 СИГНАЛ-УВЕДОМЛЕНИЕ: Новое сообщение напечатано в UI и готово к отправке на бэк. </summary>
    public record NewMessageSentMessage(string Text, int ReceiverId);

    /// <summary> Сигнал-уведомление о ПОЛУЧЕНИИ сообщения "обратно". </summary>
    public record ReceivedChatMessage(
        int MessageId,
        int SenderId,
        string Text,
        string ChatType,
        string? FilePath = null);

    /// <summary> Сигнал-команда на ОТПРАВКУ сообщения "туда". </summary>
    public record SendChatMessage(
        int SenderId,
        string Text,
        string ChatType,
        int? TargetId = null,
        string? FilePath = null);
    #endregion

    #region Status and Identity Events
    /// <summary> Прямой текстовый приказ для строки состояния. </summary>
    public record StatusTextChangedMessage(string NewStatus);

    /// <summary> Сигнал-уведомление об успешной авторизации пользователя в системе. </summary>
    public record UserLoggedMessage(UserEntity User, bool IsFromAdminPanel = false);
    #endregion



  
        /// <summary>
        /// Глобальный сигнал из ВьюМодели на закрытие и уничтожение главного окна приложения.
        /// </summary>
        public record RequestWindowCloseMessage();





    /// <summary>
    /// Сигнал из ВьюМодели на плавное изменение размера окна (эффект киберпанк-вырастания).
    /// </summary>
    public record ToggleWindowSizeMessage();




    // 🔥 Теперь тут строгий интерфейс на Майн Модел!
    public record ToggleGlobalLoggerAnimationMessage(IMainViewModel MainViewModel);

}



