using Grpc.Core;
using Message.RPC.DataContext.Message;
using Message.RPC.DataContext.User;
using Message.RPC.Entities.Message.Message;
using Message.RPC.Entities.User;
using Message.RPC.Protos.GeneralNotification;
using Message.RPC.RabbitMQ;
using Message.RPC.ReusableClass;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Message.RPC.Services.SystemMessage
{
    public class GeneralNotificationService : SendGeneralNotification.SendGeneralNotificationBase
    {
        //依赖注入
        private readonly ILogger<GeneralNotificationService> _logger;
        private readonly IDistributedCache _distributedCache;
        private readonly MessageContext _messageContext;
        private readonly UserContext _userContext;
        private readonly IMessagePublisher _messagePublisher;

        public GeneralNotificationService(ILogger<GeneralNotificationService> logger, IDistributedCache distributedCache, MessageContext messageContext, UserContext userContext, IMessagePublisher messagePublisher)
        {
            _logger = logger;
            _distributedCache = distributedCache;
            _messageContext = messageContext;
            _userContext = userContext;
            _messagePublisher = messagePublisher;
        }

        public override async Task<GeneralReply> SendGeneralNotificationSingle(SendGeneralNotificationSingleRequest request, ServerCallContext context)
        {
            string? id = context.GetHttpContext().Request.Headers["id"];
            int sender = (
                await _messageContext
                .SystemPromotions
                .Select(p => new { p.UUID, p.MiniAppId })
                .FirstOrDefaultAsync(p => p.MiniAppId == id)
                )!.UUID;

            //查找数据库
            int? targetUser = await _userContext.UserAccounts
                .Select(account => account.UUID)
                .FirstOrDefaultAsync(UUID => UUID == request.UUID);
            if (targetUser == null)
            {
                _logger.LogWarning("Warning：MiniApp[ {UUID} ]正在尝试向不存在的用户[ {targetUser} ]发送消息", id, request.UUID);
                GeneralReply sendMessageFailed = new()
                {
                    Code = 1,
                    Message = "发送失败，目标用户不存在",
                };
                return sendMessageFailed;
            }

            //使用事务
            //由于需要操作不同的数据库，故这里应当使用分布式事务，但.NET中的分布式事务目前只支持Windows平台上的.NET7.0，故暂时无法使用
            //这里的事务使用方法是错误的
            using var messageContextTransaction = _messageContext.Database.BeginTransaction();
            {
                using var userContextTransaction = _userContext.Database.BeginTransaction();
                try
                {
                    Entities.Message.Message.SystemMessage message = new(id: 0, senderId: sender, receiverId: request.UUID, createdTime: DateTime.Now, isCustom: true, isRecalled: false, isReply: false, isMediaMessage: false, isVoiceMessage: false, customType: "GeneralNotification", minimumSupportVersion: "1.0.0", textOnError: "应用版本过低，请升级至V1.0.0版本以上以阅读此消息", 
                        customMessageContent: JsonSerializer.Serialize(new
                        {
                            title = request.Title,
                            description = request.Description,
                            contents = request.Contents,
                            openPageUrl = request.OpenPageUrl,
                            openPageText = request.OpenPageText??"查看详情",
                            miniAppId = id,
                        }, options: new(){ PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
                        , messageReplied: null, messageText: request.MessageText, messageMedias: null, messageVoice: null);
                    _messageContext.SystemMessages.Add(message);

                    _messageContext.SaveChanges();

                    Entities.Message.Chat.Chat? receiverChat = await _messageContext.Chats.FirstOrDefaultAsync(chat => chat.UUID == request.UUID && chat.TargetId == sender && chat.IsWithSystem);
                    int receiverChatId;
                    if (receiverChat == null)
                    {
                        Entities.Message.Chat.Chat newReceiverChat = new(id: 0, UUID: request.UUID, targetId: sender, isWithOtherUser: false, isWithGroup: false, isWithSystem: true, isStickyOnTop: false, isDeleted: false, numberOfUnreadMessages: 1, lastMessageId: message.Id, updatedTime: message.CreatedTime);
                        _messageContext.Chats.Add(newReceiverChat);
                        _messageContext.SaveChanges();

                        receiverChatId = newReceiverChat.Id;
                    }
                    else
                    {
                        receiverChat!.IsDeleted = false;
                        receiverChat.NumberOfUnreadMessages++;
                        receiverChat.LastMessageId = message.Id;
                        receiverChat.UpdatedTime = message.CreatedTime;

                        receiverChatId = receiverChat.Id;
                    }

                    _messageContext.SaveChanges();

                    UserSyncTable? currentReceiverSyncTable = await _userContext.UserSyncTables.FirstOrDefaultAsync(table => table.UUID == request.UUID);
                    int newReceiverSequence = currentReceiverSyncTable!.SequenceForSystemMessages + 1;
                    SystemMessageInbox receiverInbox = new(id: 0, UUID: request.UUID, messageId: message.Id, chatId: receiverChatId, isDeleted: false, sequence: newReceiverSequence);

                    _messageContext.SystemMessageInboxes.Add(receiverInbox);
                    _messageContext.SaveChanges();

                    currentReceiverSyncTable.SequenceForSystemMessages++;
                    currentReceiverSyncTable.UpdatedTimeForChats = message.CreatedTime;
                    _userContext.SaveChanges();

                    userContextTransaction.Commit();
                    messageContextTransaction.Commit();

                    SystemMessageDataForClient dataForReceiver = new(message.Id, receiverInbox.ChatId, message.SenderId, message.ReceiverId, message.CreatedTime, message.IsCustom, message.IsRecalled, false, message.IsReply, message.IsMediaMessage, message.IsVoiceMessage, message.CustomType, message.MinimumSupportVersion, message.TextOnError, message.CustomMessageContent, message.MessageReplied, message.MessageText, message.MessageMedias, message.MessageVoice, currentReceiverSyncTable.SequenceForCommonMessages);

                    //操作完数据库后，使用消息队列发送消息
                    //通过Redis查找目标用户上一次在哪一台服务器连接了WebSocket，尝试由那台服务器发送消息
                    string? webSocketPort = await _distributedCache.GetStringAsync(request.UUID + "WebSocket");
                    if (webSocketPort != null)
                    {
                        _messagePublisher.SendMessage(new { type = "NewSystemMessage", data = dataForReceiver }, "msg", webSocketPort);
                    }
                    else
                    {
                        //表明无法通过WebSocket发送此消息，需要将该消息视作发送失败，进入MongoDB中
                    }


                    GeneralReply sendMessageSucceed = new()
                    {
                        Code = 0,
                        Message = "发送成功",
                    };
                    return sendMessageSucceed;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error：系统推送[ {UUID} ]在向[ {targetUser} ]发送消息时发生错误。报错信息为[ {ex} ]。", sender, request.UUID, ex);
                    GeneralReply sendMessageFailed = new()
                    {
                        Code = 2,
                        Message = "发送失败，服务器内部错误",
                    };
                    return sendMessageFailed;
                }
            }
        }

    }
}
