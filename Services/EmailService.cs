using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AspNetMvcApp.Models;
using Microsoft.Extensions.Configuration;

namespace AspNetMvcApp.Services
{
    public class EmailService
    {
        private readonly string _logPath;
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
            _logPath = @"c:\Users\ADMIN\Downloads\LapTrinhWeb-main\LapTrinhWeb-main\LapTrinhWeb\bin\emails.log";
        }

        public async Task SendOrderReceiptEmailAsync(Order order, List<CartItem> cartItems, string toEmail)
        {
            var directory = Path.GetDirectoryName(_logPath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Write plain-text log first
            var sb = new StringBuilder();
            sb.AppendLine("==========================================================================");
            sb.AppendLine($"HÓA ĐƠN ĐƠN HÀNG #{order.Id} - Shop Yonex");
            sb.AppendLine($"Gửi đến: {toEmail}");
            sb.AppendLine($"Thời gian: {order.OrderDate.ToLocalTime()}");
            sb.AppendLine($"Khách hàng: {order.CustomerName}");
            sb.AppendLine($"Số điện thoại: {order.PhoneNumber}");
            sb.AppendLine($"Địa chỉ nhận hàng: {order.ShippingAddress}");
            sb.AppendLine($"Phương thức thanh toán: {order.PaymentMethod}");
            sb.AppendLine("--------------------------------------------------------------------------");
            sb.AppendLine("Chi tiết sản phẩm:");
            foreach (var item in cartItems)
            {
                sb.AppendLine($"- {item.ProductName} x {item.Quantity} | Đơn giá: {item.Price:#,##0} đ | Thành tiền: {item.TotalPrice:#,##0} đ");
            }
            sb.AppendLine("--------------------------------------------------------------------------");
            if (!string.IsNullOrEmpty(order.CouponCode))
            {
                sb.AppendLine($"Mã giảm giá đã dùng: {order.CouponCode} (-{order.DiscountAmount:#,##0} đ)");
            }
            sb.AppendLine($"Phí vận chuyển: {(order.ShippingFee > 0 ? order.ShippingFee.ToString("#,##0") + " đ" : "Miễn phí")}");
            sb.AppendLine($"TỔNG CỘNG THANH TOÁN: {order.TotalAmount:#,##0} đ");
            sb.AppendLine("==========================================================================");
            sb.AppendLine();

            await File.AppendAllTextAsync(_logPath, sb.ToString(), Encoding.UTF8);

            // Generate HTML body for receipt email
            var paymentMethodName = order.PaymentMethod == "Momo" ? "Ví MoMo Sandbox" : (order.PaymentMethod == "BankQR" ? "QR Ngân hàng" : "Tiền mặt khi nhận hàng (COD)");
            
            var itemsHtml = new StringBuilder();
            foreach (var item in cartItems)
            {
                itemsHtml.Append($@"
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #dee2e6;'>{item.ProductName}</td>
                        <td style='padding: 10px; border-bottom: 1px solid #dee2e6; text-align: center;'>{item.Quantity}</td>
                        <td style='padding: 10px; border-bottom: 1px solid #dee2e6; text-align: right;'>{item.Price:#,##0} đ</td>
                        <td style='padding: 10px; border-bottom: 1px solid #dee2e6; text-align: right; font-weight: bold;'>{item.TotalPrice:#,##0} đ</td>
                    </tr>");
            }

            var discountRowHtml = "";
            if (order.DiscountAmount > 0)
            {
                discountRowHtml = $@"
                    <tr>
                        <td class='label' style='padding: 8px 0; border-bottom: 1px solid #f0f0f0; color: #888888;'>Giảm giá ({order.CouponCode})</td>
                        <td class='value' style='padding: 8px 0; border-bottom: 1px solid #f0f0f0; font-weight: bold; text-align: right; color: #a50064;'>-{order.DiscountAmount:#,##0} đ</td>
                    </tr>";
            }

            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{
            font-family: Arial, sans-serif;
            background-color: #f6f6f6;
            margin: 0;
            padding: 20px;
        }}
        .container {{
            max-width: 600px;
            margin: 0 auto;
            background-color: #ffffff;
            border: 1px solid #e9e9e9;
            border-radius: 12px;
            overflow: hidden;
            box-shadow: 0 4px 10px rgba(0,0,0,0.05);
        }}
        .header {{
            padding: 30px 20px;
            text-align: center;
            font-size: 24px;
            font-weight: bold;
            color: #005eb8;
            border-bottom: 1px solid #f0f0f0;
        }}
        .content {{
            padding: 30px;
            color: #333333;
            line-height: 1.6;
        }}
        .title {{
            font-size: 18px;
            font-weight: bold;
            margin-bottom: 20px;
            color: #222222;
        }}
        .info-table {{
            width: 100%;
            border-collapse: collapse;
            margin: 20px 0;
        }}
        .info-table td {{
            padding: 8px 0;
            border-bottom: 1px solid #f0f0f0;
        }}
        .info-table td.label {{
            color: #888888;
            width: 150px;
        }}
        .info-table td.value {{
            font-weight: bold;
            text-align: right;
        }}
        .items-table {{
            width: 100%;
            border-collapse: collapse;
            margin: 20px 0;
        }}
        .items-table th {{
            background-color: #f8f9fa;
            padding: 10px;
            text-align: left;
            font-size: 0.9rem;
            border-bottom: 2px solid #dee2e6;
        }}
        .footer {{
            padding: 20px 30px;
            background-color: #fafafa;
            border-top: 1px solid #f0f0f0;
            font-size: 12px;
            color: #888888;
            text-align: center;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            YONEX Shop
        </div>
        <div class='content'>
            <div class='title'>Xác nhận đặt hàng thành công #{order.Id}</div>
            <p>Xin chào <strong>{order.CustomerName}</strong>,</p>
            <p>Cảm ơn bạn đã đặt hàng tại Shop Yonex. Dưới đây là thông tin chi tiết hóa đơn đơn hàng của bạn:</p>

            <table class='info-table'>
                <tr>
                    <td class='label'>Mã đơn hàng</td>
                    <td class='value'>#{order.Id}</td>
                </tr>
                <tr>
                    <td class='label'>Thời gian đặt</td>
                    <td class='value'>{order.OrderDate.ToLocalTime()}</td>
                </tr>
                <tr>
                    <td class='label'>Số điện thoại</td>
                    <td class='value'>{order.PhoneNumber}</td>
                </tr>
                <tr>
                    <td class='label'>Địa chỉ nhận hàng</td>
                    <td class='value'>{order.ShippingAddress}</td>
                </tr>
                <tr>
                    <td class='label'>Hình thức thanh toán</td>
                    <td class='value'>{paymentMethodName}</td>
                </tr>
            </table>

            <div class='title' style='font-size: 16px; margin-top: 30px;'>Chi tiết sản phẩm</div>
            <table class='items-table'>
                <thead>
                    <tr>
                        <th style='background-color: #f8f9fa; padding: 10px; text-align: left; font-size: 0.9rem; border-bottom: 2px solid #dee2e6;'>Sản phẩm</th>
                        <th style='background-color: #f8f9fa; padding: 10px; text-align: center; font-size: 0.9rem; border-bottom: 2px solid #dee2e6;'>SL</th>
                        <th style='background-color: #f8f9fa; padding: 10px; text-align: right; font-size: 0.9rem; border-bottom: 2px solid #dee2e6;'>Giá</th>
                        <th style='background-color: #f8f9fa; padding: 10px; text-align: right; font-size: 0.9rem; border-bottom: 2px solid #dee2e6;'>Tổng</th>
                    </tr>
                </thead>
                <tbody>
                    {itemsHtml}
                </tbody>
            </table>

            <table class='info-table' style='margin-top: 20px; border-top: 2px solid #005eb8;'>
                {discountRowHtml}
                <tr>
                    <td class='label' style='padding: 8px 0; border-bottom: 1px solid #f0f0f0; color: #888888;'>Phí vận chuyển</td>
                    <td class='value' style='padding: 8px 0; border-bottom: 1px solid #f0f0f0; font-weight: bold; text-align: right;'>{(order.ShippingFee > 0 ? order.ShippingFee.ToString("#,##0") + " đ" : "Miễn phí")}</td>
                </tr>
                <tr>
                    <td class='label' style='padding: 8px 0; border-bottom: 1px solid #f0f0f0; color: #222222; font-weight: bold; font-size: 1.1rem;'>Tổng cộng</td>
                    <td class='value' style='padding: 8px 0; border-bottom: 1px solid #f0f0f0; font-weight: bold; text-align: right; font-size: 1.2rem; color: #00a651;'>{order.TotalAmount:#,##0} đ</td>
                </tr>
            </table>

            <p style='margin-top: 30px; text-align: center; font-weight: bold; color: #005eb8;'>Đơn hàng của bạn sẽ được chuẩn bị và xử lý trong thời gian sớm nhất!</p>
        </div>
        <div class='footer'>
            Đây là thư gửi tự động, vui lòng không trả lời email này.
        </div>
    </div>
</body>
</html>";

            if (!string.IsNullOrEmpty(toEmail))
            {
                await SendSmtpEmailAsync(toEmail, $"Hóa đơn đặt hàng thành công #{order.Id} - YONEX Shop", htmlBody, isHtml: true);
            }
        }

        public async Task SendPasswordResetEmailAsync(string email, string username, string resetLink)
        {
            var directory = Path.GetDirectoryName(_logPath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Create text log for development
            var sb = new StringBuilder();
            sb.AppendLine("==========================================================================");
            sb.AppendLine($"HỘP THƯ ĐẾN - Đặt lại mật khẩu - YONEX Shop");
            sb.AppendLine($"Người gửi: YONEX Shop Support <support@yonexshop.vn>");
            sb.AppendLine($"Đến: {email}");
            sb.AppendLine("--------------------------------------------------------------------------");
            sb.AppendLine("                             YONEX Shop");
            sb.AppendLine();
            sb.AppendLine("Yêu cầu đặt lại mật khẩu");
            sb.AppendLine();
            sb.AppendLine($"Xin chào {username},");
            sb.AppendLine();
            sb.AppendLine("Chúng tôi đã nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn.");
            sb.AppendLine("Vui lòng nhấn nút bên dưới để tiến hành thay đổi mật khẩu (Liên kết có hiệu lực trong vòng 1 giờ):");
            sb.AppendLine();
            sb.AppendLine($"              [ Đặt lại mật khẩu ] -> {resetLink}");
            sb.AppendLine();
            sb.AppendLine("Nếu nút trên không hoạt động, bạn có thể sao chép liên kết này và dán vào thanh địa chỉ trình duyệt:");
            sb.AppendLine(resetLink);
            sb.AppendLine();
            sb.AppendLine("Đây là thư gửi tự động, vui lòng không trả lời email này.");
            sb.AppendLine("==========================================================================");
            sb.AppendLine();

            await File.AppendAllTextAsync(_logPath, sb.ToString(), Encoding.UTF8);

            // Generate HTML email body matching the Thế Giới Di Động style
            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{
            font-family: Arial, sans-serif;
            background-color: #f6f6f6;
            margin: 0;
            padding: 20px;
        }}
        .container {{
            max-width: 600px;
            margin: 0 auto;
            background-color: #ffffff;
            border: 1px solid #e9e9e9;
            border-radius: 12px;
            overflow: hidden;
            box-shadow: 0 4px 10px rgba(0,0,0,0.05);
        }}
        .header {{
            padding: 30px 20px 20px;
            text-align: center;
            font-size: 24px;
            font-weight: bold;
            color: #005eb8;
            border-bottom: 1px solid #f0f0f0;
        }}
        .content {{
            padding: 30px;
            color: #333333;
            line-height: 1.6;
        }}
        .title {{
            font-size: 18px;
            font-weight: bold;
            margin-bottom: 20px;
            color: #222222;
        }}
        .button-wrapper {{
            text-align: center;
            margin: 30px 0;
        }}
        .button {{
            display: inline-block;
            background-color: #ffcc00;
            color: #000000 !important;
            text-decoration: none !important;
            font-weight: bold;
            padding: 12px 35px;
            border-radius: 8px;
            font-size: 16px;
            box-shadow: 0 4px 6px rgba(0,0,0,0.1);
        }}
        .button:hover {{
            background-color: #e6b800;
        }}
        .link-desc {{
            font-size: 14px;
            color: #666666;
            margin-top: 25px;
        }}
        .link-url {{
            font-size: 13px;
            color: #005eb8;
            word-break: break-all;
        }}
        .footer {{
            padding: 20px 30px;
            background-color: #fafafa;
            border-top: 1px solid #f0f0f0;
            font-size: 12px;
            color: #888888;
            text-align: center;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            YONEX Shop
        </div>
        <div class='content'>
            <div class='title'>Yêu cầu đặt lại mật khẩu</div>
            <p>Xin chào <strong>{username}</strong>,</p>
            <p>Chúng tôi đã nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn.</p>
            <p>Vui lòng nhấn nút bên dưới để tiến hành thay đổi mật khẩu (Liên kết có hiệu lực trong vòng 1 giờ):</p>
            
            <div class='button-wrapper'>
                <a href='{resetLink}' class='button'>Đặt lại mật khẩu</a>
            </div>

            <p class='link-desc'>Nếu nút trên không hoạt động, bạn có thể sao chép liên kết này và dán vào thanh địa chỉ trình duyệt:</p>
            <p class='link-url'><a href='{resetLink}'>{resetLink}</a></p>
        </div>
        <div class='footer'>
            Đây là thư gửi tự động, vui lòng không trả lời email này.
        </div>
    </div>
</body>
</html>";

            // Send actual email via SMTP
            await SendSmtpEmailAsync(email, "Đặt lại mật khẩu - YONEX Shop", htmlBody, isHtml: true);
        }

        private string GetStatusName(string status)
        {
            return status switch
            {
                "Pending" => "Chờ xác nhận",
                "Processing" => "Đang xử lý",
                "Shipped" => "Đang giao hàng",
                "Delivered" => "Đã giao hàng thành công",
                "Cancelled" => "Đã hủy",
                _ => status
            };
        }

        public async Task SendOrderStatusEmailAsync(string email, Order order)
        {
            var statusName = GetStatusName(order.Status);
            var subject = $"Cập nhật trạng thái đơn hàng #{order.Id} - YONEX Shop";

            // Log representation
            var directory = Path.GetDirectoryName(_logPath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var sbLog = new StringBuilder();
            sbLog.AppendLine("==========================================================================");
            sbLog.AppendLine($"HỘP THƯ ĐẾN - Cập nhật trạng thái đơn hàng #{order.Id} - YONEX Shop");
            sbLog.AppendLine($"Người gửi: YONEX Shop Support <support@yonexshop.vn>");
            sbLog.AppendLine($"Đến: {email}");
            sbLog.AppendLine("--------------------------------------------------------------------------");
            sbLog.AppendLine($"Trạng thái mới: {statusName}");
            sbLog.AppendLine($"Khách hàng: {order.CustomerName}");
            sbLog.AppendLine($"Địa chỉ giao hàng: {order.ShippingAddress}");
            sbLog.AppendLine($"Tổng tiền thanh toán: {order.TotalAmount:#,##0} đ");
            sbLog.AppendLine("==========================================================================");
            sbLog.AppendLine();

            await File.AppendAllTextAsync(_logPath, sbLog.ToString(), Encoding.UTF8);

            // Generate HTML body
            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{
            font-family: Arial, sans-serif;
            background-color: #f6f6f6;
            margin: 0;
            padding: 20px;
        }}
        .container {{
            max-width: 600px;
            margin: 0 auto;
            background-color: #ffffff;
            border: 1px solid #e9e9e9;
            border-radius: 12px;
            overflow: hidden;
            box-shadow: 0 4px 10px rgba(0,0,0,0.05);
        }}
        .header {{
            padding: 30px 20px;
            text-align: center;
            font-size: 24px;
            font-weight: bold;
            color: #005eb8;
            border-bottom: 1px solid #f0f0f0;
        }}
        .content {{
            padding: 30px;
            color: #333333;
            line-height: 1.6;
        }}
        .title {{
            font-size: 18px;
            font-weight: bold;
            margin-bottom: 20px;
            color: #222222;
        }}
        .status-badge {{
            display: inline-block;
            background-color: #005eb8;
            color: #ffffff;
            padding: 8px 16px;
            border-radius: 20px;
            font-weight: bold;
            margin: 15px 0;
        }}
        .info-table {{
            width: 100%;
            border-collapse: collapse;
            margin-top: 20px;
        }}
        .info-table td {{
            padding: 10px 0;
            border-bottom: 1px solid #f0f0f0;
        }}
        .info-table td.label {{
            color: #888888;
            width: 150px;
        }}
        .info-table td.value {{
            font-weight: bold;
        }}
        .footer {{
            padding: 20px 30px;
            background-color: #fafafa;
            border-top: 1px solid #f0f0f0;
            font-size: 12px;
            color: #888888;
            text-align: center;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            YONEX Shop
        </div>
        <div class='content'>
            <div class='title'>Cập nhật trạng thái đơn hàng #{order.Id}</div>
            <p>Xin chào <strong>{order.CustomerName}</strong>,</p>
            <p>Đơn hàng của bạn đã có cập nhật trạng thái mới:</p>
            
            <div style='text-align: center;'>
                <div class='status-badge'>{statusName}</div>
            </div>

            <table class='info-table'>
                <tr>
                    <td class='label'>Mã đơn hàng</td>
                    <td class='value'>#{order.Id}</td>
                </tr>
                <tr>
                    <td class='label'>Số điện thoại</td>
                    <td class='value'>{order.PhoneNumber}</td>
                </tr>
                <tr>
                    <td class='label'>Địa chỉ nhận hàng</td>
                    <td class='value'>{order.ShippingAddress}</td>
                </tr>
                <tr>
                    <td class='label'>Tổng tiền thanh toán</td>
                    <td class='value'>{order.TotalAmount:#,##0} đ</td>
                </tr>
            </table>

            <p style='margin-top: 25px;'>Cảm ơn bạn đã đồng hành và lựa chọn Shop Yonex!</p>
        </div>
        <div class='footer'>
            Đây là thư gửi tự động, vui lòng không trả lời email này.
        </div>
    </div>
</body>
</html>";

            await SendSmtpEmailAsync(email, subject, htmlBody, isHtml: true);
        }

        private async Task SendSmtpEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
        {
            try
            {
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var portStr = _configuration["EmailSettings:Port"];
                var senderEmail = _configuration["EmailSettings:SenderEmail"];
                var senderName = _configuration["EmailSettings:SenderName"];
                var username = _configuration["EmailSettings:Username"];
                var password = _configuration["EmailSettings:Password"];
                var enableSslStr = _configuration["EmailSettings:EnableSsl"];

                // Check if SMTP settings are placeholders or not set
                if (string.IsNullOrEmpty(smtpServer) || 
                    string.IsNullOrEmpty(senderEmail) || 
                    senderEmail == "your-email@gmail.com" || 
                    password == "your-app-password")
                {
                    return;
                }

                int port = int.TryParse(portStr, out var p) ? p : 587;
                bool enableSsl = !bool.TryParse(enableSslStr, out var ssl) || ssl;

                using (var mail = new System.Net.Mail.MailMessage())
                {
                    mail.From = new System.Net.Mail.MailAddress(senderEmail, senderName);
                    mail.To.Add(toEmail);
                    mail.Subject = subject;
                    mail.Body = body;
                    mail.IsBodyHtml = isHtml;

                    using (var smtp = new System.Net.Mail.SmtpClient(smtpServer, port))
                    {
                        smtp.UseDefaultCredentials = false;
                        smtp.Credentials = new System.Net.NetworkCredential(username, password);
                        smtp.EnableSsl = enableSsl;
                        await smtp.SendMailAsync(mail);
                    }
                }
            }
            catch (Exception ex)
            {
                await File.AppendAllTextAsync(_logPath, $"\n[SMTP ERROR] {DateTime.Now}: {ex.Message}\n", Encoding.UTF8);
            }
        }
    }
}
