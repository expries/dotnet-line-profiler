using SampleConsoleApp;

//Console.Read();
Console.WriteLine("Hello, World!");



var random = new Random().Next(0, 100);
switch (random)
{
    case 1:
        Console.WriteLine("One");
        break;
    case 2: 
        Console.WriteLine("Two");
        break;
    case 3:
        if (random == 2) 
        {
            Console.WriteLine("Three");
        }
        else
        {
            Console.WriteLine("Not three");
        }
        break;
    default:
        var test = new Test(1);
        test.OneLocal();
        test.OneLocal();
        test.OneLocal();
        test.OneLocal();
        test.OneLocal();
        test.OneLocal();
        test.TwoLocals();
        test.ThreeLocals();
        test.X();
        break;
}

Console.WriteLine("Finished");
