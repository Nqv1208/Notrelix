namespace TodoApp.Application.Common.Email;

public static class EmailTemplateService
{
    public static string ForgotPasswordOtp(string userName, string otpCode)
    {
        return $"""
        <!DOCTYPE html>
        <html>
        <head>
          <meta charset="utf-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1" />
        </head>
        <body style="margin:0;padding:0;background:#f4f4f5;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif">
          <table width="100%" cellpadding="0" cellspacing="0" style="padding:40px 20px">
            <tr>
              <td align="center">
                <table width="480" cellpadding="0" cellspacing="0" style="background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 1px 3px rgba(0,0,0,0.08)">
                  <tr>
                    <td style="background:linear-gradient(135deg,#7c3aed,#4f46e5);padding:32px 40px;text-align:center">
                      <h1 style="margin:0;color:#fff;font-size:24px;font-weight:700;letter-spacing:-0.5px">Notrelix</h1>
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:40px">
                      <p style="margin:0 0 8px;font-size:16px;color:#18181b">Hi {userName},</p>
                      <p style="margin:0 0 24px;font-size:14px;color:#71717a;line-height:1.6">
                        We received a request to reset your password. Use the code below to proceed. This code expires in <strong>10 minutes</strong>.
                      </p>
                      <div style="background:#f4f4f5;border-radius:8px;padding:20px;text-align:center;margin:0 0 24px">
                        <span style="font-size:32px;font-weight:700;letter-spacing:8px;color:#18181b;font-family:monospace">{otpCode}</span>
                      </div>
                      <p style="margin:0 0 4px;font-size:13px;color:#a1a1aa">
                        If you didn't request a password reset, you can safely ignore this email.
                      </p>
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:20px 40px;border-top:1px solid #f4f4f5;text-align:center">
                      <p style="margin:0;font-size:12px;color:#a1a1aa">&copy; {DateTime.UtcNow.Year} Notrelix, Inc.</p>
                    </td>
                  </tr>
                </table>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;
    }

    public static string PasswordChanged(string userName)
    {
        return $"""
        <!DOCTYPE html>
        <html>
        <head>
          <meta charset="utf-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1" />
        </head>
        <body style="margin:0;padding:0;background:#f4f4f5;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif">
          <table width="100%" cellpadding="0" cellspacing="0" style="padding:40px 20px">
            <tr>
              <td align="center">
                <table width="480" cellpadding="0" cellspacing="0" style="background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 1px 3px rgba(0,0,0,0.08)">
                  <tr>
                    <td style="background:linear-gradient(135deg,#7c3aed,#4f46e5);padding:32px 40px;text-align:center">
                      <h1 style="margin:0;color:#fff;font-size:24px;font-weight:700;letter-spacing:-0.5px">Notrelix</h1>
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:40px">
                      <p style="margin:0 0 8px;font-size:16px;color:#18181b">Hi {userName},</p>
                      <p style="margin:0 0 24px;font-size:14px;color:#71717a;line-height:1.6">
                        Your password has been successfully changed. If you did not make this change, please contact our support team immediately.
                      </p>
                      <p style="margin:0;font-size:14px;color:#71717a;line-height:1.6">
                        All active sessions have been revoked for security. Please sign in again with your new password.
                      </p>
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:20px 40px;border-top:1px solid #f4f4f5;text-align:center">
                      <p style="margin:0;font-size:12px;color:#a1a1aa">&copy; {DateTime.UtcNow.Year} Notrelix, Inc.</p>
                    </td>
                  </tr>
                </table>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;
    }
}
