﻿using Allowed.Telegram.Bot.Attributes;
using Allowed.Telegram.Bot.Extensions.Collections;
using Allowed.Telegram.Bot.Helpers;
using Allowed.Telegram.Bot.Middlewares;
using Allowed.Telegram.Bot.Models;
using Allowed.Telegram.Bot.Models.Enums;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Allowed.Telegram.Bot.Handlers.MessageHandlers
{
    public class MessageHandler
    {
        protected readonly List<Type> _controllerTypes;
        protected readonly ITelegramBotClient _client;
        protected readonly BotData _botData;

        protected readonly IServiceProvider _provider;

        public MessageHandler(ControllersCollection collection, ITelegramBotClient client,
            BotData botData, IServiceProvider provider)
        {
            _client = client;
            _botData = botData;
            _controllerTypes = collection.ControllerTypes
                .Where(c =>
                {
                    BotNameAttribute[] attributes = c.GetBotNameAttributes();
                    return attributes.Length == 0 || attributes.Any(a => a.GetName() == botData.Name);
                }).ToList();

            _provider = provider;
        }

        protected virtual Task<MethodInfo[]> GetAllowedMethods(long chatId)
        {
            return Task.FromResult(_controllerTypes.SelectMany(c => c.GetMethods()).ToArray());
        }

        protected virtual Task<string> GetStateValue(long chatId = 0) => Task.FromResult(string.Empty);

        protected async Task<TelegramMethod> GetMethod(MethodType type, Message message)
        {
            MethodInfo method = null;
            MethodInfo[] allowedMethods = await GetAllowedMethods(message.Chat.Id);

            switch (type)
            {
                case MethodType.ByPath:

                    method = allowedMethods
                        .FirstOrDefault(m => m.GetCommandAttributes().Any(a => $"/{a.GetPath()}" == message.Text));

                    if (method == null)
                        method = allowedMethods.FirstOrDefault(m => m.GetDefaultCommandAttributes().Any());

                    break;

                case MethodType.BySmile:

                    method = allowedMethods
                                .FirstOrDefault(m => m.GetEmojiCommandAttributes().Any(a => message.Text.StartsWith(a.GetSmile())));

                    if (method == null)
                        method = allowedMethods.FirstOrDefault(m => m.GetEmojiDefaultCommandAttributes().Any());

                    break;

                case MethodType.Text:

                    List<MethodInfo> methods = allowedMethods.Where(m => m.GetTextCommandAttributes().Any()).ToList();

                    if (methods.Count != 0)
                    {
                        string userState = await GetStateValue(message.Chat.Id);

                        if (!string.IsNullOrEmpty(userState))
                            method = methods.FirstOrDefault(m => m.GetStateAttributes().Any(s => s.GetState() == userState));

                        if (method == null)
                            method = methods.FirstOrDefault(m => !m.GetStateAttributes().Any());
                    }

                    break;

                case MethodType.ByType:

                    method = allowedMethods
                         .FirstOrDefault(m => m.GetTypedCommandAttributes().Any(a => a.GetMessageType() == message.Type));

                    break;
            }

            if (method != null)
            {
                return new TelegramMethod
                {
                    ControllerType = _controllerTypes.First(c => c == method.DeclaringType),
                    Method = method
                };
            }

            return null;
        }

        protected async Task<TelegramMethod> GetMethod(MethodType type, CallbackQuery callback)
        {
            MethodInfo method = null;
            MethodInfo[] allowedMethods = await GetAllowedMethods(callback.Message.Chat.Id);

            string path = JsonConvert.DeserializeObject<CallbackQueryModel>(callback.Data).Path;

            if (type == MethodType.Callback)
            {
                method = allowedMethods.FirstOrDefault(m => m.GetCallbackQueryAttributes().Any(a => a.GetPath() == path));
            }

            if (method == null)
                method = allowedMethods.FirstOrDefault(m => m.GetCallbackDefaultQueryAttributes().Any());

            if (method != null)
            {
                return new TelegramMethod
                {
                    ControllerType = _controllerTypes.First(c => c == method.DeclaringType),
                    Method = method
                };
            }

            return null;
        }

        protected async Task<TelegramMethod> GetMethod(MethodType type, InlineQuery inline)
        {
            MethodInfo method = null;
            MethodInfo[] allowedMethods = await GetAllowedMethods(inline.From.Id);

            if (type == MethodType.Inline)
            {
                method = allowedMethods.FirstOrDefault(m => m.GetInlineQueryAttributes().Any());
            }

            if (method != null)
            {
                return new TelegramMethod
                {
                    ControllerType = _controllerTypes.First(c => c == method.DeclaringType),
                    Method = method
                };
            }

            return null;
        }

        private async Task<object> InvokeMethod(MethodType type, Message message)
        {
            TelegramMethod method = await GetMethod(type, message);

            if (method != null && method.ControllerType != null && method.Method != null)
            {
                ParameterInfo[] methodParams = method.Method.GetParameters();
                List<object> parameters = new List<object> { };

                if (methodParams.Any(p => p.ParameterType == typeof(MessageData)))
                {
                    parameters.Add(new MessageData
                    {
                        Message = message,
                        Client = _client,
                        BotData = _botData
                    });
                }

                object instance = ActivatorUtilities.CreateInstance(_provider, method.ControllerType);
                return await MethodHelper.InvokeMethod(method.Method, parameters, instance);
            }

            return null;
        }

        private async Task<object> InvokeCallback(MethodType type, CallbackQuery callback)
        {
            TelegramMethod method = await GetMethod(type, callback);

            if (method != null && method.ControllerType != null && method.Method != null)
            {
                ParameterInfo[] methodParams = method.Method.GetParameters();
                List<object> parameters = new List<object> { };

                if (methodParams.Any(p => p.ParameterType == typeof(CallbackQueryData)))
                {
                    parameters.Add(new CallbackQueryData
                    {
                        Client = _client,
                        CallbackQuery = callback,
                        BotData = _botData
                    });
                }

                Type callbackType = typeof(CallbackQueryModel);

                if (methodParams.Any(p => p.ParameterType == callbackType))
                    parameters.Add(JsonConvert.DeserializeObject(callback.Data, callbackType));

                if (methodParams.Any(p => p.ParameterType.IsSubclassOf(callbackType)))
                    parameters.Add(JsonConvert.DeserializeObject(
                        callback.Data, methodParams.First(p => p.ParameterType.IsSubclassOf(callbackType)).ParameterType));

                object instance = ActivatorUtilities.CreateInstance(_provider, method.ControllerType);
                return await MethodHelper.InvokeMethod(method.Method, parameters, instance);
            }

            return null;
        }

        private async Task<object> InvokeInline(MethodType type, InlineQuery inline)
        {
            TelegramMethod method = await GetMethod(type, inline);

            if (method != null && method.ControllerType != null && method.Method != null)
            {
                ParameterInfo[] methodParams = method.Method.GetParameters();
                List<object> parameters = new List<object> { };

                if (methodParams.Any(p => p.ParameterType == typeof(InlineQueryData)))
                {
                    parameters.Add(new InlineQueryData
                    {
                        InlineQuery = inline,
                        Client = _client,
                        BotData = _botData
                    });
                }

                object instance = ActivatorUtilities.CreateInstance(_provider, method.ControllerType);
                return await MethodHelper.InvokeMethod(method.Method, parameters, instance);
            }

            return null;
        }

        protected MethodType GetMethodType(Message message)
        {
            switch (message.Type)
            {
                case MessageType.Text:
                    if (message.Text.StartsWith("/"))
                        return MethodType.ByPath;
                    else if (EmojiHelper.IsStartEmoji(message.Text))
                        return MethodType.BySmile;
                    else
                        return MethodType.Text;
                default:
                    return MethodType.ByType;
            }
        }

        public async Task<object> OnMessage(MessageEventArgs e)
        {
            Message message = e.Message;
            object result = await InvokeMethod(GetMethodType(message), message);

            MessageMiddleware messageMiddleware = _provider.GetService<MessageMiddleware>();
            if (messageMiddleware != null)
            {
                messageMiddleware.AfterMessageProcessed(e.Message.Chat.Id);
                await messageMiddleware.AfterMessageProcessedAsync(e.Message.Chat.Id);
            }

            return result;
        }

        public async Task<object> OnCallbackQuery(CallbackQueryEventArgs e)
        {
            CallbackQuery callback = e.CallbackQuery;
            object result = null;

            if (e.CallbackQuery.Data != null)
                result = await InvokeCallback(MethodType.Callback, callback);

            MessageMiddleware messageMiddleware = _provider.GetService<MessageMiddleware>();
            if (messageMiddleware != null)
            {
                messageMiddleware.AfterCallbackProcessed(e.CallbackQuery.Message.Chat.Id);
                await messageMiddleware.AfterCallbackProcessedAsync(e.CallbackQuery.Message.Chat.Id);
            }

            return result;
        }

        public async Task<object> OnInlineQuery(InlineQueryEventArgs e)
        {
            InlineQuery inline = e.InlineQuery;
            object result = await InvokeInline(MethodType.Inline, inline);

            MessageMiddleware messageMiddleware = _provider.GetService<MessageMiddleware>();
            if (messageMiddleware != null)
            {
                messageMiddleware.AfterCallbackProcessed(e.InlineQuery.From.Id);
                await messageMiddleware.AfterCallbackProcessedAsync(e.InlineQuery.From.Id);
            }

            return result;
        }
    }
}
