using SampleConsoleApp;

Console.WriteLine("Hello, World!");

var random = new Random().Next(0, 100);
var randomIsEven = random % 2 == 0;
Console.WriteLine("Random is even: " + randomIsEven);


switch (random)
{
    case 1:
        Console.WriteLine("One");
        break;
    case 2: 
        Console.WriteLine("Two");
        break;
    case 3:
        Console.WriteLine("Not three");
        break;
    default:
        Test.If(-0);
        Test.If(1);
        Test.If(2);
        Test.If(3);

        Test.IfElse(2);
        Test.IfElse(-1);
        
        Test.IfElseIfElse(4);
        Test.IfElseIfElse(-1);
        Test.IfElseIfElse(-11);
        
        Test.Switch(0, 1);
        Test.Switch(1, 2);
        Test.Switch(2, 3);
        break;
}

Console.WriteLine("Finished");
