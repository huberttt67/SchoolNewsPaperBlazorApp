namespace SchoolNewspaperBlazorApp.Data
{
    public static class LessonHours
    {
        private static readonly (TimeOnly Start, TimeOnly End)[] Hours =
        {
            (new(8, 0),  new(8, 45)),
            (new(8, 50), new(9, 35)),
            (new(9, 40), new(10, 25)),
            (new(10, 30), new(11, 15)),
            (new(11, 20), new(12, 5)),
            (new(12, 25), new(13, 10)),
            (new(13, 15), new(14, 0)),
            (new(14, 5),  new(14, 50))
        };

        public static int Count => Hours.Length;

        public static TimeOnly GetStartTime(int lessonNumber)
        {
            ValidateLessonNumber(lessonNumber);
            return Hours[lessonNumber - 1].Start;
        }

        public static TimeOnly GetEndTime(int lessonNumber)
        {
            ValidateLessonNumber(lessonNumber);
            return Hours[lessonNumber - 1].End;
        }

        private static void ValidateLessonNumber(int lessonNumber)
        {
            if (lessonNumber < 1 || lessonNumber > Hours.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(lessonNumber),
                    "Numer lekcji musi być od 1 do 8.");
            }
        }
    }
}