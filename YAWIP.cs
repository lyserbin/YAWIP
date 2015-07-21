using YAWIP;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using Gtk;
using Gdk;
using SpotifyAPI.Local;
using SpotifyAPI.Local.Models;

static class FormatterCodes
{
	internal static readonly string Title = Encoding.Default.GetString(new byte[] {0xFF, 0x01});
	internal static readonly string Artist = Encoding.Default.GetString(new byte[] {0xFF, 0x02});
	internal static readonly string Album = Encoding.Default.GetString(new byte[] {0xFF, 0x03});
}

public partial class YAWIPForm: Gtk.Window
{
	SpotifyLocalAPI spotify;
	Thread worker;
	WorkType currentWork;
	bool working = false;
	uint istat = 0;
	string filepath;
	StatusIcon trayIcon;

	public YAWIPForm() : base (Gtk.WindowType.Toplevel)
	{
		Build ();

		#region Graphics
		txtFilePath.Xalign = 1.0F;
		spinRefreshRate.Xalign = 1.0F;
		txtVlcHostname.Xalign = 1.0F;
		spinVlcPort.Xalign = 1.0F;
		txtVlcPassword.Xalign = 1.0F;
		txtSpotifyFormat.Xalign = 1.0F;
		#endregion

		#region Configs
		Configs.Load();
		LoadConfigs();
		#endregion

		#region TrayIcon
		trayIcon = new StatusIcon (this.Icon);
		trayIcon.Tooltip = "Yet Another What Is Playing";
		trayIcon.Visible = true;
		trayIcon.Activate += OnTrayIconClick;
		#endregion
	}

	#region Events

	protected void OnDeleteEvent (object sender, DeleteEventArgs a)
	{
		#region Configs
		Configs.FilePath = txtFilePath.Text;
		Configs.RefreshRate = (uint)spinRefreshRate.Value;
		Configs.VlcHostname = txtVlcHostname.Text;
		Configs.VlcPort = (uint)spinVlcPort.Value;
		Configs.VlcPassword = txtVlcPassword.Text;
		Configs.SpotifyFormat = txtSpotifyFormat.Text;

		Configs.Save ();
		#endregion

		stop ();	
		trayIcon.Dispose ();
		Application.Quit ();
		a.RetVal = true;
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

	protected void OnTrayIconClick(object sender, EventArgs e)
	{
		this.Visible = !this.Visible;
		if (this.Visible)
			this.Deiconify ();
	}

	protected void OnWindowStateEvent(object sender, WindowStateEventArgs e)
	{
		if (e.Event.ChangedMask == WindowState.Iconified && e.Event.NewWindowState == WindowState.Iconified)
			this.Visible = false;
	}

	protected void ResetConfigs (object sender, EventArgs e)
	{
		Configs.Reset ();
		LoadConfigs ();
	}

	#endregion

	void LoadConfigs()
	{
		txtFilePath.Text = Configs.FilePath;
		spinRefreshRate.Value = Configs.RefreshRate;
		txtVlcHostname.Text = Configs.VlcHostname;
		spinVlcPort.Value = Configs.VlcPort;
		txtVlcPassword.Text = Configs.VlcPassword;
		txtSpotifyFormat.Text = Configs.SpotifyFormat;
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
		string tmp, currentsong = string.Empty;

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
					if(tmp!= currentsong)
					{
						currentsong = tmp;
						File.WriteAllText(filepath, tmp);
					}
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
			spotify = new SpotifyLocalAPI();

			if (!SpotifyLocalAPI.IsSpotifyRunning())
			{
				Application.Invoke(delegate{ SetStatus("Spotify isn't running!");});
				stop();
			}
			else if (!SpotifyLocalAPI.IsSpotifyWebHelperRunning())
			{
				Application.Invoke(delegate{ SetStatus("SpotifyWebHelper isn't running!");});
				stop();
			}
			else if (!spotify.Connect())
			{
				Application.Invoke(delegate{ SetStatus("Can't connect to the SpotifyWebHelper.");});
				stop();
			}

			Application.Invoke(delegate{ SetStatus("Spotify worker running");});

			while (true)
			{
				StatusResponse status =  spotify.GetStatus();
				if(status != null && status.Track != null)
				{
					tmp = status.Track.TrackResource.Uri;

					if(tmp != currentsong)
					{
						string format, formatted;

						currentsong = tmp;

						format = txtSpotifyFormat.Text;
						format = format.Replace("%t", FormatterCodes.Title);
						format = format.Replace("%a", FormatterCodes.Artist);
						format = format.Replace("%A", FormatterCodes.Album);

						formatted = format.Replace(FormatterCodes.Title, status.Track.TrackResource.Name);
						formatted = formatted.Replace(FormatterCodes.Artist, status.Track.ArtistResource.Name);
						formatted = formatted.Replace(FormatterCodes.Album, status.Track.AlbumResource.Name);

						File.WriteAllText(filepath, formatted);
					}
				}

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