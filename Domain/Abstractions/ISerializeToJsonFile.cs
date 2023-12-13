namespace Domain.Abstractions
{
    public interface ISerializeToJsonFile: ISerializeToJson
    {
        public string JsonFileName { get; }
    }
}
