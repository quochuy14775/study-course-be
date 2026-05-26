namespace StudyCourseAPI.DTOs.Responses
{
    public class ODataResponse <T>
    {
        public decimal Count { get; set; }
        public IEnumerable<T>? Value { get; set; }
    }
}