using YAWIP;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using Gtk;
using SpotifyAPI.Local;
using MinimalisticTelnet;

public partial class YAWIPForm: Gtk.Window
{
	Thread worker;
	WorkType currentWork;
	bool working = false;
	uint istat = 0;
	string filepath;

	public YAWIPForm() : base (Gtk.WindowType.Toplevel)
	{
		Build ();

		#region Graphics
		txtFilePath.Xalign = 1.0F;
		spinRefreshRate.Xalign = 1.0F;
		txtVlcHostname.Xalign = 1.0F;
		spinVlcPort.Xalign = 1.0F;
		txtVlcPassword.Xalign = 1.0F;
		#endregion

		#region Configs
		Configs.Load();
		LoadConfigs();
		#endregion
	}

	protected void OnDeleteEvent (object sender, DeleteEventArgs a)
	{
		#region Configs
		Configs.FilePath = txtFilePath.Text;
		Configs.RefreshRate = (uint)spinRefreshRate.Value;
		Configs.VlcHostname = txtVlcHostname.Text;
		Configs.VlcPort = (uint)spinVlcPort.Value;
		Configs.VlcPassword = txtVlcPassword.Text;

		Configs.Save ();
		#endregion


		if (worker != null && worker.IsAlive)
			worker.Abort ();
		Application.Quit ();
		a.RetVal = true;
	}

	void LoadConfigs()
	{
		txtFilePath.Text = Configs.FilePath;
		spinRefreshRate.Value = Configs.RefreshRate;
		txtVlcHostname.Text = Configs.VlcHostname;
		spinVlcPort.Value = Configs.VlcPort;
		txtVlcPassword.Text = Configs.VlcPassword;
	}

	protected void ResetConfigs (object sender, EventArgs e)
	{
		Configs.Reset ();
		LoadConfigs ();
	}

	protected void OnCommandClick (object sender, EventArgs e)
	{
		if (working)
		{
			stop ();
			SetStatus ("Stopped {0} worker sucessfully", currentWork.ToString());
		}
		else
		{
			start ();
			SetStatus ("Starting {0} worker", currentWork.ToString());
		}
	}

	void stop()
	{
		File.WriteAllText (filepath, string.Empty);

		btnCommand.Label = "Start";

		working = false;

		if (worker != null && worker.IsAlive)
			worker.Abort ();
	}

	void start()
	{
		filepath = txtFilePath.Text;
		currentWork = (WorkType)notebook.CurrentPage;

		worker = new Thread (work);
		worker.Start ();

		btnCommand.Label = "Stop";

		working = true;
	}

	void work()
	{
		uint refreshrate = (uint)spinRefreshRate.Value;
		string tmp, currentsong;

		switch (currentWork) {
		case WorkType.VLC:
			#region VLC
			string hostname = txtVlcHostname.Text;
			uint port = (uint)spinVlcPort.Value;
			string password = txtVlcPassword.Text;
			VlcController conn;

			try
			{
				conn = new VlcController(hostname, port);
				conn.Authenticate(password);
				if (!conn.Authenticated)
				{
					Application.Invoke(delegate{ SetStatus("Wrong password, please retry!");});
					stop();
				}

				Application.Invoke(delegate{ SetStatus("VLC worker running");});

				while (true)
				{
					tmp = conn.GetCurrentSongTitle();
					File.WriteAllText(filepath, tmp);
					Thread.Sleep ((int)refreshrate);
				}
			}
			catch(SocketException ex)
			{
				Application.Invoke(delegate{ SetStatus("Can't connect to the VLC server / Connection aborted by the server");});
				stop();
			}

			break;
			#endregion
		case WorkType.Spotify:
			#region Spotify
			Application.Invoke(delegate{ SetStatus("Spotify worker running");});
			while (true)
			{
				Thread.Sleep ((int)refreshrate);
			}
			#endregion
		default:
			Application.Invoke (delegate {SetStatus ("Error: Invalid work selected");});
			stop ();
			break;
		}
	}

	void SetStatus(string format, params object[] objs)
	{
		PopStatus ();
		PushStatus (format, objs);
	}

	void PushStatus(string format, params object[] objs)
	{
		statusbar.Push (istat++, String.Format(format, objs));
	}

	void PopStatus()
	{
		if(istat != 0)
			statusbar.Pop (--istat);
	}
}
