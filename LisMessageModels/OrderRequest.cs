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
          private List<Result> results = new();

          [JsonIgnore, ForeignKey(nameof(Patient))]
          public int PatientID { get => patientID; set => patientID = value; }
          [JsonIgnore]
          public PatientBase Patient { get => patient; init => patient = value; }
          [JsonIgnore, NotMapped]
          public override List<Result> Results { get => results; set => results = value; }

          public OrderRequest(PatientRequest patient)
          {
               SetOrderMessage("O||||^^^||||||||||||||||||||||||||");
               this.patient = patient;
               patientID = patient.PatientID;
          }

          public OrderRequest()
          {
               SetOrderMessage("O||||^^^||||||||||||||||||||||||||");
               this.patient = new PatientRequest();
          }

     }
}
