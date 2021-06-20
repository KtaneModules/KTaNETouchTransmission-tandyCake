using System;
using System.Collections.Generic;
using System.Linq;

public static class BrailleData
{
    static Dictionary<string, bool[]> BrailleLetters = new Dictionary<string, bool[]>()
    {
        { "A", new bool[] { true, false, false, false, false, false } },
        { "B", new bool[] { true, true, false, false, false, false } },
        { "C", new bool[] { true, false, false, true, false, false } },
        { "D", new bool[] { true, false, false, true, true, false } },
        { "E", new bool[] { true, false, false, false, true, false } },
        { "F", new bool[] { true, true, false, true, false, false } },
        { "G", new bool[] { true, true, false, true, true, false } },
        { "H", new bool[] { true, true, false, false, true, false, } },
        { "I", new bool[] { false, true, false, true, false, false } },
        { "J", new bool[] { false, true, false, true, true, false } },
        { "K", new bool[] { true, false, true, false, false, false } },
        { "L", new bool[] { true, true, true, false, false, false } },
        { "M", new bool[] { true, false, true, true, false, false } },
        { "N", new bool[] { true, false, true, true, true, false } },
        { "O", new bool[] { true, false, true, false, true, false } },
        { "P", new bool[] { true, true, true, true, false, false } },
        { "Q", new bool[] { true, true, true, true, true, false } },
        { "R", new bool[] { true, true, true, false, true, false } },
        { "S", new bool[] { false, true, true, true, false, false } },
        { "T", new bool[] { false, true, true, true, true, false } },
        { "U", new bool[] { true, false, true, false, false, true } },
        { "V", new bool[] { true, true, true, false, false, true } },
        { "W", new bool[] { false, true, false, true, true, true } },
        { "X", new bool[] { true, false, true, true, false, true } },
        { "Y", new bool[] { true, false, true, true, true, true } },
        { "Z", new bool[] { true, false, true, false, true, true } },
        { "AND", new bool[] { true, true, true, true, false, true } },
        { "FOR", new bool[] { true, true, true, true, true, true } },
        { "THE", new bool[] { false, true, true, true, false, true } },
        { "WITH", new bool[] { false, true, true, true, true, true } },
        { "AR", new bool[] { false, false, true, true, true, false } },
        { "BB", new bool[] { false, true, true, false, false, false } },
        { "CC", new bool[] { false, true, false, false, true, false } },
        { "CH", new bool[] { true, false, false, false, false, true } },
        { "EA", new bool[] { false, true, false, false, false, false } },
        { "ED", new bool[] { true, true, false, true, false, true } },
        { "EN", new bool[] { false, true, false, false, false, true } },
        { "ER", new bool[] { true, true, false, true, true, true } },
        { "FF", new bool[] { false, true, true, false, true, false } },
        { "GG", new bool[] { false, false, false, false, false, false } },
        { "GH", new bool[] { true, true, false, false, false, true } },
        { "IN", new bool[] { false, false, true, false, true, false } },
        { "ING", new bool[] { false, false, true, true, false, true } },
        { "OF", new bool[] { true, true, true, false, true, true } },
        { "OU", new bool[] { true, true, false, false, true, true } },
        { "OW", new bool[] { false, true, false, true, false, true } },
        { "SH", new bool[] { true, false, false, true, false, true } },
        { "ST", new bool[] { false, false, true, true, false, false } },
        { "TH", new bool[] { true, false, false, true, true, true } },
        { "WH", new bool[] { true, false, false, false, true, true } },
     };
    private static string[] glyphNames = BrailleLetters.Select(x => x.Key).ToArray();

    public static List<string> Split(string input)
    {
        input = input.ToUpperInvariant();
        List<string> output = new List<string>();
        while (input.Length > 0)
        {
            List<string> substrings = Enumerable.Range(1, input.Length).Select(x => input.Substring(0, x)).ToList();
            string addition = substrings.Last(x => glyphNames.Contains(x));
                output.Add(addition);
            input = input.Skip(addition.Length).Join("");
        }
        return output;
    }
    public static List<bool[]> WordToBraille(string input)
    {
        return Split(input).Select(x => BrailleLetters[x]).ToList();
    }
    public static string BrailleToWord(List<bool[]> input)
    {
        if (input.Any(x => x.Length != 6))
            throw new ArgumentOutOfRangeException("input.Length");
        string output = string.Empty;
        foreach (bool[] letter in input)
        {
            foreach (KeyValuePair<string, bool[]> entry in BrailleLetters)
            {
                if (entry.Value.SequenceEqual(letter))
                {
                    output += entry.Key;
                    break;
                }
                output += "?";   
            }
        }
        return output;
    }
}