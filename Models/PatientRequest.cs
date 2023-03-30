using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniversaLIS.Models
{
     [Table("PatientRequest")]
     public class PatientRequest : Patient
     {
          private List<OrderRequest> orders = new List<OrderRequest>();
          public new List<OrderRequest> Orders { get => orders; set => orders = value; }
     }
}
