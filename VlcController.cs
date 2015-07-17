using System;

namespace YAWIP
{
	public class VlcController : MinimalisticTelnet.TelnetConnection
	{
		public bool Authenticated { get; private set; }
		
		public VlcController(string hostname, uint port) : base(hostname, (int)port) { }

		public bool Authenticate(string password)
		{
			string tmp = Login(password, 100);
			Authenticated = tmp.Length > 2 && tmp [tmp.Length - 2] == '>';
			return Authenticated;
		}

		public string GetCurrentSongTitle()
		{
			string tmp;

			WriteLine ("get_title");
			tmp = Read ();
			return tmp.Substring (0, tmp.Length - 4);
		}
	}
}

