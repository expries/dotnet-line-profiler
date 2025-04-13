using SampleConsoleApp;

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
        
        test.If(-0);
        test.If(1);
        test.If(2);
        test.If(3);
        
        
        
        test.IfElse(2);
        test.IfElse(-1);
        
        
        
        
        test.IfElseIfElse(4);
        test.IfElseIfElse(-1);
        test.IfElseIfElse(-11);


        test.Switch(0, 1);
        test.Switch(1, 2);
        test.Switch(2, 3);

/*
test.OneLocal();
test.OneLocal();
test.OneLocal();
test.OneLocal();
test.OneLocal();
test.OneLocal();
test.TwoLocals();
test.ThreeLocals();
test.X();
*/
        break;
}

Console.WriteLine("Finished");
