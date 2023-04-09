using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace UniversaLIS.Models
{
     [Table("OrderRequest")]
     public class OrderRequest : OrderBase
     {
          [JsonIgnore]
          private int patientID;
          private PatientBase patient;

          [JsonIgnore, ForeignKey(nameof(Patient))]
          public int PatientID { get => patientID; set => patientID = value; }
          [JsonIgnore]
          public PatientBase Patient { get => patient; init => patient = value; }
          [JsonIgnore, NotMapped]
          public new List<object>? Results { get; set; }

          public OrderRequest(PatientRequest patient)
          {
               SetOrderMessage("O||||||||||||||||||||||||||||||");
               this.patient = patient;
               patientID = patient.PatientID;
          }

          public OrderRequest()
          {
               SetOrderMessage("O||||||||||||||||||||||||||||||");
               this.patient = new PatientRequest();
          }

     }
}
