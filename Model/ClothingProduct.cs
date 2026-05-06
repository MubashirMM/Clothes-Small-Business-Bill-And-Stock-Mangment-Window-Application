using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WpfApp2.Model
{
    public class ClothingProduct
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProductId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(20)]
        public string Size { get; set; }

        [MaxLength(30)]
        public string Color { get; set; }

        [MaxLength(50)]
        public string Material { get; set; }

        [MaxLength(50)]
        public string Brand { get; set; }

        [MaxLength(10)]
        public string Gender { get; set; } // Male, Female, Unisex

        [MaxLength(20)]
        public string Season { get; set; } // Summer, Winter, Spring, Fall

        [Required]
        [Range(0, 999999.99)]
        public decimal Price { get; set; }

        [Required]
        [Range(0, 999999)]
        public int StockQuantity { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }

        public ClothingProduct()
        {
            CreatedDate = DateTime.Now;
        }
    }
}