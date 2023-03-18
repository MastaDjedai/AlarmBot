using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Metrics;
using System.Security.Cryptography.X509Certificates;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace GroupCallerBot
{
    public class UsersData
    {
        public int id { get;set; }
        public long userId { get; set; }
        public string nickName { get; set; }
        public long chatId { get; set; }
        public UsersData()
        {

        }
        public UsersData(long userid, string nickname, long chatid)
        {
            userId = userid;
            nickName = nickname;
            chatId = chatid;
        }
    }

    public class AppllicationContext : DbContext
    {
        public DbSet<UsersData> Users => Set<UsersData>();

        public AppllicationContext()
        {
            Database.EnsureCreated();
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source = UsersDB");
        }
    }
    public class BotConfig
    {
        async private Task BotKeyboard(ITelegramBotClient botClient, Message message)
        {
            ReplyKeyboardMarkup keyboard = new(new[]
            {
                new KeyboardButton[] { "Клич усіх" },
                new KeyboardButton[] {"/shareNickname", "/exitFromAnnouncement"}
            })
            {
                ResizeKeyboard = true
            };
            Message showKeyboard = await botClient.SendTextMessageAsync(message.Chat.Id, "Вітаю", replyMarkup: keyboard);
        }
        async public Task HandleMessage(ITelegramBotClient botClient, Message message)
        {
            var chatId = message.Chat.Id;
            if (message.Text == "/start")
            {
                await BotKeyboard(botClient, message);
            }
            if (message.Text == "/shareNickname")
            {
                await NickNamesRecording(botClient, message);
            }
            if (message.Text == "Клич усіх")
            {
                await Alarm(botClient, message);
            }
            if (message.Text == "/exitFromAnnouncement")
            {
                await ExitFromAnnouncement(botClient, message);
            }
        }
        async private Task NickNamesRecording(ITelegramBotClient botClient, Message message)
        {
            long chatId = message.Chat.Id;
            long userId = message.From.Id;
            ChatMember memberUsername = await botClient.GetChatMemberAsync(chatId, userId);
            string userName = "";
            if (memberUsername != null)
            {
                if (memberUsername.User.Username != null)
                {
                    userName = memberUsername.User.Username;
                }
                else if (memberUsername.User.Username == null)
                {
                    userName = memberUsername.User.FirstName + " " + memberUsername.User.LastName;
                }
                UsersData user = new UsersData(userId, userName, chatId);

                using (AppllicationContext Data = new())
                {
                    bool objectExist = await Data.Users.AnyAsync(u=>u.chatId == chatId && u.userId == userId);
                    if (!objectExist)
                    {
                        Data.Users.Add(user);
                        Data.SaveChanges();
                    }
                }
            }
        }
        async private Task Alarm(ITelegramBotClient botClient, Message message)
        {
            using (AppllicationContext data = new())
            {
                List<UsersData> membersList = data.Users.ToList();
                foreach (var members in membersList)
                {
                    if (message.Chat.Id == members.chatId)
                    {
                        Message printMembers = await botClient.SendTextMessageAsync(message.Chat.Id, "@" + members.nickName);
                    }
                }

            }
        }
        async private Task ExitFromAnnouncement(ITelegramBotClient botClient, Message message)
        {
            using (AppllicationContext data = new())
            {
                long chatId = message.Chat.Id;
                long userId = message.From.Id;
                DbSet<UsersData> chatMembers = data.Set<UsersData>();
                var member = await botClient.GetChatMemberAsync(chatId, userId);
                var usersToDelete = chatMembers.Where(x => x.nickName == member.User.Username && x.chatId == chatId && x.userId == userId);
                chatMembers.RemoveRange(usersToDelete);
                data.SaveChanges();
            }
        }

    }
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("PrivetChert");
            var botClient = new TelegramBotClient("6159197679:AAEG_oK70Bz0obHryWYHMay-WHhstVvXiYE");
            BotConfig config = new BotConfig();
            ReceiverOptions receiverOptions = new()
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };
            using var cts = new CancellationTokenSource();
            botClient.StartReceiving(HandleUpdateAsync, HandlePollingErrorAsync, receiverOptions, cancellationToken: cts.Token);

            async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
            {
                if (update.Type == UpdateType.Message && update?.Message?.Text != null)
                {
                    await config.HandleMessage(botClient, update.Message);
                }
            }

            static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
            {
                var ErrorMessage = exception switch
                {
                    ApiRequestException apiRequestException
                        => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                    _ => exception.ToString()
                };

                Console.WriteLine(ErrorMessage);
                return Task.CompletedTask;
            }


            Console.ReadLine();
        }
    }
}