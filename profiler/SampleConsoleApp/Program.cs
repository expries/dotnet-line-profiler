using System.Diagnostics;
using System.Text.Unicode;
using SampleConsoleApp;

var test = bool.Parse("true");
Console.WriteLine(test);
var activity = new Activity("SampleConsoleApp");
Activity.Current = activity;
activity.Start();

;

Console.WriteLine("TraceID Length: " +  System.Text.Encoding.Unicode.GetBytes(Activity.Current.TraceId.ToHexString()).Length);
Console.WriteLine("TraceID: " + Activity.Current.TraceId);


Test.CallTrace();

Test.Linear();
Test.If(-0);
Test.If(1);
Test.If(2);
Test.If(3);




Test.For(10);
Test.For(100);
Test.For(30);
Test.For(-1);

Test.IfElse(2);
Test.IfElse(-1);

Test.IfElseIfElse(4);
Test.IfElseIfElse(-1);
Test.IfElseIfElse(-11);

Test.Switch(0, 1);
Test.Switch(1, 2);
Test.Switch(2, 3);
