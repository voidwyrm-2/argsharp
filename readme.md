# Arg#
An argument parsing library like my [Arg++](https://github.com/voidwyrm-2/argplusplus) library, but in C#

Example:
```cs
using Argsharp;

class Program
{
    public static void Main(string[] args)
    {
        Flag help = new("h", "help", "Lists all the commands");
        Flag echo = new("e", "echo", "Echoes back something", storeTrue: false);

        Parser parser = new(args, [help, echo], "example");

        var parsed = parser.Parse();
        var pflags = parsed.Item1;

        if (pflags.TryFlag(help))
        {
            Console.WriteLine(parser.Help());
            return;
        }
        else if (pflags.TryGetFlag(echo, out var msg))
        {
            Console.WriteLine(msg);
        }
    }
}
```