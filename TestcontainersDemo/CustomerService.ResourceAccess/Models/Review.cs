using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace CustomerService.ResourceAccess.Models
{
    public class Review
    {
        [Column("ReviewId")]
        public long Id { get; set; }

        [Column("CustomerId")]
        public long CustomerId { get; set; }
        public Customer Customer { get; set; } = new();

        public ICollection<Comment> Comments { get; set; } = new List<Comment>();

        [Column("Rating")]
        public ReviewRating Rating { get; set; }
    }

    public enum ReviewRating
    {
        Poor,
        Fair,
        Good,
        VeryGood,
        Excellent
    }
}
