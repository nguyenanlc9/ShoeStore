namespace ShoeStore.Models.DTO.Request
{
    public class AdminLoginDTO
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool RememberMe { get; set; }
    }
} 