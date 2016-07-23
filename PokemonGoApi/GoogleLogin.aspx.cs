using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
using PokemonGoApi.Source;

namespace PokemonGoApi
{
	public partial class GoogleLogin : System.Web.UI.Page
	{
		protected string Parameters;

		protected void Page_Load(object sender, EventArgs e)
		{
			if (Session["idToken"] != null)
			{
				Response.Redirect("Default.aspx");
			}

			if (Session["loginWith"] != null && Session["loginWith"].ToString() == "google")
			{
				var url = Request.Url.Query;
				if (url != "")
				{
					string queryString = url.ToString();
					char[] delimiterChars = { '=' };
					string[] words = queryString.Split(delimiterChars);
					string code = words[1];

					if (code != null)
					{
						//get the access token 
						HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create("https://www.googleapis.com/oauth2/v4/token");
						webRequest.Method = "POST";
						Parameters = "code=" + code + "&client_id=" + ConfigurationManager.AppSettings["GoogleClientId"] + 
							"&client_secret=" + ConfigurationManager.AppSettings["GoogleClientSecret"] + 
							"&redirect_uri=" + ConfigurationManager.AppSettings["GoogleRedirectUrl"] + "&grant_type=authorization_code";
						byte[] byteArray = Encoding.UTF8.GetBytes(Parameters);
						webRequest.ContentType = "application/x-www-form-urlencoded";
						webRequest.ContentLength = byteArray.Length;
						Stream postStream = webRequest.GetRequestStream();
						// Add the post data to the web request
						postStream.Write(byteArray, 0, byteArray.Length);
						postStream.Close();

						WebResponse response = webRequest.GetResponse();
						postStream = response.GetResponseStream();
						StreamReader reader = new StreamReader(postStream);
						string responseFromServer = reader.ReadToEnd();

						GooglePlusAccessToken serStatus = JsonConvert.DeserializeObject<GooglePlusAccessToken>(responseFromServer);

						if (serStatus != null)
						{
							if (!string.IsNullOrEmpty(serStatus.id_token))
							{
								Session["idToken"] = serStatus.id_token;
								Session["accessToken"] = serStatus.access_token;
								Session["GooglePlusAccessToken"] = serStatus;
								Response.Redirect("Default.aspx");
							}
						}
					}
				}
			}
		}

		protected void LogoutLinkButton_OnClick(object sender, EventArgs e)
		{
			Session["idToken"] = null;
			Session["loginWith"] = null;
			Response.Redirect("Default.aspx");
		}
	}
}