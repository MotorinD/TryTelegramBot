using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using File = System.IO.File;

namespace Task9
{
    internal class TelegramModule
    {
        DataManager dataManager = new DataManager();

        public TelegramModule(string token)
        {
            var tgBot = new TelegramBotClient(token);

            using (var cts = new CancellationTokenSource())
            {

                // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
                var receiverOptions = new ReceiverOptions
                {
                    AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
                };

                tgBot.StartReceiving(
                    updateHandler: this.HandleUpdateAsync,
                    pollingErrorHandler: this.HandlePollingErrorAsync,
                    receiverOptions: receiverOptions,
                    cancellationToken: cts.Token
                );
            }
        }

        /// <summary>
        /// Метод вызываемый при получении сообщения
        /// </summary>
        async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message is null)
                return;

            var message = update.Message;

            this.WriteMessageOnConsole(message);

            if (message.Type == MessageType.Document
                || message.Type == MessageType.Audio
                || message.Type == MessageType.Photo)
            {
                this.DownloadFile(botClient, message, cancellationToken);
                return;
            }

            await this.RepeatUserMessage(botClient, message, cancellationToken);

            if (this.MessageTextIsCommand(message))
            {
                switch (message.Text)
                {
                    case "/start":
                        await this.StartReaction(botClient, message.Chat.Id, cancellationToken);
                        break;
                    case "/help":
                        await this.SendHelpText(botClient, message.Chat.Id, cancellationToken);
                        break;
                    case "/1":
                        await this.SendCatImageToUser(botClient, update.Message.Chat.Id, cancellationToken);
                        break;
                    case "/2":
                        await this.SendFileList(botClient, update.Message.Chat.Id, cancellationToken);
                        break;
                    case var someVal when new Regex(@"^\/3.").IsMatch(someVal):
                        await this.SendFile(botClient, update.Message.Chat.Id, someVal, cancellationToken);
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Справка по комадам для пользователя
        /// </summary>
        async Task SendHelpText(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            Message sentMessage = await botClient.SendTextMessageAsync(
                chatId: userId,
                text: "Команды:\n" +
                "/1 ===> Для получения кота\n" +
                "/2 ===> Список файлов\n" +
                "/3 ===> Получить файл. Вводить в формате /3 {fileNum} с пробелом между /3 и номером файла fileNum - номер файла из списка по команде /2",
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Отправить пользователю выбранный файл
        /// </summary>
        async Task SendFile(ITelegramBotClient botClient, long userId, string cmd, CancellationToken cancellationToken)
        {
            var fileArr = Directory.GetFiles(@"files");

            if (fileArr.Length == 0)
                return;

            var cmdArr = cmd.Split(' ');
            if (cmd.Length <= 1)
                return;

            if (int.TryParse(cmdArr[1], out var fileNum) && fileNum > 0 && fileNum <= fileArr.Length)
            {
                var fs = File.Open(fileArr[fileNum - 1], FileMode.Open);

                await botClient.SendDocumentAsync(
                    chatId: userId,
                    document: fs,
                    cancellationToken: cancellationToken);
            }
        }

        /// <summary>
        /// Отправить пользователю список файлов которые он насохранял
        /// </summary>
        async Task SendFileList(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            var fileArr = Directory.GetFiles(@"files");

            if (fileArr.Length == 0)
                return;

            var text = new StringBuilder();

            for (int i = 0; i < fileArr.Length; i++)
                text.AppendLine($"{i + 1} ===> {fileArr[i]}\n");

            var sentMessage = await botClient.SendTextMessageAsync(
                chatId: userId,
                text: text.ToString(),
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Вывод ошибок
        /// </summary>
        Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine(exception.ToString());
            return Task.CompletedTask;
        }

        /// <summary>
        /// Загрузка и созранение файла отправленного пользователем
        /// </summary>
        async void DownloadFile(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            var fileId = string.Empty;
            var fileName = string.Empty;

            if (!Directory.Exists(@"files/"))
                Directory.CreateDirectory(@"files/");

            switch (message.Type)
            {
                case MessageType.Photo:
                    fileId = message.Photo.LastOrDefault().FileId;
                    fileName = fileId + ".jpg";
                    break;

                case MessageType.Audio:
                    fileId = message.Audio.FileId;
                    fileName = message.Audio.FileName;
                    break;

                case MessageType.Document:
                    fileId = message.Document.FileId;
                    fileName = message.Document.FileName;
                    break;

                default:
                    return;
            }

            var file = await botClient.GetFileAsync(fileId, cancellationToken);
            var fs = new FileStream($@"files\{fileName}", FileMode.Create);
            await botClient.DownloadFileAsync(file.FilePath, fs, cancellationToken);
            fs.Close();
            fs.Dispose();
        }

        /// <summary>
        /// Вывод полученного сообщения на консоль
        /// </summary>
        private void WriteMessageOnConsole(Message message)
        {
            Console.WriteLine($"=> {DateTime.Now} Received a '{message.Text}' message ChatId:{message.Chat.Id}. Type:{message.Type} \n from {message.From?.FirstName} {message.From?.LastName} {message.From.Username}\n");
        }

        /// <summary>
        /// Реакция на команду /start
        /// </summary>
        async Task StartReaction(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            Message sentMessage = await botClient.SendTextMessageAsync(
                chatId: userId,
                text: "Реагирую на команду /start\nЕсли нужна помощь по командам - /help",
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Ответить пользователю на его сообщение
        /// </summary>
        async Task RepeatUserMessage(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(message.Text))
                return;

            // Echo received message text
            Message sentMessage = await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "You said:\n" + message.Text,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Проверка, является ли сообщение командой
        /// </summary>
        private bool MessageTextIsCommand(Message message)
        {
            if (message.Entities == null)
                return false;

            return message.Entities[0].Type == MessageEntityType.BotCommand;
        }

        /// <summary>
        /// Отправить картинку с котом указанному пользователю
        /// </summary>
        async Task SendCatImageToUser(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            var pathToFile = this.dataManager.DownloadCatImage(userId);
            var fs = File.Open(pathToFile, FileMode.Open);

            await botClient.SendPhotoAsync(
                chatId: userId,
                photo: fs,
                cancellationToken: cancellationToken);
        }
    }

}
