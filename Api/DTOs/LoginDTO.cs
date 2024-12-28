namespace Api.DTOs
{
    public class LoginDTO
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class AppLoginDTO
    {
        public string AppKey { get; set; }
        public string AppSecret { get; set; }
    }
    
}
