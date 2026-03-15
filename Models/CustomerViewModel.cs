namespace PhoneStore.Models
{
    public class CustomerViewModel
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public decimal TotalSpent { get; set; }
        public int TotalOrders { get; set; }
        public string Rank { get; set; }
    }
}