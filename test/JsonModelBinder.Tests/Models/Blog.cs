namespace JsonModelBinder.Tests.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using Attributes;

    [Bind(nameof(Name), nameof(NumberofSubscribers), nameof(CreatedOn), nameof(Image), nameof(TestObject), nameof(Posts))]
    public class Blog
    {
        [Key]
        public int BlogId { get; set; }
        [StringLength(15)]
        public string Name { get; set; }
        [Range(10, 25)]
        public short NumberofSubscribers { get; set; }
        public DateTime CreatedOn { get; set; }
        
        public byte[] Image { get; set; }

        [Required]
        public TestObject TestObject { get; set; }

        [InverseProperty(nameof(Post.Blog))]
        public ICollection<Post> Posts { get; set; } = new HashSet<Post>();
    }
}