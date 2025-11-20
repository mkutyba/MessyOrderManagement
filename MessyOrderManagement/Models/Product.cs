using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MessyOrderManagement.Models;

[Table("Products")]
public class Product
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("Name")]
    [MaxLength(200)]
    public string? Name { get; set; }

    [Column("Price", TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [Column("Stock")]
    public int Stock { get; set; }

    [Column("Category")]
    [MaxLength(100)]
    public string? Category { get; set; }

    [Column("Description")]
    [MaxLength(1000)]
    public string? Description { get; set; }

    [Column("IsActive")]
    public bool IsActive { get; set; }

    [Column("LastUpdated")]
    public DateTime LastUpdated { get; set; }

    public static Product GetDefault()
    {
        var temp = new Product();
        temp.IsActive = true;
        temp.LastUpdated = DateTime.Now;
        return temp;
    }

    public static decimal CalculatePrice(decimal p, int q)
    {
        if (q > 10)
        {
            if (p > 50)
            {
                return p * 0.9m;
            }
        }

        return p;
    }
}