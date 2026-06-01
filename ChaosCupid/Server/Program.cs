using System;
using System.ServiceModel;
using System.ServiceModel.Description;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            // Jedna servisna instanca za oba ugovora
            var service = new KupidonService();
            var host = new ServiceHost(service, new Uri("http://localhost:8080"));

            host.Description.Behaviors.Add(new ServiceMetadataBehavior { HttpGetEnabled = true });

            // Endpoint za osobe (duplex - wsDualHttpBinding zbog callbacka)
            var personBinding = new WSDualHttpBinding();
            personBinding.SendTimeout = TimeSpan.FromMinutes(2);
            personBinding.ReceiveTimeout = TimeSpan.FromMinutes(2);
            personBinding.ReliableSession.InactivityTimeout = TimeSpan.FromMinutes(10);

            host.AddServiceEndpoint(
                typeof(IPersonService),
                personBinding,
                "PersonService"
            );


            // Endpoint za kupidona (obican basicHttp, ne treba callback)
            host.AddServiceEndpoint(
                typeof(ICupidService),
                new BasicHttpBinding(),
                "CupidService"
            );
            host.AddServiceEndpoint(
                typeof(ICupidService),
                MetadataExchangeBindings.CreateMexHttpBinding(),
                "CupidService/Mex"
            );

            host.Open();
            Console.WriteLine("[SERVER] Kupidon servis je pokrenut.");
            Console.WriteLine("[SERVER] PersonService  -> http://localhost:8080/PersonService");
            Console.WriteLine("[SERVER] CupidService   -> http://localhost:8080/CupidService");
            Console.WriteLine("[SERVER] Pritisnite Enter za zaustavljanje...");
            Console.ReadLine();
            host.Close();
        }
    }
}
