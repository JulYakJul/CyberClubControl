using CybontrolX.DBModels;

public class Payment
{
    public int Id { get; set; }
    public string PaymentYooKassaId { get; set; } // ID платежа в системе YooKassa
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "RUB";
    public string Status { get; set; } = "Success";
    public string Description { get; set; }
    public DateTime PaymentDateTime { get; set; } = DateTime.UtcNow;
    public string PaymentMethod { get; set; } 

    public int? ClientId { get; set; }
    public Client Client { get; set; }

    public int? EmployeeId { get; set; }
    public Employee Employee { get; set; }

    public int? ProductId { get; set; }
    public Product Product { get; set; }
    public int? ProductQuantity { get; set; }

    public int? SessionId { get; set; }
    public Session Session { get; set; }
}