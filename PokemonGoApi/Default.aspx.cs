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
		private int m_stepCount;
		MainLoop program;

		protected void Page_Load(object sender, EventArgs e)
		{
			StepCountTextBox.Width = 30;
		}

		protected void Begin()
		{
			var worker = DBWorker.GetByUsername(m_user);
			if (worker != null && worker.IsAvailable)
			{
				worker.SetBusy();
				Thread clearPokesThread = new Thread(() => program.BeginClearStalePokes()) { IsBackground = true };
				Thread mainRequestThread = new Thread(() => program.BeginRequests()) { IsBackground = true };
				clearPokesThread.Start();
				mainRequestThread.Start();
				while (mainRequestThread.IsAlive) { }
				clearPokesThread.Abort();
				program = new MainLoop();
				program.Login(m_user, m_pass, m_location, m_stepCount);
				program.SendInitialRequest();
				Begin();
			}
			else
			{
				UserLabel.Text = "Worker unavailable. Try again later.";
				LocationLabel.Visible = false;
			}
		}

		protected void Login(string user, string pass, string location, int stepCount)
		{
			m_user = user;
			m_pass = pass;
			m_location = location;
			m_stepCount = stepCount;

			program = new MainLoop();
			program.Login(user, pass, location, stepCount);
			var initialResponse = program.SendInitialRequest();
			if (initialResponse != null)
			{
				program.SendSecondaryRequests();

				Session["latitude"] = initialResponse.Latitude;
				Session["longitude"] = initialResponse.Longitude;
				Session.Timeout = 1440;
				UserLabel.Text = String.Format("Scanning as user: {0}", initialResponse.Username);
				LocationLabel.Text = String.Format("Starting at location: {0} - lat {1}, lng {2}", initialResponse.FormattedAddress, initialResponse.Latitude, initialResponse.Longitude);
				LocationTextBox.Visible = false;
				LocationTitleLabel.Visible = false;
				StartButton.Visible = false;
				StepCountLabel.Visible = false;
				StepCountTextBox.Visible = false;
				ResultsIFrame.Visible = true;
				ResultsIFrame.Style.Add("position", "absolute");
				ResultsIFrame.Style.Add("width", "calc(100% - 25px)");
				ResultsIFrame.Style.Add("height", "100%");
				ResultsIFrame.Style.Add("top", "45px");
				var worker = DBWorker.GetByUsername(m_user);
				worker.SetAvailable();
			}
			new Thread(() => Begin()) { IsBackground = true }.Start();
		}

		protected void StartButton_OnClick(object sender, EventArgs e)
		{
			var availableWorkers = DBWorker.GetAvailableWorkers();
			if (availableWorkers.Count > 0)
			{
				var worker = availableWorkers[0];
				worker.SetBusy();
				Login(worker.Username, worker.Password, LocationTextBox.Text, Convert.ToInt32(StepCountTextBox.Text));
			}
			else
			{
				UserLabel.Text = "No workers available. Try again later.";
			}
		}
	}
}