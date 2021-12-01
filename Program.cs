using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;

namespace spotify
{
	class Program
	{
		private static readonly string ClientId = ConfigurationManager.AppSettings["clientid"];
		private static readonly string ClientSecret = ConfigurationManager.AppSettings["clientsecret"];
		private static readonly string Scope = "user-read-private user-read-email user-read-playback-position user-top-read user-read-recently-played playlist-modify-private playlist-read-collaborative playlist-read-private playlist-modify-public";
		private static readonly string RedirectUrl = "https://localhost:1337/callback";
		private static readonly string State = "ffEENtest";
		private static readonly string TextFileLocation = ConfigurationManager.AppSettings["textfilelocation"];

		static void Main(string[] args)
		{
			try
			{
				if (string.IsNullOrEmpty(Code))
				{
					Authorize();
				}
				var token = Token;
				if (string.IsNullOrEmpty(token))
				{
					token = GetToken();
					File.WriteAllText(Path.Combine(TextFileLocation, "token.txt"), token);
				}
				FetchTracks(token, "long_term");
				FetchTracks(token, "medium_term");
				FetchTracks(token, "short_term");
			}
			catch (Exception x) {
				Console.WriteLine(x.Message);
			}
			Console.WriteLine("Klaar!");
			Console.ReadKey();
		}

		private static void FetchTracks(string token, string timeRange)
		{
			var tracks = GetData(token, $"https://api.spotify.com/v1/me/top/tracks?limit=50&time_range={timeRange}");
			var trackResult = System.Text.Json.JsonSerializer.Deserialize<TopMusicResult>(tracks);
			var counter = 1;
			var resultWriter = new StringBuilder();
			foreach (var track in trackResult.items)
			{
				var line = $"{counter}: {track.artists[0].name} - {track.name}";
				resultWriter.AppendLine(line);
				Console.WriteLine(line);
				counter++;
			}
			File.WriteAllText(Path.Combine(TextFileLocation, $"resultlist_{timeRange}.txt"), resultWriter.ToString());
		}

		private static string Code { get {
				var codeFile = Path.Combine(TextFileLocation, "code.txt");
				var result = (File.Exists(codeFile) ? File.ReadAllText(codeFile) : "");
				if (!string.IsNullOrEmpty(result))
				{
					var uri = new Uri(result);
					var items = uri.Query.Split(new char[] { '&' });
					foreach(var item in items)
					{
						if (item.TrimStart(new char[] { '?' }).StartsWith("code="))
						{
							result = item.Substring(item.IndexOf('=') + 1);
						}
					}
				}
				return result;
		}}


		private static string Token
		{
			get
			{
				var tokenFile = Path.Combine(TextFileLocation, "token.txt");
				return (File.Exists(tokenFile) ? File.ReadAllText(tokenFile) : "");
			}
		}

		private static void Authorize()
		{
			Console.WriteLine("Open onderstaande URL en druk dan op Enter:\r\n\r\n");
			Console.WriteLine($"https://accounts.spotify.com/authorize?response_type=code&client_id={ClientId}&scope={Scope}&redirect_uri={RedirectUrl}&state={State}");
			Console.ReadKey();
		}

		private static string GetToken()
		{
			var token = new AccessToken() { };

			Console.WriteLine($"Plak de URL (dus {RedirectUrl}?code=heel-lange-tekst&state={State}) in het bestand code.txt, sla dat op en druk op een toets.");
			Console.ReadKey();
			var code = Code;
			const string url = "https://accounts.spotify.com/api/token";

			var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);

			var bytes = UTF8Encoding.UTF8.GetBytes($"{ClientId}:{ClientSecret}");
			var baseString = Convert.ToBase64String(bytes);
			httpWebRequest.Headers.Add(HttpRequestHeader.Authorization, $"Basic {baseString}");

			httpWebRequest.ContentType = "application/x-www-form-urlencoded";
			httpWebRequest.PreAuthenticate = true;
			httpWebRequest.Method = "POST";

			using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
			{
				var formData = $"code={code}&redirect_uri={RedirectUrl}&grant_type=authorization_code";
				streamWriter.Write(formData);
			}

			var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
			using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
			{
				var result = streamReader.ReadToEnd();
				Console.WriteLine(result);
				token = System.Text.Json.JsonSerializer.Deserialize<AccessToken>(result);
			}

			return token.access_token;
		}

		private static string GetData(string token, string url)
		{
			var result = "";
			var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);

			httpWebRequest.Headers.Add(HttpRequestHeader.Authorization, $"Bearer {token}");

			httpWebRequest.PreAuthenticate = true;
			httpWebRequest.Method = "GET";

			var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
			using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
			{
				result = streamReader.ReadToEnd();
			}
			return result;
		}

		private class AccessToken
		{
			public string access_token { get; set; }
		}

		private class TopMusicResult
		{
			public int total { get; set; }
			public string next { get; set; }
			public Album[] items { get; set; }
		}

		private class Artist
		{
			public string name { get; set; }
		}

		private class Album
		{
			public string name { get; set; }
			public Artist[] artists {get;set;}	
		}
	}
}