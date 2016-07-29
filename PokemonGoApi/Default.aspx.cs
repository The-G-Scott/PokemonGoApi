using System;
using System.Threading;
using PokemonGoApi.Source;

namespace PokemonGoApi
{
	public partial class Default : System.Web.UI.Page
	{
		private string m_user;
		private string m_pass;
		private string m_location;
		MainLoop program;

		protected void Page_Load(object sender, EventArgs e)
		{

		}

		protected void Begin()
		{
			Thread clearPokesThread = new Thread(() => program.BeginClearStalePokes()) { IsBackground = true };
			Thread mainRequestThread = new Thread(() => program.BeginRequests()) { IsBackground = true };
			clearPokesThread.Start();
			mainRequestThread.Start();
			while (mainRequestThread.IsAlive) { }
			clearPokesThread.Abort();
			program = new MainLoop();
			program.Login(m_user, m_pass, m_location);
			program.SendInitialRequest();
			Begin();
		}

		protected void Login(string user, string pass, string location)
		{
			m_user = user;
			m_pass = pass;
			m_location = location;

			program = new MainLoop();
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
				ResultsIFrame.Style.Add("width", "calc(100% - 50px)");
				ResultsIFrame.Style.Add("height", "100%");
				ResultsIFrame.Style.Add("top", "190px");

				new Thread(() => Begin()) { IsBackground = true }.Start();
			}
		}

		protected void GoogleLoginButton_OnClick(object sender, EventArgs e)
		{
			Login(GoogleUserTextBox.Text, GooglePassTextBox.Text, LocationTextBox.Text);
		}
	}
}