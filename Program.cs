using System;
using Gtk;

namespace YAWIP
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Application.Init ();
			YAWIPForm win = new YAWIPForm ();
			win.Show ();
			Application.Run ();
		}
	}
}
