using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomerService.ResourceAccess.Models
{
    public class Customer
    {
        [Column("CustomerId")]
        public long Id { get; set; }

        [Column("Name")]
        public string Name { get; set; } = String.Empty;
    }
}
