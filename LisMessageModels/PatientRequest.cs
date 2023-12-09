using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace UniversaLIS.Models
{
     [Table("PatientRequest")]
     public class PatientRequest : PatientBase
     {
          private List<OrderRequest> orders = new List<OrderRequest>();
          [JsonPropertyOrder(100), InverseProperty("Patient")]
          public new List<OrderRequest> Orders { get => orders; set => orders = value; }
     }
}
