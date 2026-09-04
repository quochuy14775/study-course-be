using Microsoft.AspNetCore.Mvc;

namespace StudyCourseAPI.Extensions
{
    public static class BaseExtensions
    {
        public static IActionResult ValidationFailed(
            this ControllerBase controller,
            Dictionary<string, List<string>>? errors)
            => controller.BadRequest(new { status = 400, message = "Validation failed", errors = FlattenErrors(errors) });

        private static Dictionary<string, object> FlattenErrors(Dictionary<string, List<string>>? errors)
        {
            var result = new Dictionary<string, object>();
            if (errors == null) return result;
            foreach (var kv in errors)
            {
                if (kv.Value == null || kv.Value.Count == 0) continue;
                result[kv.Key] = kv.Value.Count == 1 ? kv.Value[0] : (object)kv.Value;
            }
            return result;
        }
    }
}
