using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace Server
{
    // -------------------------------------------------------
    // Data contract za osobu
    // -------------------------------------------------------
    [DataContract(Namespace = "http://kupidon")]
    public class PersonInfo
    {
        [DataMember] public string Username { get; set; }
        [DataMember] public string City { get; set; }
        [DataMember] public int Age { get; set; }
        [DataMember] public string Phone { get; set; }
    }

    // -------------------------------------------------------
    // Callback interfejs - server poziva klijenta
    // -------------------------------------------------------
    public interface IPersonCallback
    {
        // Salje pismo osobi: info o posiljalcu + poruka + da li se prikazuje telefon
        [OperationContract(IsOneWay = true)]
        void ReceiveLetter(PersonInfo sender, string message, bool showPhone);
    }

    // -------------------------------------------------------
    // Interfejs za osobe (Subscriber strana)
    // -------------------------------------------------------
    [ServiceContract(CallbackContract = typeof(IPersonCallback))]
    public interface IPersonService
    {
        [OperationContract]
        string InitSinglePerson(string username, string city, int age, string phone);

        // Klijent blokira drugog usera
        [OperationContract(IsOneWay = true)]
        void BlockUser(string requesterUsername, string usernameToBlock);

        // Klijent potvrdjuje da je primio pismo (otblokira prijem sledeceg)
        [OperationContract(IsOneWay = true)]
        void AcknowledgeLetter(string username);

        [OperationContract(IsOneWay = true)]
        void Unregister(string username);
    }

    // -------------------------------------------------------
    // Interfejs za kupidona (Publisher strana)
    // -------------------------------------------------------
    [ServiceContract]
    public interface ICupidService
    {
        // Kupidon poziva ovo da bi uputio pisma svim osobama
        [OperationContract]
        void SendLetters();

        // Lista trenutno prijavljenih (samo za prikaz)
        [OperationContract]
        List<PersonInfo> GetRegisteredPersons();
    }
}
