using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace PersonClient
{
    [DataContract(Namespace = "http://kupidon")]
    public class PersonInfo
    {
        [DataMember] public string Username { get; set; }
        [DataMember] public string City { get; set; }
        [DataMember] public int Age { get; set; }
        [DataMember] public string Phone { get; set; }
    }

    // Callback interfejs koji implementira klijent
    [ServiceContract]
    public interface IPersonServiceCallback
    {
        [OperationContract(IsOneWay = true)]
        void ReceiveLetter(PersonInfo sender, string message, bool showPhone);
    }

    // Servisni interfejs (isti kao na serveru)
    [ServiceContract(CallbackContract = typeof(IPersonServiceCallback))]
    public interface IPersonService
    {
        [OperationContract]
        string InitSinglePerson(string username, string city, int age, string phone);

        [OperationContract(IsOneWay = true)]
        void BlockUser(string requesterUsername, string usernameToBlock);

        [OperationContract(IsOneWay = true)]
        void AcknowledgeLetter(string username);

        [OperationContract(IsOneWay = true)]
        void Unregister(string username);
    }

    // Channel interfejs koji spaja servisni i communication object
    public interface IPersonServiceChannel : IPersonService, IClientChannel { }
}
