using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading;

namespace PersonClient
{
    public class PersonCallbackFull : IPersonServiceCallback
    {
        private readonly string _username;
        private IPersonServiceChannel _proxy;

        public static volatile bool WaitingForConfirmation = false;

        public PersonCallbackFull(string username)
        {
            _username = username;
        }

        public void SetProxy(IPersonServiceChannel proxy)
        {
            _proxy = proxy;
        }

        public void ReceiveLetter(PersonInfo sender, string message, bool showPhone)
        {
            Console.WriteLine();
            Console.WriteLine("====================================");
            Console.WriteLine("       STIGLO TI JE PISMO!");
            Console.WriteLine("====================================");
            Console.WriteLine("  Od:      " + sender.Username);
            Console.WriteLine("  Grad:    " + sender.City);
            Console.WriteLine("  Godine:  " + sender.Age);

            if (showPhone)
                Console.WriteLine("  Telefon: " + sender.Phone);
            else
                Console.WriteLine("  Telefon: (skriveno - nije zainteresovan/a)");

            Console.WriteLine("  Poruka:  \"" + message + "\"");
            Console.WriteLine("====================================");
            Console.WriteLine("Pritisni Enter da potvrdis prijem pisma...");

            WaitingForConfirmation = true;

            while (WaitingForConfirmation)
            {
                Thread.Sleep(100);
            }

            _proxy.AcknowledgeLetter(_username);
            Console.WriteLine("Pismo potvrdjeno. Cekate sledece...");
        }
    }

    class Program
    {
        static IPersonServiceChannel _proxy;
        static string _username;

        static void Cleanup()
        {
            try
            {
                _proxy.Unregister(_username);
                ((IClientChannel)_proxy).Close();
            }
            catch { }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("=== Haoticni Kupidon - Prijava osobe ===");
            Console.WriteLine();

            _username = ReadNonEmpty("Unesite username: ");
            string city = ReadNonEmpty("Unesite grad: ");
            int age = ReadPositiveInt("Unesite godine: ");
            string phone = ReadNonEmpty("Unesite broj telefona: ");

            var binding = new WSDualHttpBinding();
            binding.SendTimeout = TimeSpan.FromMinutes(2);
            binding.ReceiveTimeout = TimeSpan.FromMinutes(2);
            binding.ReliableSession.InactivityTimeout = TimeSpan.FromMinutes(10);

            var endpoint = new EndpointAddress("http://localhost:8080/PersonService");

            var callbackFinal = new PersonCallbackFull(_username);
            var ctxFinal = new InstanceContext(callbackFinal);
            var factoryFinal = new DuplexChannelFactory<IPersonServiceChannel>(ctxFinal, binding, endpoint);
            _proxy = factoryFinal.CreateChannel();
            callbackFinal.SetProxy(_proxy);

            ((IClientChannel)_proxy).Open();

            string result = _proxy.InitSinglePerson(_username, city, age, phone);
            Console.WriteLine("[SERVIS] " + result);
            Console.WriteLine();
            Console.WriteLine("Ukucajte '/block username' da blokirate nekoga, ili sacekajte pismo.");
            Console.WriteLine("Za izlaz pritisnite Ctrl+C.");

            // Ctrl+C
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                Console.WriteLine("\n[KLIJENT] Odjavljujem se...");
                Cleanup();
                Environment.Exit(0);
            };

            // Zatvaranje X dugmeta / kill signala
            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                Cleanup();
            };

            try
            {
                while (true)
                {
                    string input = Console.ReadLine();
                    if (input == null) continue;

                    if (PersonCallbackFull.WaitingForConfirmation)
                    {
                        PersonCallbackFull.WaitingForConfirmation = false;
                        continue;
                    }

                    if (input.StartsWith("/block "))
                    {
                        string toBlock = input.Substring(7).Trim();
                        if (string.IsNullOrEmpty(toBlock))
                        {
                            Console.WriteLine("Greska: navedi username kojeg blokirate.");
                            continue;
                        }
                        _proxy.BlockUser(_username, toBlock);
                        Console.WriteLine("Korisnik '" + toBlock + "' je blokiran.");
                    }
                    else
                    {
                        Console.WriteLine("Nepoznata komanda. Koristite: /block username");
                    }
                }
            }
            finally
            {
                Cleanup();
            }
        }

        static string ReadNonEmpty(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                string val = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(val))
                    return val.Trim();
                Console.WriteLine("Greska: polje ne sme biti prazno.");
            }
        }

        static int ReadPositiveInt(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                string val = Console.ReadLine();
                if (int.TryParse(val, out int result) && result > 0)
                    return result;
                Console.WriteLine("Greska: unesite pozitivan ceo broj (bez slova i bez negativnih vrednosti).");
            }
        }
    }
}