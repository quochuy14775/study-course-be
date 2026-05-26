namespace StudyCourseAPI.DTOs.Responses.Auth
{
    public class RegisterResponse
    {
        public string Message { get; set; }
    }

    public class LoginResponse
    {
        public string Token { get; set; }
    }

    public class RegisterMemberResponse
    {
        public string Message { get; set; }
    }

    public class ErrorResponse
    {
        public IEnumerable<string> Errors { get; set; }
    }
}