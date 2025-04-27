using System.Diagnostics;
using SampleConsoleApp;



var test = bool.Parse("true");
Console.WriteLine(test);
var activity = new Activity("SampleConsoleApp");
Activity.Current = activity;
activity.Start();

/*
const string pdbPath = @"C:\Coding\profiler\SampleConsoleApp\bin\Debug\net8.0\SampleConsoleApp.pdb";
var functions = PdbParser.ParsePdbMOdule(pdbPath);

var json = JsonSerializer.Serialize(functions, new JsonSerializerOptions { WriteIndented = true });
Console.WriteLine(json);

Console.WriteLine("TraceID Length: " +  System.Text.Encoding.Unicode.GetBytes(Activity.Current.TraceId.ToHexString()).Length);
Console.WriteLine("TraceID: " + Activity.Current.TraceId);
*/

//Test.CallTrace();

//Test.Linear();
//Test.If(0);
//Test.If(1);

//Test.Switch(0, -1);

Test.If(0);

/*

Test.If(0);
Test.If(1);

Test.For(10);
*/

//Test.Switch(0, 1);
//Test.Switch(1, 2);
//Test.Switch(2, 3);

/*

Test.If(2);
Test.If(3);




Test.For(100);
Test.For(30);
Test.For(-1);

Test.IfElse(0);

Test.IfElse(-1);

Test.IfElseIfElse(4);
Test.IfElseIfElse(-1);
Test.IfElseIfElse(-11);

Test.Switch(0, 1);
Test.Switch(1, 2);

Test.Switch(2, 3);
*/
