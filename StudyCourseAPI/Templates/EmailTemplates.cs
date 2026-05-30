namespace StudyCourseAPI.Services;

/// <summary>
/// Render HTML email theo layout dùng chung. Dùng table + inline CSS để tương thích
/// các email client (Gmail, Outlook, Apple Mail...).
/// </summary>
public static class EmailTemplates
{
    private const string BrandName = "EduHub";
    private const string BrandColor = "#4f46e5";     // indigo
    private const string BrandColorDark = "#4338ca";

    /// <summary>Email xác thực tài khoản.</summary>
    public static string Confirmation(string actionUrl, string? fullName = null)
        => Layout(
            preheader: "Xác thực email để kích hoạt tài khoản của bạn.",
            heading: "Chào mừng đến với EduHub! 🎉",
            bodyHtml: $"""
                <p style="margin:0 0 16px;">Xin chào {Escape(fullName) ?? "bạn"},</p>
                <p style="margin:0 0 16px;">Cảm ơn bạn đã đăng ký. Chỉ còn một bước nữa thôi —
                nhấn nút bên dưới để xác thực địa chỉ email và kích hoạt tài khoản.</p>
                """,
            buttonText: "Xác thực email",
            buttonUrl: actionUrl,
            footerNote: "Nếu bạn không tạo tài khoản này, hãy bỏ qua email."
        );

    /// <summary>Email đặt lại mật khẩu.</summary>
    public static string ResetPassword(string actionUrl, string? fullName = null)
        => Layout(
            preheader: "Yêu cầu đặt lại mật khẩu của bạn.",
            heading: "Đặt lại mật khẩu 🔑",
            bodyHtml: $"""
                <p style="margin:0 0 16px;">Xin chào {Escape(fullName) ?? "bạn"},</p>
                <p style="margin:0 0 16px;">Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn.
                Nhấn nút bên dưới để tạo mật khẩu mới. Liên kết sẽ hết hạn sau ít phút.</p>
                """,
            buttonText: "Đặt lại mật khẩu",
            buttonUrl: actionUrl,
            footerNote: "Nếu bạn không yêu cầu, hãy bỏ qua email — mật khẩu của bạn không thay đổi."
        );

    // ── layout dùng chung ────────────────────────────────────────────────
    private static string Layout(
        string preheader,
        string heading,
        string bodyHtml,
        string buttonText,
        string buttonUrl,
        string footerNote)
        => $$"""
        <!DOCTYPE html>
        <html lang="vi">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1.0">
          <meta name="x-apple-disable-message-reformatting">
          <title>{{BrandName}}</title>
        </head>
        <body style="margin:0;padding:0;background-color:#f3f4f6;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
          <!-- preheader ẩn -->
          <div style="display:none;max-height:0;overflow:hidden;opacity:0;">{{Escape(preheader)}}</div>

          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background-color:#f3f4f6;padding:24px 0;">
            <tr>
              <td align="center">
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:560px;background-color:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 1px 3px rgba(0,0,0,0.08);">
                  <!-- header -->
                  <tr>
                    <td style="background-color:{{BrandColor}};padding:24px 32px;">
                      <span style="color:#ffffff;font-size:20px;font-weight:700;letter-spacing:.5px;">{{BrandName}}</span>
                    </td>
                  </tr>
                  <!-- body -->
                  <tr>
                    <td style="padding:32px;">
                      <h1 style="margin:0 0 20px;font-size:22px;line-height:1.3;color:#111827;">{{heading}}</h1>
                      <div style="font-size:15px;line-height:1.6;color:#374151;">
                        {{bodyHtml}}
                      </div>
                      <!-- button -->
                      <table role="presentation" cellpadding="0" cellspacing="0" style="margin:28px 0;">
                        <tr>
                          <td align="center" bgcolor="{{BrandColorDark}}" style="border-radius:8px;">
                            <a href="{{buttonUrl}}" target="_blank"
                               style="display:inline-block;padding:14px 32px;font-size:15px;font-weight:600;color:#ffffff;text-decoration:none;border-radius:8px;background-color:{{BrandColorDark}};">
                              {{buttonText}}
                            </a>
                          </td>
                        </tr>
                      </table>
                    </td>
                  </tr>
                  <!-- footer -->
                  <tr>
                    <td style="padding:20px 32px;background-color:#f9fafb;border-top:1px solid #e5e7eb;">
                      <p style="margin:0 0 6px;font-size:12px;color:#9ca3af;">{{Escape(footerNote)}}</p>
                      <p style="margin:0;font-size:12px;color:#9ca3af;">© {{BrandName}}. Email tự động, vui lòng không trả lời.</p>
                    </td>
                  </tr>
                </table>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;

    private static string? Escape(string? s)
        => s is null ? null : s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
