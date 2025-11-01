using Google.Cloud.Firestore;
using System;
using System.ComponentModel.DataAnnotations;

namespace INSY7315_ElevateDigitalStudios_POE.Models
{
    [FirestoreData]
    public class Notifications
    {
        [FirestoreProperty("uid")]
        public string? Uid { get; set; }

        [FirestoreProperty("message")]
        public string message { get; set; }

        [FirestoreProperty("timestamp")]
        public Timestamp timestamp { get; set; }

        public DateTime Date => timestamp.ToDateTime().ToLocalTime();
    }
}