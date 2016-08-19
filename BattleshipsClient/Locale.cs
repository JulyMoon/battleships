using System.Globalization;

namespace BattleshipsClient
{
    public class Locale
    {
        public string Title { get; private set; }
        public string PlacementStatus { get; private set; }
        public string ConnectingStatus { get; private set; }
        public string RandomButton { get; private set; }
        public string PlayButton { get; private set; }
        public string ContinueButton { get; private set; }
        public string Win { get; private set; }
        public string Loss { get; private set; }
        public string YourTurn { get; private set; }
        public string OpponentsTurn { get; private set; }
        public string Waiting { get; private set; }
        public string Alphabet { get; private set; }

        public static readonly Locale English = new Locale
        {
            Title = "Battleships Beta",
            PlacementStatus = "Place the ships",
            ConnectingStatus = "Connecting to the server...",
            RandomButton = "Randomize",
            PlayButton = "Play",
            ContinueButton = "Continue",
            Win = "You won!",
            Loss = "You lost!",
            YourTurn = "Your turn",
            OpponentsTurn = "Opponent's turn",
            Waiting = "Waiting for opponent...",
            Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
        };

        public static readonly Locale Russian = new Locale
        {
            Title = "Морской бой Beta",
            PlacementStatus = "Расставь корабли",
            ConnectingStatus = "Подключение к серверу...",
            RandomButton = "Случайно",
            PlayButton = "Играть",
            ContinueButton = "Продолжить",
            Win = "Ты выиграл!",
            Loss = "Ты проиграл!",
            YourTurn = "Твой ход",
            OpponentsTurn = "Ход противника",
            Waiting = "Поиск противника...",
            Alphabet = "АБВГДЕЖЗИКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯЁЙ"
        };

        public static Locale GetLocale(CultureInfo cultireInfo)
        {
            switch (cultireInfo.TwoLetterISOLanguageName)
            {
                case "ru": return Russian;
                default: return English;
            }
        }
    }
}
