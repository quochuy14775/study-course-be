using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace StudyCourseAPI.Extensions;

public static class ServiceCollectionExtensions
{
    private const string ServicesNamespace = "StudyCourseAPI.Services";

    /// <summary>
    /// Tự đăng ký mọi cặp <c>IFooService</c> / <c>FooService</c> trong namespace
    /// <c>StudyCourseAPI.Services</c> với lifetime Scoped.
    ///
    /// QUAN TRỌNG: gọi hàm này SAU các đăng ký đặc biệt (Singleton, AddHttpClient...).
    /// Nó dùng TryAddScoped nên sẽ bỏ qua service nào đã được đăng ký trước đó — nhờ vậy
    /// JwtService/EmailService giữ nguyên Singleton và GroqService giữ nguyên HttpClient.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        var implementations = assembly.GetTypes()
            .Where(t => t.IsClass
                        && !t.IsAbstract
                        && !t.IsGenericTypeDefinition
                        && t.Namespace == ServicesNamespace
                        && t.Name.EndsWith("Service", StringComparison.Ordinal))
            .ToList();

        foreach (var implementation in implementations)
        {
            var contract = implementation.GetInterface($"I{implementation.Name}");
            if (contract is null) continue;

            services.TryAddScoped(contract, implementation);
        }

        VerifyEveryContractIsRegistered(assembly, services);

        return services;
    }

    /// <summary>
    /// Đổi tên class mà quên đổi tên interface (hoặc ngược lại) sẽ khiến convention không
    /// khớp và service âm thầm không được đăng ký — lỗi chỉ nổ khi có request đầu tiên gọi
    /// tới nó. Kiểm tra ngay lúc khởi động để lỗi đó nổ sớm, ở chỗ dễ thấy.
    /// </summary>
    private static void VerifyEveryContractIsRegistered(Assembly assembly, IServiceCollection services)
    {
        var registered = services.Select(d => d.ServiceType).ToHashSet();

        var missing = assembly.GetTypes()
            .Where(t => t.IsInterface
                        && t.Namespace == ServicesNamespace
                        && t.Name.EndsWith("Service", StringComparison.Ordinal)
                        && !registered.Contains(t))
            .Select(t => t.Name)
            .ToList();

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Các service sau không có implementation khớp quy ước tên (I{{Tên}} ↔ {{Tên}}) " +
                $"và chưa được đăng ký thủ công: {string.Join(", ", missing)}.");
        }
    }
}
