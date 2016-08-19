using System.Net;
using System.Net.Mail;

namespace PokemonGoApi.Source.Helpers
{
	public static class GmailHelper
	{
		public static void SendEmail(string subject, string body)
		{
			try
			{
				MailAddress fromAddress = new MailAddress(Constants.MailSender, "POGO Alert");
				MailAddress toAddress = new MailAddress("9522509870@vtext.com", "Scoot Goomdan");
				string fromPassword = Constants.MailPass;

				var smtp = new SmtpClient
				{
					Host = "smtp.gmail.com",
					Port = 587,
					EnableSsl = true,
					DeliveryMethod = SmtpDeliveryMethod.Network,
					UseDefaultCredentials = false,
					Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
				};
				using (var message = new MailMessage(fromAddress, toAddress)
				{
					Subject = subject,
					Body = body
				})
				{
					smtp.Send(message);
				}
			}
			catch
			{

			}
		}
	}
}