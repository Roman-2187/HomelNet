using HomeNetCore.Models;
using System;
using System.Collections.Generic;

namespace HomeNetCore.Messaging
{
    // =================================================================================
    // СИГНАЛЫ ОТОБРАЖЕНИЯ ОКОН И НАВИГАЦИИ (ЧИСТЫЙ C#)
    // =================================================================================

    /// <summary> Сигнал управления отображением центральной таблицы пользователей. </summary>
    public record UserTableVisibilityChangedMessage(bool IsVisible);

    /// <summary> Сигнал от верхней кнопки-тумблера "🛠️ Админка". </summary>
    public record AdminMenuVisibilityChangedMessage(bool IsVisible);

    /// <summary> Универсальный переключатель видимости диалоговых окон. </summary>
    public record FormVisibilityChangedMessage(Type FormType, bool IsVisible);

    /// <summary> 🔥 СИГНАЛ НАВИГАЦИИ: Клик по другу в списке дашборда. </summary>
    public record FriendSelectedMessage(HomeNetCore.Models.UserEntity Friend);


    // =================================================================================
    // СИГНАЛЫ ДАННЫХ И БАЗЫ ДАННЫХ (БИЗНЕС-СОБЫТИЯ)
    // =================================================================================

    /// <summary> Сигнал об успешном удалении пользователя из базы данных. </summary>
    public record UserDeletedMessage(int UserId);

    /// <summary> Сигнал о регистрации и добавлении нового пользователя в СУБД. </summary>
    public record UserAddedMessage(HomeNetCore.Models.UserEntity User);

    /// <summary> Сигнал о полной перезагрузке и обновлении списка пользователей из SQLite. </summary>
    public record UsersListRefreshedMessage(IReadOnlyList<HomeNetCore.Models.UserEntity> Users);

    /// <summary> 🔥 СИГНАЛ-УВЕДОМЛЕНИЕ: Новое сообщение напечатано в UI и готово к отправке на бэк. </summary>
    public record NewMessageSentMessage(string Text, int ReceiverId);


    // =================================================================================
    // СИГНАЛЫ ДЛЯ РАБОТЫ СЕТИ И ЧАТА (ОБМЕН В ОБЕ СТОРОНЫ)
    // =================================================================================

    /// <summary> Сигнал-команда на ОТПРАВКУ сообщения "туда". </summary>
    public record SendChatMessage(
        int SenderId,
        string Text,
        string ChatType,
        int? TargetId = null,
        string? FilePath = null);

    /// <summary> Сигнал-уведомление о ПОЛУЧЕНИИ сообщения "обратно". </summary>
    public record ReceivedChatMessage(
        int MessageId,
        int SenderId,
        string Text,
        string ChatType,
        string? FilePath = null);


    // =================================================================================
    // СИГНАЛЫ СТАТУСА И УВЕДОМЛЕНИЙ
    // =================================================================================

    /// <summary> Прямой текстовый приказ для строки состояния. </summary>
    public record StatusTextChangedMessage(string NewStatus);



    /// <summary> Сигнал-уведомление об успешной авторизации пользователя в системе. </summary>
    public record UserLoggedMessage(UserEntity User, bool IsFromAdminPanel = false);


    /// <summary> Запрос от интерфейса на принудительное обновление и синхронизацию таблиц. </summary>
    public record RequestStatusRefreshMessage();


}
