using System;
using System.Threading;
using PokemonGoApi.Source;

namespace PokemonGoApi
{
	public partial class Default : System.Web.UI.Page
	{

		protected void Page_Load(object sender, EventArgs e)
		{

		}

		protected void Login(string user, string pass, string location)
		{
			var program = new MainLoop();
			program.Login(user, pass, location);
			var initialResponse = program.SendInitialRequest();
			if (initialResponse != null)
			{
				Session["latitude"] = initialResponse.Latitude;
				Session["longitude"] = initialResponse.Longitude;
				UserLabel.Text = String.Format("Got user: {0}", initialResponse.Username);
				LocationLabel.Text = String.Format("Starting at location: {0} <br /> lat {1}, lng {2}", initialResponse.FormattedAddress, initialResponse.Latitude, initialResponse.Longitude);
				ResultsIFrame.Visible = true;
				ResultsIFrame.Style.Add("position", "absolute");
				ResultsIFrame.Style.Add("width", "100%");
				ResultsIFrame.Style.Add("height", "100%");
				ResultsIFrame.Style.Add("top", "250px");
				new Thread(() => program.BeginRequests()) { IsBackground = true }.Start();
				new Thread(() => program.BeginClearStalePokes()) { IsBackground = true }.Start();
			}
		}

		protected void GoogleLoginButton_OnClick(object sender, EventArgs e)
		{
			Login(GoogleUserTextBox.Text, GooglePassTextBox.Text, LocationTextBox.Text);
		}
	}
}