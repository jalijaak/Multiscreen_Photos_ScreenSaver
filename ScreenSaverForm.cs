using System;
using System.Drawing;
using System.Windows.Forms;

namespace ScreenSaver
{
	public class ScreenSaverForm : System.Windows.Forms.Form
	{
		private System.ComponentModel.IContainer components;
		private Point MouseXY;
		private int ScreenNumber;
		private AnimationControl animationControl;
		protected int screenIndex;
		private int animationStepInterval = 100; // Default animation step interval

		public ScreenSaverForm(int scrn)
		{
			components = new System.ComponentModel.Container();
			InitializeComponent();
			ScreenNumber = scrn;
			
			// Create and configure animation control
			animationControl = new AnimationControl();
			animationControl.Dock = DockStyle.Fill;
			Controls.Add(animationControl);

			screenIndex = scrn;
			Screen screen = Screen.AllScreens[screenIndex];
			
			this.FormBorderStyle = FormBorderStyle.None;
			this.Bounds = screen.Bounds;
			this.TopMost = true;
		}

		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
				if (animationControl != null)
				{
					animationControl.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		private void ScreenSaverForm_Load(object sender, System.EventArgs e)
		{
			try
			{
				// Set form bounds to match the screen
				Screen targetScreen = Screen.AllScreens[ScreenNumber];
				this.Bounds = targetScreen.Bounds;
				
				// Configure window for screensaver display
				Cursor.Hide();
				TopMost = true;
				
				// Start the animation
				if (animationControl != null)
				{
					animationControl.Animate(animationStepInterval);
				}
			}
			catch (Exception ex)
			{
				// If there's an error (like screen no longer exists), close this form
				MessageBox.Show($"Error initializing screen {ScreenNumber}: {ex.Message}");
				Close();
			}
		}

		private void OnMouseEvent(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			if (!MouseXY.IsEmpty)
			{
				// Close if mouse has moved significantly (more than 5 pixels)
				if (Math.Abs(MouseXY.X - e.X) > 5 || Math.Abs(MouseXY.Y - e.Y) > 5)
					Application.Exit();
				if (e.Clicks > 0)
					Application.Exit();
			}
			MouseXY = new Point(e.X, e.Y);
		}
		
		private void ScreenSaverForm_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			Application.Exit();
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			// No need to stop animation explicitly - it will be handled by Dispose
			Cursor.Show();
			base.OnFormClosing(e);
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			// 
			// ScreenSaverForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.BackColor = System.Drawing.Color.Black;
			this.ClientSize = new System.Drawing.Size(292, 273);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.Name = "ScreenSaverForm";
			this.Text = "ScreenSaver";
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ScreenSaverForm_KeyDown);
			this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.OnMouseEvent);
			this.Load += new System.EventHandler(this.ScreenSaverForm_Load);
			this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.OnMouseEvent);

		}
		#endregion
	}
}
