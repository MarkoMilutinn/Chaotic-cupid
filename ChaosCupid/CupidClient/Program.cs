using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace CupidClient
{
    // Servisni ugovor za kupidona (isti kao na serveru)
    [ServiceContract]
    public interface ICupidService
    {
        [OperationContract]
        void SendLetters();

        [OperationContract]
        List<PersonInfo> GetRegisteredPersons();
    }

    [System.Runtime.Serialization.DataContract(Namespace = "http://kupidon")]
    public class PersonInfo
    {
        [System.Runtime.Serialization.DataMember] public string Username { get; set; }
        [System.Runtime.Serialization.DataMember] public string City { get; set; }
        [System.Runtime.Serialization.DataMember] public int Age { get; set; }
        [System.Runtime.Serialization.DataMember] public string Phone { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Haoticni Kupidon ===");
            Console.WriteLine("Kupidon ce slati pisma svakog minuta.");
            Console.WriteLine("Za pregled prijavljenih osoba pritisnite 'l', za izlaz Ctrl+C.");
            Console.WriteLine();

            var binding = new BasicHttpBinding();
            var endpoint = new EndpointAddress("http://localhost:8080/CupidService");
            var factory = new ChannelFactory<ICupidService>(binding, endpoint);
            var proxy = factory.CreateChannel();

            // Timer koji okida svakih 60 sekundi
            var timer = new System.Timers.Timer(60_000);
            timer.Elapsed += (sender, e) =>
            {
                Console.WriteLine($"[KUPIDON] {DateTime.Now:HH:mm:ss} - Saljem pisma...");
                try
                {
                    proxy.SendLetters();
                    Console.WriteLine("[KUPIDON] Pisma poslata.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[KUPIDON] Greska pri slanju: {ex.Message}");
                }
            };

            timer.Start();
            Console.WriteLine($"[KUPIDON] Timer pokrenut. Prvo slanje za ~60 sekundi.");

            // Rucno pokretanje odmah na pocetku
            Console.WriteLine("[KUPIDON] Saljemo prvo pismo odmah...");
            try
            {
                proxy.SendLetters();
                Console.WriteLine("[KUPIDON] Pisma poslata.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KUPIDON] Greska: {ex.Message}");
            }

            while (true)
            {
                string input = Console.ReadLine();
                if (input?.ToLower() == "l")
                {
                    try
                    {
                        var persons = proxy.GetRegisteredPersons();
                        Console.WriteLine($"\nTrenutno prijavljenih osoba: {persons.Count}");
                        foreach (var p in persons)
                            Console.WriteLine($"  - {p.Username} ({p.City}, {p.Age} god.)");
                        Console.WriteLine();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Greska: {ex.Message}");
                    }
                }
            }
        }
    }
}
