namespace ABShared.Enum
{
    public static class EnumsHelper
    {
        public static string ToMyString(this TenisGamePart part)
        {
            var str = part.ToString();
            str = str.Replace("Game", " Гейм: ");
            return str;
        }


    }
}
