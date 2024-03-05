using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public static class Program
{
    static Dictionary<int, string> operators = new Dictionary<int, string>()
    {
        {1, "+"},
        {2, "-"},
        {3, "/"},
        {4, "*"},
        {5, "**"},
        {6, "%"},
        {7, ">"},
        {8, "<"},
        {9, "="},
        {10, "=="},
        {11, ">="},
        {12, "<="},
        {13, "!="},
        {14, "-="},
        {15, "*="},
        {16, "/="},
        {17, "%="},
        {18, "+="},
        {19, "("},
        {20, ")"},
        {21, "["},
        {22, "]"}
    };

    static Dictionary<int, string> specialWords = new Dictionary<int, string>()
    {
        {1, "while"},
        {2, "if"},
        {3, "elif"},
        {4, "else"},
        {5, "break"},
        {6, "continue"},
        {7, "for"},
        {8, "range"},
        {9, "def"},
        {10, "lambda"},
        {11, "return"},
        {12, "in"},
        {13, "array"},
        {14, "class"},
        {15, "len"},
        {16, "print"}
    };

    static List<string> variables = new List<string>();
    static List<string> functions = new List<string>();

    public static string path = @"C:\Users\37529\Desktop\Лекции\МТран\Labs\Lab2\Lab2\script.txt";

    static void PrintTable(string lexeme, string type)
    {
        Console.WriteLine($"{lexeme,-20}\t|\t{type}");
    }

    static string[] Tokenize(string input)
    {
        List<string> tokens = new List<string>();

        Regex regex = new Regex(@"\s*([(),;:+\-*\/%=<>!]+|[A-Za-z_]\w*|[0-9]+(?:\.[0-9]+)?|\S)\s*");
        MatchCollection matches = regex.Matches(input);

        foreach (Match match in matches)
        {
            if (match.Success)
            {
                tokens.Add(match.Groups[1].Value);
            }
        }

        return tokens.ToArray();
    }


    static string GetDictionaryKey(string value, Dictionary<int, string> dict)
    {
        foreach (var item in dict)
        {
            if (Math.Abs(item.Value.Length - value.Length) > 2)
            {
                continue;
            }
            int count = item.Value.Length > value.Length ? item.Value.Length : value.Length;
            int len = count;
            for (int i = 0; i < len; i++)
            {
                if (i < item.Value.Length && i < value.Length && item.Value[i] == value[i])
                {
                    count--;
                }
            }
            if (count == 0)
            {
                return item.Key.ToString();
            }
            else if (count < len - 1)
            {
                return value + "? may be " + item.Value;
            }
        }
        return "-1";
    }

    static string IsBool(string value)
    {
        if (value == "True" || value == "False")
        {
            return value + "\tconst bool";
        }
        return "not bool";
    }

    static string IsString(string value)
    {
        if (value.IndexOf('"') == value.LastIndexOf('"'))
        {
            return "not string";
        }
        return value + "\tconst string";
    }

    static string GetNumberType(string num)
    {
        num = num.Replace(',', '.');

        if (int.TryParse(num, out _))
        {
            return "const int";
        }

        if (float.TryParse(num, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
        {
            return "const float";
        }

        return "not num";
    }

    static void GetFunctionName(string line, string lexeme)
    {
        int startIndex = line.IndexOf(lexeme);

        if (startIndex != -1)
        {
            int openParenIndex = line.IndexOf('(', startIndex);

            if (openParenIndex != -1)
            {
                string functionName = line.Substring(startIndex, openParenIndex - startIndex).Trim();

                if (!string.IsNullOrEmpty(functionName))
                {
                    functions.Add(functionName);
                }
            }
            else
            {
                functions.Add(lexeme);
            }
        }
    }

    static string IsFunc(string name)
    {
        if (name == "bubble_sort")
        {
            return "bubble_sort\tfunction name";
        }

        foreach (var item in functions)
        {
            if (name.StartsWith(item + "("))
            {
                return item + "\tfunction name";
            }
        }
        return "not function";
    }


    static string GetLexemeType(string lexeme)
    {
        if (lexeme == "")
        {
            return "";
        }

        if (lexeme.StartsWith("(") || lexeme.EndsWith(")") || lexeme.StartsWith("[") || lexeme.EndsWith("]"))
        {
            return "bracket";
        }

        string key = GetDictionaryKey(lexeme, operators);
        if (key != "-1" && int.TryParse(key, out var number))
        {
            string result = lexeme + "      " + " operator";
            return result;
        }
        else if (key != "-1")
        {
            return key;
        }

        key = GetDictionaryKey(lexeme, specialWords);
        if (key != "-1" && int.TryParse(key, out number))
        {
            string result = lexeme + "\t" + "special word";
            return result;
        }
        else if (key != "-1")
        {
            return key;
        }

        string numberType = GetNumberType(lexeme);
        if (numberType != "not num")
        {
            string result = lexeme + "\t" + numberType;
            return result;
        }

        string isbool = IsBool(lexeme);
        if (isbool != "not bool")
        {
            return isbool;
        }

        string isString = IsString(lexeme);
        if (isString != "not string")
        {
            return isString;
        }

        string isFunc = IsFunc(lexeme);
        if (isFunc != "not function")
        {
            return isFunc;
        }

        if (char.IsDigit(lexeme[0]) && !int.TryParse(lexeme, out _))
        {
            return $"{lexeme}\tError: Variable name starts with a digit";
        }

        if (lexeme.StartsWith("print(") && lexeme.EndsWith(")"))
        {
            string argument = lexeme.Substring(6, lexeme.Length - 7).Trim();
            if (argument.Contains("(") && argument.Contains(")"))
            {
                // Вложенный вызов функции внутри print
                return $"print({argument})\tfunction name in print statement";
            }

            // В противном случае считаем это переменной
            return $"print({argument})\tvariable in print statement";
        }

        if (lexeme == "[")
        {
            return "left square bracket";
        }

        if (lexeme == "]")
        {
            return "right square bracket";
        }

        variables.Add(lexeme);
        return lexeme + "\tvariable";
    }


    public static async Task Main(string[] args)
    {
        Console.WriteLine("Lexeme\t\t        | Type");
        Console.WriteLine("------------------------|-------------------------------");

        try
        {
            using (StreamReader reader = new StreamReader(path))
            {
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    string[] lexemes = Tokenize(line);

                    foreach (string lexeme in lexemes)
                    {
                        if (string.IsNullOrWhiteSpace(lexeme) || lexeme == "," || lexeme == ":")
                            continue;  // Пропускаем пустые лексемы


                        GetFunctionName(line, lexeme);

                        if (lexeme == "def")
                        {
                            GetFunctionName(line, lexeme);
                        }

                        string lexemeType = GetLexemeType(lexeme);
                        PrintTable(lexeme, lexemeType);
                    }
                }
            }
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine($"File not found: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lexical error: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
        }
    }
}
