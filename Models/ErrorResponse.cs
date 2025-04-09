namespace GameAPI.Models
{
    public class ErrorResponse
    {
        private string? _message;

        public string? Message
        {
            get => _message != null ? $"ERROR: {_message}" : null;
            set => _message = value;
        }
    }
}