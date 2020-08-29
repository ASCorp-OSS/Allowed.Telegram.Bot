﻿using Allowed.Telegram.Bot.Models.Store;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Allowed.Telegram.Bot.Services.UserServices
{
    public interface IUserService<TKey, TUser>
        where TKey : IEquatable<TKey>
        where TUser : TelegramUser<TKey>
    {
        Task CheckUser(long chatId, string username);

        Task<bool> AnyUser(string username);
        Task<bool> AnyUser(long chatId);

        Task<TUser> GetUser(string username);
        Task<TUser> GetUser(long chatId);

        Task<List<TUser>> GetUsersByRole(string role);
    }
}
