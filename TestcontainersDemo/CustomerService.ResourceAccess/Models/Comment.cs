using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomerService.ResourceAccess.Models
{
    public class Comment
    {
        [Column("CommentId")]
        public long Id { get; set; }

        [Column("ReviewId")]
        public long ReviewId { get; set; }
        public Review Review { get; set; } = new();

        [Column("CommentText")]
        public string CommentText { get; set; } = String.Empty;

        [Column("CreatedDate")]
        public DateTime CreatedDate { get; set; }

        [Column("CreatedBy")]
        public string CreatedBy { get; set; } = String.Empty;
    }
}
