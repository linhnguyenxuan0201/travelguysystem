using TripCompass.Infrastructure.Security;

var hasher = new EfPasswordHasher();

Console.WriteLine("admin:   " + hasher.Hash("123456"));
Console.WriteLine("user:    " + hasher.Hash("123456"));
Console.WriteLine("partner: " + hasher.Hash("123456"));

Console.ReadLine();
