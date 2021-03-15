using System;
using System.Collections.Generic;

public static class StringExtensions
{
    public static string[] SubstringAtCount(this string self, int count)
    {
        var result = new List<string>();
        var length = (int) Math.Ceiling((double) self.Length / count);

        for (int i = 0; i < length; i++)
        {
            int start = count * i;
            if (self.Length <= start)
            {
                break;
            }

            if (self.Length < start + count)
            {
                result.Add(self.Substring(start));
            }
            else
            {
                result.Add(self.Substring(start, count));
            }
        }

        return result.ToArray();
    }
}