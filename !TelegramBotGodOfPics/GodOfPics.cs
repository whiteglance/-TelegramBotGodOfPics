using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Net.Http;
using Newtonsoft.Json;
using _TelegramBotGodOfPics.Models;
using static System.Net.WebRequestMethods;
using System.Text;

namespace _TelegramBotGodOfPics
{
    internal class GodOfPics
    {
        private readonly HttpClient _httpClient;

        public GodOfPics()
        {
            _httpClient = new HttpClient();
        }
        TelegramBotClient botClient = new TelegramBotClient("5987530850:AAHo4i35n5eYt65VirgDZihUEtwPua-cpIQ");
        CancellationToken cancellationToken = new CancellationToken();
        ReceiverOptions receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        public async Task Start()
        {
            botClient.StartReceiving(HandlerUpdateAsync, HandlerError, receiverOptions, cancellationToken);
            var botMe = await botClient.GetMeAsync();
            Console.WriteLine($"Bot {botMe.Username} почав працювати");
            Console.ReadKey();
        }

        private Task HandlerError(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Помилка в телеграм бот API: \n{apiRequestException.ErrorCode}" +
                $"\n{apiRequestException.Message}",
                _ => exception.ToString()
            };
            Console.WriteLine(errorMessage);
            return Task.CompletedTask;
        }

        private async Task HandlerUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if(update.Type == UpdateType.Message && update?.Message?.Text!=null)
            {
                await HandlerMessageAsync(botClient, update.Message);
            }
        }

       

        private async Task HandlerMessageAsync(ITelegramBotClient botClient, Message message)
        {
            if(message.Text == "/start")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Выберіть команду для початку роботи з ботом!");
                await botClient.SendTextMessageAsync(message.Chat.Id, "\nПросто пишіть запит у чат, щоб отримати фото!\n\nІнші команди:\n\n/random - показує рандомне фото\n/best - видає краще фото за останній час");
                await botClient.SendTextMessageAsync(message.Chat.Id, "\n/save - зберігає фото в улюблені\n/delete - видаляє фото з улюблених");
            }
            if (message.Text == "/best")
            {
                var apiUrl = "https://localhost:7164/api/Photos/best";

                var response = await _httpClient.GetAsync(apiUrl);
                response.EnsureSuccessStatusCode();
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var photoData = JsonConvert.DeserializeObject<Photo>(jsonResponse);
                var url = photoData?.Urls.Regular;

                if (!string.IsNullOrEmpty(url))
                {
                    var photoUrl = photoData?.Urls?.Regular;

                    message = await botClient.SendPhotoAsync(
                        chatId: message.Chat.Id,
                        photo: InputFile.FromUri(photoUrl),
                        parseMode: ParseMode.Html,
                        cancellationToken: cancellationToken);

                    // Сохранение фото в базу данных
                    await SavePhotoAsync(photoUrl);
                }
                else
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Failed to retrieve a random photo.");
                }
            }
            if (message.Text == "/random")
            {
                var apiUrl = "https://localhost:7164/api/Photos/random";

                var response = await _httpClient.GetAsync(apiUrl);
                response.EnsureSuccessStatusCode();
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var photoData = JsonConvert.DeserializeObject<Photo>(jsonResponse);
                var url = photoData?.Urls.Regular;

                if (!string.IsNullOrEmpty(url))
                {
                    var photoUrl = photoData?.Urls?.Regular;

                    message = await botClient.SendPhotoAsync(
                        chatId: message.Chat.Id,
                        photo: InputFile.FromUri(photoUrl),
                        parseMode: ParseMode.Html,
                        cancellationToken: cancellationToken);

                    await SavePhotoAsync(photoUrl);
                }
                else
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Failed to retrieve a random photo.");
                }
            }
            if (message.Text != null && message.Text.Trim() != string.Empty)
            {
                var messageText = message.Text.Trim();

                var chatId = message.Chat.Id;
                Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");
                
                if (messageText.StartsWith("/search"))
                {
                    var query = messageText.Substring("/search".Length).Trim();

                    if (!string.IsNullOrEmpty(query))
                    {
                        var apiUrl = $"https://localhost:7164/api/Photos/search?query={Uri.EscapeDataString(query)}";
                        var response = await _httpClient.GetAsync(apiUrl);
                        response.EnsureSuccessStatusCode();

                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        var photoData = JsonConvert.DeserializeObject<Photo>(jsonResponse);
                        var url = photoData?.Urls.Regular;

                        if (!string.IsNullOrEmpty(url))
                        {
                            var photoUrl = photoData?.Urls?.Regular;

                            await botClient.SendPhotoAsync(
                                chatId: chatId,
                                photo: InputFile.FromUri(photoUrl),
                                parseMode: ParseMode.Html,
                                cancellationToken: cancellationToken);
                            
                            await SavePhotoAsync(photoUrl);
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(chatId, "Не вдалось отримати фото по запиту");
                        }
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(chatId, "Будь ласка, введіть запит для пошуку фото.");
                    }
                }
                else if (messageText.StartsWith("/delete"))
                {
                    var query = messageText.Substring("/delete".Length).Trim();
                    if (!string.IsNullOrEmpty(query))
                    {
                        var apiUrl = $"https://localhost:7164/api/Photos/delete?id={Uri.EscapeDataString(query)}";
                        var response = await _httpClient.DeleteAsync(apiUrl);
                        response.EnsureSuccessStatusCode();

                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        var photoData = JsonConvert.DeserializeObject<Photo>(jsonResponse);
                        var id = photoData?.Id;

                        if (!string.IsNullOrEmpty(id))
                        {
                            await botClient.SendTextMessageAsync(chatId, $"фото з ID = {id} - видалено!");

                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(chatId, "Не вдалось видалити фото");
                        }
                    }

                }

            }
            

        }
        private async Task SavePhotoAsync(string photoUrl)
        {
            var apiUrl = "https://localhost:7164/api/Photos/save";

            var photo = new Photo
            {
                Id = Guid.NewGuid().ToString(),
                Title = "title",
                Urls = new Urls
                {
                    Regular = Convert.ToString(photoUrl)
                }
            };

            var jsonBody = JsonConvert.SerializeObject(photo);
            var saveContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(apiUrl, saveContent);
            response.EnsureSuccessStatusCode();
        }


    }
}
