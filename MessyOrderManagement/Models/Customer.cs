using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MessyOrderManagement.Models;

[Table("Customers")]
public class Customer
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("Name")]
    [MaxLength(200)]
    public string? Name { get; set; }

    [Column("Email")]
    [MaxLength(100)]
    public string? Email { get; set; }

    [Column("Phone")]
    [MaxLength(20)]
    public string? Phone { get; set; }

    [Column("Address")]
    [MaxLength(500)]
    public string? Address { get; set; }

    [Column("City")]
    [MaxLength(100)]
    public string? City { get; set; }

    [Column("State")]
    [MaxLength(50)]
    public string? State { get; set; }

    [Column("ZipCode")]
    [MaxLength(10)]
    public string? ZipCode { get; set; }

    [Column("CreatedDate")]
    public DateTime CreatedDate { get; set; }

    public static Customer New()
    {
        var x = new Customer();
        x.CreatedDate = DateTime.Now;
        return x;
    }

    public static string FormatPhone(string p)
    {
        if (p != null)
        {
            if (p.Length == 10)
            {
                return p;
            }
        }

        return "";
    }
}