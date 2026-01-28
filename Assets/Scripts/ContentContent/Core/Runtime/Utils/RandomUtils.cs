using System;
using System.Diagnostics;
using System.Linq;

namespace ContentContent
{
    public static class RandomUtils
    {
        public static string RandomAlphanumeric(int length)
        {
            Debug.Assert(length > 0);

            var random = new Random();
            const string kChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            return new string(Enumerable.Repeat(kChars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}