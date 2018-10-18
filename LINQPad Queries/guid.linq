<Query Kind="Statements" />

Guid guid = Guid.NewGuid();
Console.WriteLine(guid.ToString());
Console.WriteLine(Convert.ToBase64String(guid.ToByteArray()));