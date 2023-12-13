namespace Domain.Helpers
{
    public interface IResult<T>
    {
        bool IsSuccess { get; }

        IReadOnlyCollection<string> ErrorMessages { get; }
        public void AddErrorMessage(string errorMessage);

        T Value { get; set; }
    }
}
