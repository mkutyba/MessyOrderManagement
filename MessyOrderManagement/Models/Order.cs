using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MessyOrderManagement.Models;

[Table("Orders")]
public class Order
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("CustomerId")]
    public int CustomerId { get; set; }

    [Column("ProductId")]
    public int ProductId { get; set; }

    [Column("Quantity")]
    public int Quantity { get; set; }

    [Column("Price", TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [Column("Status")]
    [MaxLength(50)]
    public string? Status { get; set; }

    [Column("Date")]
    public DateTime Date { get; set; }

    [Column("Notes")]
    [MaxLength(1000)]
    public string? Notes { get; set; }

    [Column("Total", TypeName = "decimal(18,2)")]
    public decimal Total { get; set; }

    public static Order Create()
    {
        var a = new Order();
        a.Date = DateTime.Now;
        return a;
    }

    public static bool IsValid(Order o)
    {
        if (o != null)
        {
            if (o.Quantity > 0)
            {
                return true;
            }
        }

        return false;
    }
}