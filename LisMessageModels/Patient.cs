using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

[assembly: InternalsVisibleTo("REST-LIS")]

namespace UniversaLIS.Models
{
     [Table("PatientRecord")]
     public class Patient : PatientBase
     {
          private List<Order> orders = new();
          // Comments can be added at any level. 
          // TODO: Support comments.
          private List<Comment> comments = new();

          [JsonPropertyOrder(100)]
          public new List<Order> Orders { get => orders; set => orders = value; }

          public Patient(string patientMessage)
          {
               SetPatientString(patientMessage);
          }

          public Patient()
          {
               SetPatientString("|1|||||||||||||||||||||||||||||||||");
          }

     }

}
