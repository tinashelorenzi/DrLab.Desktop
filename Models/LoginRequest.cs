namespace DrLab.Desktop.Models
{
    public class LoginRequest
    {
        public string username { get; set; }
        public string password { get; set; }
        public string? device_fingerprint { get; set; }
        public string location { get; set; } = "Main Lab";
    }
}