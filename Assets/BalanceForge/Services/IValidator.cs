namespace BalanceForge.Services
{
    public interface IValidator
    {
        bool Validate(object value);
        string GetErrorMessage();
    }
}