using SampleConsoleApp;

var random = new Random().Next(0, 100);
var randomIsEven = random % 2 == 0;
Console.WriteLine("Random is even: " + randomIsEven);

Test.If(-0);
Test.If(1);
Test.If(2);
Test.If(3);
        
Test.For(10);

Test.IfElse(2);
Test.IfElse(-1);
        
Test.IfElseIfElse(4);
Test.IfElseIfElse(-1);
Test.IfElseIfElse(-11);
        
Test.Switch(0, 1);
Test.Switch(1, 2);
Test.Switch(2, 3);
