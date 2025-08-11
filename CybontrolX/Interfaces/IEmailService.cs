namespace CybontrolX.Interfaces
{
    public interface IEmailService
    {
        Task SendConfirmationEmail(string email, string code);
    }
}
