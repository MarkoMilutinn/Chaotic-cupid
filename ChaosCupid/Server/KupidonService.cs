using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.ServiceModel;

namespace Server
{
    internal class RegisteredPerson
    {
        public PersonInfo Info { get; set; }
        public IPersonCallback Callback { get; set; }
        public HashSet<string> BlockedUsers { get; set; } = new HashSet<string>();
        public bool WaitingForAck { get; set; } = false;
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class KupidonService : IPersonService, ICupidService
    {
        private static readonly object _lock = new object();
        private static readonly Dictionary<string, RegisteredPerson> _persons = new Dictionary<string, RegisteredPerson>();

        private static readonly string[] _messages = new[]
        {
            "Radujem se nasem susretu!",
            "Zelim da se upoznamo.",
            "Nisam zainteresovan/a za upoznavanje."
        };

        // -------------------------------------------------------
        // IPersonService implementacija
        // -------------------------------------------------------

        public string InitSinglePerson(string username, string city, int age, string phone)
        {
            lock (_lock)
            {
                if (_persons.ContainsKey(username))
                    return $"Korisnik '{username}' je vec prijavljen.";

                var callback = OperationContext.Current.GetCallbackChannel<IPersonCallback>();

                _persons[username] = new RegisteredPerson
                {
                    Info = new PersonInfo
                    {
                        Username = username,
                        City = city,
                        Age = age,
                        Phone = phone
                    },
                    Callback = callback
                };

                Console.WriteLine($"[SERVER] Prijavljena osoba: {username}, {city}, {age} god.");
                return $"Uspesno prijavljen/a kao '{username}'. Cekaj pismo od kupidona!";
            }
        }

        public void BlockUser(string requesterUsername, string usernameToBlock)
        {
            lock (_lock)
            {
                if (_persons.TryGetValue(requesterUsername, out var person))
                {
                    person.BlockedUsers.Add(usernameToBlock);
                    Console.WriteLine($"[SERVER] {requesterUsername} je blokirao/la {usernameToBlock}");
                }
            }
        }

        public void AcknowledgeLetter(string username)
        {
            lock (_lock)
            {
                if (_persons.TryGetValue(username, out var person))
                {
                    person.WaitingForAck = false;
                    Console.WriteLine($"[SERVER] {username} je potvrdio/la prijem pisma.");
                }
            }
        }

        public void Unregister(string username)
        {
            lock (_lock)
            {
                if (_persons.Remove(username))
                    Console.WriteLine($"[SERVER] {username} se odjavio/la.");
            }
        }

        // -------------------------------------------------------
        // ICupidService implementacija
        // -------------------------------------------------------

        public void SendLetters()
        {
            List<RegisteredPerson> snapshot;
            lock (_lock)
            {
                snapshot = _persons.Values.ToList();
            }

            Console.WriteLine($"[SERVER] Kupidon salje pisma... ({snapshot.Count} osoba prijavljeno)");

            var toRemove = new List<string>();

            foreach (var recipient in snapshot)
            {
                var commObj = recipient.Callback as ICommunicationObject;
                if (commObj != null && commObj.State != CommunicationState.Opened)
                {
                    Console.WriteLine($"[SERVER] Kanal za {recipient.Info.Username} je zatvoren, brisem.");
                    toRemove.Add(recipient.Info.Username);
                    continue;
                }

                if (recipient.WaitingForAck)
                {
                    Console.WriteLine($"[SERVER] {recipient.Info.Username} jos nije potvrdio prethodno pismo, preskacemo.");
                    continue;
                }

                var best = FindBestMatch(recipient, snapshot);
                if (best == null)
                {
                    Console.WriteLine($"[SERVER] Nema dostupnog posiljaoca za {recipient.Info.Username}");
                    continue;
                }

                string chosenMessage = PickRandomMessage();
                bool showPhone = chosenMessage != "Nisam zainteresovan/a za upoznavanje.";

                Console.WriteLine($"[SERVER] Saljemo pismo: {best.Info.Username} -> {recipient.Info.Username} | \"{chosenMessage}\"");

                lock (_lock)
                {
                    recipient.WaitingForAck = true;
                }

                try
                {
                    recipient.Callback.ReceiveLetter(best.Info, chosenMessage, showPhone);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SERVER] Greska pri slanju pisma {recipient.Info.Username}: {ex.Message}");
                    toRemove.Add(recipient.Info.Username);
                }
            }

            if (toRemove.Count > 0)
            {
                lock (_lock)
                {
                    foreach (var name in toRemove)
                        _persons.Remove(name);
                }
            }
        }

        public List<PersonInfo> GetRegisteredPersons()
        {
            lock (_lock)
            {
                return _persons.Values.Select(p => p.Info).ToList();
            }
        }

        // -------------------------------------------------------
        // Pomocne metode
        // -------------------------------------------------------

        private RegisteredPerson FindBestMatch(RegisteredPerson recipient, List<RegisteredPerson> allPersons)
        {
            RegisteredPerson bestPerson = null;
            int bestScore = -1;

            foreach (var candidate in allPersons)
            {
                if (candidate.Info.Username == recipient.Info.Username)
                    continue;

                if (recipient.BlockedUsers.Contains(candidate.Info.Username))
                    continue;

                int score = CalculateScore(recipient.Info, candidate.Info);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestPerson = candidate;
                }
            }

            return bestPerson;
        }

        private int CalculateScore(PersonInfo recipient, PersonInfo sender)
        {
            int score = 0;

            if (string.Equals(recipient.City, sender.City, StringComparison.OrdinalIgnoreCase))
                score += 30;

            if (Math.Abs(recipient.Age - sender.Age) <= 2)
                score += 20;

            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] randomBytes = new byte[4];
                rng.GetBytes(randomBytes);
                int randomValue = Math.Abs(BitConverter.ToInt32(randomBytes, 0)) % 101;
                score += randomValue;
            }

            return score;
        }

        private string PickRandomMessage()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] randomBytes = new byte[4];
                rng.GetBytes(randomBytes);
                int index = Math.Abs(BitConverter.ToInt32(randomBytes, 0)) % _messages.Length;
                return _messages[index];
            }
        }
    }
}