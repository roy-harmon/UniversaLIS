using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

[assembly: InternalsVisibleTo("REST-LIS")]

namespace UniversaLIS.Models
{
     [Table("PatientRecord")]
     public class Patient : PatientBase
     {
          private List<Order> orders = new List<Order>();
          private List<Comment> comments = new List<Comment>();

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
