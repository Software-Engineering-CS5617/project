using Microsoft.Azure.Cosmos;
using Persistence;
using System.Security.Cryptography;
using System.Text;

public class Program
{
    public static async Task Main()
    {
        IStorage storage =
                await StorageFactory.CreateAsync(ModuleType.Chat);
        try
        {


            // test 1
            Console.WriteLine("Test 1 : Create, Read, Update, Delete");
            string key = "message-011";

            await storage.Create(key, Encoding.UTF8.GetBytes("Hello from Chat1"));
            Console.WriteLine("Item created.");

            Console.WriteLine($"Read: {Encoding.UTF8.GetString(await storage.Read(key))}");

            string etag = await storage.ReadEtag(key);
            await storage.Update(key, Encoding.UTF8.GetBytes("Updated message"), etag);
            Console.WriteLine("Item updated.");

            byte[] bytes = await storage.Read(key);
            Console.WriteLine(
                $"Read bytes after update: {string.Join(", ", bytes)}");

            Console.WriteLine(
                $"Read data after update: {Encoding.UTF8.GetString(await storage.Read(key))}");


            await storage.Delete(key);
            Console.WriteLine("Item deleted.");

            // test 2
            Console.WriteLine("\nTest 2 : Create, Read, Update, Delete");
            string key2 = "message-019";

            await storage.Create(key2, Encoding.UTF8.GetBytes("Hello from Chat2"));

            string etag2 = await storage.ReadEtag(key2);
            await storage.Update(key2, Encoding.UTF8.GetBytes("Updated message from Chat2"), etag2);
            byte[] data2 = await storage.Read(key2);
            Console.WriteLine($"Read data from key2: {Encoding.UTF8.GetString(data2)}");

            await storage.Delete(key2);


            // test 3: Simulate concurrent updates
            Console.WriteLine("\nTest 3 : Simulating concurrent updates");

            string key3 = "incident-025";
            await storage.Create(
                            key3,
                            Encoding.UTF8.GetBytes("Incident reported"));
            Console.WriteLine("Initial incident created.");

            // Simulate two users reading the same version.
            string etagB = await storage.ReadEtag(key3);
            string etagC = await storage.ReadEtag(key3);


            byte[] dataB = Encoding.UTF8.GetBytes("Update B");

            byte[] dataC = Encoding.UTF8.GetBytes("Update C");

            // Both users attempt to update concurrently.

            Task<bool> updateB = storage.Update(key3, dataB, etagB);
            Task<bool> updateC = storage.Update(key3, dataC, etagC);

            bool resultB = await updateB;
            bool resultC = await updateC;

            Console.WriteLine($"User B update: {resultB}");
            Console.WriteLine($"User C update: {resultC}");

            byte[] latestData = await storage.Read(key3);

            Console.WriteLine(
                $"Final incident data: {Encoding.UTF8.GetString(latestData)}");

            await storage.Delete(key3);
        }
       
        catch (CosmosException ex)
        {
            Console.WriteLine($"Cosmos error : {ex.Message}");
        }

    }
}




