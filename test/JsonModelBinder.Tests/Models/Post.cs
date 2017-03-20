namespace JsonModelBinder.Tests.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using Attributes;

    [Bind(nameof(Title), nameof(BlogId), nameof(Visibility))]
    public class Post
    {
        [Key]
        public int PostId { get; set; }

        [StringLength(15)]
        public string Title { get; set; }

        [Required]
        public int BlogId { get; set; }

        [Required]
        public Visibility Visibility { get; set; }

        [ForeignKey(nameof(BlogId))]
        public Blog Blog { get; set; }
    }
}
