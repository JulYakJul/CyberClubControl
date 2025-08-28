using System.ComponentModel.DataAnnotations;

namespace CybontrolX.DBModels
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Название товара обязательно для заполнения.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Цена закупки обязательно для заполнения.")]
        public int? PurchasePrice { get; set; }

        public int? SalePrice { get; set; }

        [Required(ErrorMessage = "Количество обязательно для заполнения.")]
        [Range(1, int.MaxValue, ErrorMessage = "Количество должно быть не менее 1.")]
        public int Quantity { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
