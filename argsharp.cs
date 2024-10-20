namespace Argsharp
{
    /// <summary>
    /// The exception class used by Argsharp
    /// </summary>
    public class ArgsharpException(string message) : Exception(message) { }

    /// <summary>
    /// The exception class used by the Parser class
    /// </summary>
    public class ArgsharpParseException(string message) : ArgsharpException(message) { }

    /// <summary>
    /// The exception class used by the Flag class
    /// </summary>
    public class ArgsharpFlagException(string message) : ArgsharpException(message) { }

    /// <summary>
    /// A command-line flag.
    /// </summary>
    public readonly struct Flag
    {
        private readonly string shorthand, sh, longhand, lh, description;
        /// <summary>
        /// </summary>
        public readonly bool required, storeTrue;

        /// <summary>
        /// A string used to get the flag's corrsponding ParseResult
        /// </summary>
        public string Mkey { get => sh + lh; }

        private static bool ValidFlagChar(char ch) => (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9') || ch == '-';

        private static void ValidateFlag(string flag)
        {
            if (flag.Length == 0 || flag == " ") return;

            int hyphens = 0;
            foreach (char ch in flag)
            {
                if (!ValidFlagChar(ch))
                    throw new ArgsharpFlagException("flags can only contain alphanumeric characters or hyphens('-')");
                else if (ch == '-')
                    hyphens++;
            }

            if (hyphens == flag.Length)
                throw new ArgsharpFlagException("flags cannot only contain hyphens");
            else if (flag.Length != 0)
            {
                if (flag[0] == '-')
                    throw new ArgsharpFlagException("flags cannot begin with a hyphen");
                else if (flag.Last() == '-')
                    throw new ArgsharpFlagException("flags cannot end with a hyphen");
            }
        }

        /// <summary>
        /// </summary>
        /// <exception cref="ArgsharpFlagException"></exception>
        public Flag(string shorthand, string longhand = "", string description = "", bool required = false, bool storeTrue = true)
        {
            if (shorthand == "" && longhand == "") throw new ArgsharpFlagException("the flag shorthand and longhand cannot both be empty");

            ValidateFlag(shorthand);
            sh = shorthand;
            this.shorthand = "-" + shorthand;
            ValidateFlag(longhand);
            lh = longhand;
            this.longhand = "--" + longhand;
            this.description = description;
            this.required = required;
            this.storeTrue = storeTrue;
        }

        /// <summary>
        /// The flag's help, shown in the message that's usually shown when the -h or --help flags are given
        /// </summary>
        public Tuple<string, string> Help()
        {
            string flag = shorthand == "-" ? "" : shorthand;
            if (flag != "") flag += "  ";
            flag += longhand;

            return new(flag, description);
        }

        /// <summary>
        /// The flag's usage, shown in the 'usage: app ...' message
        /// </summary>
        public string Usage()
        {
            string flag = shorthand == "-" ? "" : shorthand;
            if (flag != "") flag += "|";
            flag += longhand;
            if (!storeTrue) flag += " <value>";
            return required ? flag : "[" + flag + "]";
        }

        /// <summary>
        /// </summary>
        public string Rstr()
        {
            string flag = shorthand == "-" ? "" : shorthand;
            if (flag != "") flag += "/";
            flag += longhand;
            return flag;
        }

        /// <summary>
        /// </summary>
        public readonly override bool Equals(object? obj) => obj is string s && this == s;
        /// <summary>
        /// </summary>
        public readonly override int GetHashCode() => base.GetHashCode();

        /// <summary>
        /// </summary>
        public static bool operator ==(Flag f, string s) => f.shorthand == s || f.longhand == s;
        /// <summary>
        /// </summary>
        public static bool operator !=(Flag f, string s) => !(f == s);
    }

    /// <summary>
    /// Holds if the flag was found and the value of the flag if it has one.
    /// </summary>
    public readonly struct ParseResult(bool exists = false, string value = "")
    {
        /// <summary>
        /// true if this flag exists, false otherwise
        /// </summary>
        public readonly bool exists = exists;
        /// <summary>
        /// The value of the flag, empty if it doesn't exist, doesn't take an input, or the input given was empty
        /// </summary>
        public readonly string value = value;

        /// <summary>
        /// </summary>
        public readonly override bool Equals(object? obj) => obj is string s && this == s;
        /// <summary>
        /// </summary>
        public readonly override int GetHashCode() => base.GetHashCode();

        /// <summary>
        /// </summary>
        public static bool operator ==(ParseResult pr, string s) => pr.value == s;
        /// <summary>
        /// </summary>
        public static bool operator !=(ParseResult pr, string s) => !(pr == s);
    }

    /// <summary>
    /// A map of Flags to ParseResults
    /// </summary>
    public readonly struct ResultMap
    {
        private readonly Dictionary<string, ParseResult> map;

        internal ResultMap(Dictionary<string, ParseResult> map) => this.map = map;

        /// <summary>
        /// Trys to get the given Flag mkey and returns true if it's found, false otherwise
        /// </summary>
        /// <returns>True if the Flag mkey is found, false otherwise</returns>
        public bool TryGetFlag(string mkey, out string value)
        {
            if (map.TryGetValue(mkey, out var result))
            {
                value = result.value;
                return result.exists;
            }
            throw new ArgsharpException($"the mkey '{mkey}' does not exist; if this key is from a Flag instance, make an issue about it; otherwise, an invalid key was given");
        }

        /// <summary>
        /// Trys to get the given Flag
        /// </summary>
        /// <returns>True if the Flag is found, false otherwise</returns>
        public bool TryGetFlag(Flag flag, out string result) => TryGetFlag(flag.Mkey, out result);

        /// <summary>
        /// Checks if a flag exists
        /// </summary>
        /// <returns>True if the Flag is found, false otherwise</returns>
        public bool TryFlag(string mkey) => TryGetFlag(mkey, out var _);

        /// <summary>
        /// Checks if a flag exists
        /// </summary>
        /// <returns>True if the Flag is found, false otherwise</returns>
        public bool TryFlag(Flag flag) => TryGetFlag(flag, out var _);
    }

    /// <summary>
    /// The main class of Arg#, that takes in Flags and the argument source and outputs the results
    /// </summary>
    public class Parser
    {
        private readonly string name, description;
        private readonly string[] args;
        private readonly Flag[] flags;

        private static bool IsFlag(string f) => f.Length != 0 && f[0] == '-' && f.Length > 1;

        Tuple<bool, Flag> IsValidFlag(string f)
        {
            foreach (Flag fl in flags)
            {
                if (fl == f) return new(true, fl);
            }
            return new(false, new Flag("a"));
        }

        private void SortFlags() => flags.ToList().Sort((a, b) => !a.required && b.required ? -1 : a.required && !b.required ? 1 : 0);

        /// <summary>
        /// </summary>
        public Parser(string[] args, Flag[] flags, string name, string description = "")
        {
            this.args = args;
            this.flags = flags;
            SortFlags();
            this.name = name;
            this.description = description;
        }

        /// <summary>
        /// </summary>
        public Parser(List<string> args, Flag[] flags, string name, string description = "") : this(args.ToArray(), flags, name, description) { }

        /// <summary>
        /// Generates the help message that usually shows when `-h` or `--help` is given as a flag.
        /// </summary>
        public string Help()
        {
            string help = $"{Usage()}\n";

            if (description.Length != 0)
            {
                help += "\n";
                string s = $"usage: {name} ";
                foreach (char _ in s) help += " ";
                help += description;
            }

            if (description.Length != 0) help += "\n";
            help += "\nArguments:";

            List<Tuple<string, string>> flagHelp = [];
            int min = int.MaxValue;
            int max = 0;

            foreach (Flag f in flags)
            {
                var h = f.Help();
                if (h.Item1.Length > max) max = h.Item1.Length;
                if (h.Item1.Length < min) min = h.Item1.Length;
                flagHelp.Add(h);
            }

            string spacing = "  ";
            // for (int i = 0; i < max - min; i++) spacing += " ";

            foreach (var fh in flagHelp) help += "\n  " + fh.Item1 + spacing + fh.Item2;

            return help.Trim();
        }

        /// <summary>
        /// Generates the usage, e.g. `usage: app [-h|--help] [-s|--say &lt; value >]`.
        /// </summary>
        public string Usage()
        {
            string use = name;
            string fu = FlagUsage();
            if (fu.Length != 0) use += $" {fu}";
            return use;
        }

        /// <summary>
        /// Generates the flag part of the usage e.g. `[-h|--help] [-s|--say &lt; value >]`.
        /// </summary>
        public string FlagUsage()
        {
            string use = "";
            int c = 0;
            foreach (Flag f in flags)
            {
                use += use[^1] == '\n' ? f.Usage() : $" {f.Usage()}";
                c++;

                if (c == 3)
                {
                    c = 0;
                    use += "\n";
                }
            }

            use = use[1..];

            if (use[^1] == '\n') use = use[..^1];

            return use;
        }

        /// <summary>
        /// Returns a Tuple containing the flags and the leftover arguments that aren't flags and aren't matched to flags.
        /// </summary>
        /// <exception cref="ArgsharpParseException"></exception>
        public Tuple<ResultMap, string[]> Parse()
        {
            Dictionary<string, ParseResult> parsed = [];
            List<string> leftovers = [];

            for (int i = 0; i < args.Length; i++)
            {
                if (IsFlag(args[i]))
                {
                    Tuple<bool, Flag> valid = IsValidFlag(args[i]);
                    if (valid.Item1)
                    {
                        if (valid.Item2.storeTrue)
                            parsed.Add(valid.Item2.Mkey, new(true));
                        else
                        {
                            if (i + 1 >= args.Length) throw new ArgsharpParseException($"flag {valid.Item2.Rstr()} requires an arguement");

                            parsed.Add(valid.Item2.Mkey, new(true, args[i + 1]));
                            i++;
                        }
                    }
                    else
                        throw new ArgsharpParseException($"unknown flag '{args[i]}'");
                }
                else
                    leftovers.Add(args[i]);
            }

            foreach (Flag f in flags)
            {
                if (!parsed.ContainsKey(f.Mkey))
                {
                    if (f.required) throw new ArgsharpParseException($"required flag {f.Rstr()} was not given");
                    parsed.Add(f.Mkey, new());
                }
            }

            return new(new(parsed), [.. leftovers]);
        }

        /// <summary>
        /// Creates a new Parser instance with the current's values
        /// </summary>
        public Parser Copy(string[]? args = null, string? name = null, string? description = null) => new(args is not null ? args : this.args, flags, name is not null ? name : this.name, description is not null ? description : this.description);

        /// <summary>
        /// Creates a new Parser instance with the current's values
        /// </summary>
        public Parser Copy(List<string>? args = null, string? name = null, string? description = null) => Copy(args?.ToArray(), name, description);
    }
}
