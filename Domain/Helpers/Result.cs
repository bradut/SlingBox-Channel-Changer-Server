namespace Domain.Helpers
{
    public class Result<T> : IResult<T>
    {
        public bool IsSuccess => !ErrorMessages.Any();

        private List<string> Messages { get; } = new();
        public IReadOnlyCollection<string> ErrorMessages =>  Messages.AsReadOnly();

        public void AddErrorMessage(string errorMessage)
        {
            Messages.Add(errorMessage);
        }

        public T Value { get; set; } = default!;
    }
    
}
