using Nakov.IO;

const string NAME = "YetAnotherFTL";

var shouldExit = false;

while (!shouldExit)
{
    shouldExit = Process();
}

bool Process()
{
    var command = Cin.NextToken();

    if (command == "DOUDIZHUVER")
    {
        var arg1 = Cin.NextToken();
        if (arg1 == "1.0")
        {
            Console.WriteLine($"NAME {NAME}");
            return false;
        }
    }
    
    // Todo

    return false;
}

