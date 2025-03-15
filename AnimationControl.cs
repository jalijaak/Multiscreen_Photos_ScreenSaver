using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
// a_pess&yahoo.com
// omar amin ibrahim
// coding for fun
// OCtober 31, 2008
// dedicated to Bob

using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ScreenSaver
{

    public class AnimationControl : Control
    {

        #region " Field "


        private System.Timers.Timer m_timer = new System.Timers.Timer();
        private TimeSpan m_AnimationSpeed = new TimeSpan(0, 0, 0, 2, 0);
        private DateTime m_AnimationStartTime;
        private Bitmap m_AnimatedBitmap = null;
        private Bitmap m_AnimatedFadeImage = null;
        private AnimationTypes m_AnimationType;
        private int m_Divider = 4; // Number of divisions for chess/panorama effects

        private int m_AnimationPercent = 0;
        private Color m_backcolor;
        private Color m_borderColor;
        private Size m_minSize;

        private GraphicsPath Path;
        private Rectangle Rect;

        private bool IsAnimating = false;
        private bool m_transparent;
        private Color m_transparentColor;

        private double m_opacity;

        private Boolean m_showFileName;

        [System.ComponentModel.DefaultValue(typeof(Font), "Arial, 12pt")]
        [System.ComponentModel.Description("Set the font for file name display.")]
        [System.ComponentModel.Category("File Name Display")]
        public Font FileNameFont { get; set; } = new Font("Arial", 12, FontStyle.Regular);

        [System.ComponentModel.DefaultValue(typeof(Color), "White")]
        [System.ComponentModel.Description("Set the color for file name display.")]
        [System.ComponentModel.Category("File Name Display")]
        public Color FileNameColor { get; set; } = Color.White;

        [System.ComponentModel.DefaultValue(2)]
        [System.ComponentModel.Description("Set how the file name should be displayed (0: Full path, 1: Relative path, 2: File name only).")]
        [System.ComponentModel.Category("File Name Display")]
        public int FileNameDisplayMode { get; set; } = 2;

        #endregion

        #region " Constructor "


        public AnimationControl()
        {

            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.Opaque, false);
            SetStyle(ControlStyles.DoubleBuffer, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            UpdateStyles();

            m_backcolor = Color.Transparent;
            m_minSize = new Size(100, 100);
            m_borderColor = Color.LightGray;
            m_transparent = false;
            m_transparentColor = Color.DodgerBlue;
            m_opacity = 1.0;

            m_timer.Elapsed += TimerTick;

        }

        #endregion

        #region " Property "

        protected override Size DefaultSize
        {
            get { return new Size(150, 150); }
        }

        public override System.Drawing.Size MinimumSize
        {
            get { return m_minSize; }
            set
            {
                if ((value != (m_minSize)))
                {
                    m_minSize = value;
                    Refresh();
                }
            }
        }

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [System.ComponentModel.DefaultValue(typeof(Color), "Transparent")]
        [System.ComponentModel.Description("Set background color.")]
        [System.ComponentModel.Category("Control Style")]
        public override System.Drawing.Color BackColor
        {
            get { return m_backcolor; }
            set
            {
                m_backcolor = value;
                Refresh();
            }
        }

        [System.ComponentModel.Browsable(true)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
        [System.ComponentModel.DefaultValue(1.0)]
        [System.ComponentModel.TypeConverter(typeof(OpacityConverter))]
        [System.ComponentModel.Description("Set the opacity percentage of the control.")]
        [System.ComponentModel.Category("Control Style")]
        public virtual double Opacity
        {
            get { return m_opacity; }
            set
            {
                if (value == m_opacity)
                {
                    return;
                }
                m_opacity = value;
                UpdateStyles();
                Refresh();
            }
        }

        [System.ComponentModel.Browsable(true)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
        [System.ComponentModel.DefaultValue(typeof(bool), "False")]
        [System.ComponentModel.Description("Enable control trnasparency.")]
        [System.ComponentModel.Category("Control Style")]
        public virtual bool Transparent
        {
            get { return m_transparent; }
            set
            {
                if (value == m_transparent)
                {
                    return;
                }
                m_transparent = value;
                Refresh();
            }
        }

        [System.ComponentModel.Browsable(true)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
        [System.ComponentModel.DefaultValue(typeof(Color), "DodgerBlue")]
        [System.ComponentModel.Description("Set the fill color of the control.")]
        [System.ComponentModel.Category("Control Style")]
        public virtual Color TransparentColor
        {
            get { return m_transparentColor; }
            set
            {
                m_transparentColor = value;
                Refresh();
            }
        }

        [System.ComponentModel.DefaultValue(typeof(Bitmap), "")]
        [System.ComponentModel.Description("Set animated iamge.")]
        [System.ComponentModel.Category("Control Style")]
        public Bitmap AnimatedImage
        {
            get { return m_AnimatedBitmap; }
            set { m_AnimatedBitmap = value; }
        }

        [System.ComponentModel.DefaultValue(typeof(Bitmap), "")]
        [System.ComponentModel.Description("Set fade iamge.")]
        [System.ComponentModel.Category("Control Style")]
        public Bitmap AnimatedFadeImage
        {
            get { return m_AnimatedFadeImage; }
            set { m_AnimatedFadeImage = value; }
        }

        [System.ComponentModel.DefaultValue(typeof(AnimationTypes), "Maximize")]
        [System.ComponentModel.Description("Set animation type.")]
        [System.ComponentModel.Category("Control Style")]
        public AnimationTypes AnimationType
        {
            get { return m_AnimationType; }
            set { m_AnimationType = value; }
        }

        [System.ComponentModel.DefaultValue(typeof(float), "2")]
        [System.ComponentModel.Description("Set animation speed. the greater value the slowest speed")]
        [System.ComponentModel.Category("Control Style")]
        public float AnimationSpeed
        {
            get { return Convert.ToSingle(m_AnimationSpeed.TotalSeconds); }
            set { m_AnimationSpeed = new TimeSpan(0, 0, 0, 0, Convert.ToInt32((1000 * value))); }
        }

        [System.ComponentModel.DefaultValue(typeof(Color), "LightGray")]
        [System.ComponentModel.Description("Set border color.")]
        [System.ComponentModel.Category("Control Style")]
        public Color BorderColor
        {
            get { return m_borderColor; }
            set
            {
                m_borderColor = value;
                Invalidate();
            }
        }

        [System.ComponentModel.DefaultValue(typeof(Boolean), "true")]
        [System.ComponentModel.Description("Show file names.")]
        [System.ComponentModel.Category("Control Style")]
        public Boolean showFileName
        {
            get { return m_showFileName; }
            set
            {
                m_showFileName = value;
            }
        }
        public int AnimationPercent
        {
            get { return m_AnimationPercent; }
        }


        #endregion

        #region " Method "


        ///<summary>
        ///<para>Starts control animation</para>
        ///<param name="m_Interval">duration between animation steps in miliseconds, recomended 20-60</param>
        ///</summary>
        public void Animate(int m_Interval)
        {
            if (m_Interval > 100)
            {
                m_Interval = 100;
            }

            m_timer.Interval = m_Interval;
            m_timer.Enabled = true;
            m_AnimationPercent = 0;
            m_AnimationStartTime = DateTime.Now;
            IsAnimating = true;

            Invalidate();

        }

        private void AnimationStop()
        {
            IsAnimating = false;
            m_timer.Enabled = false;
        }


        private void TimerTick(object source, System.Timers.ElapsedEventArgs e)
        {
            TimeSpan ts = DateTime.Now - m_AnimationStartTime;
            if(ts.TotalSeconds > 10) ts = TimeSpan.FromSeconds(10);
            m_AnimationPercent = Convert.ToInt16((100f / m_AnimationSpeed.TotalSeconds * ts.TotalSeconds));

            if (m_AnimationPercent > 100)
            {
                m_AnimationPercent = 100;
            }

            Invalidate();

        }


        private void DrawBorder(Graphics g, AnimationControl control)
        {
            if (m_borderColor != Color.Transparent)
            {
                using (Pen p = new Pen(GetDarkColor(this.BorderColor, 40), -1))
                {
                    Rect = new Rectangle(control.ClientRectangle.X, control.ClientRectangle.Y, control.ClientRectangle.Width - 1, control.ClientRectangle.Height - 1);
                    g.DrawRectangle(p, Rect);
                }
            }

        }


        private void DrawBackground(Graphics g, AnimationControl control)
        {
            if (Transparent)
            {
                using (SolidBrush sb = new SolidBrush(control.BackColor))
                {
                    g.FillRectangle(sb, Rect);

                    using (SolidBrush sbt = new SolidBrush(Color.FromArgb((int)(control.Opacity * 255), control.TransparentColor)))
                    {
                        g.FillRectangle(sbt, Rect);
                    }
                }


            }
            else
            {
                using (SolidBrush sb = new SolidBrush(control.TransparentColor))
                {
                    g.FillRectangle(sb, Rect);
                }
            }

        }


        protected void DrawAnimatedImage(Graphics g, AnimationControl control)
        {

            if (m_AnimatedBitmap != null)
            {
                //prepare aspect ratio preservation data
                Rectangle destRectangle = control.ClientRectangle;
                Rectangle sourceRectangle = new Rectangle(0, 0, m_AnimatedBitmap.Width, m_AnimatedBitmap.Height);

                float aspectRatio = ((float)m_AnimatedBitmap.Height / (float)m_AnimatedBitmap.Width);
                int newHeight = (int)(destRectangle.Width * aspectRatio);
                //newHeight = Math.Min(newHeight, destRectangle.Height);//prevent overflow
                int newTopLeft = (destRectangle.Height - newHeight) / 2;
                if (newTopLeft > 0) //prevent overflow
                {
                    destRectangle.Height = newHeight;
                    destRectangle.Y = newTopLeft;
                }
                else
                {
                    //to maintain correct aspect ratio - fix destination size width
                    //NewHeight = GivenWidth * (OriginalHeight / OriginalWidth)
                    int newWidth = (int)(destRectangle.Height / aspectRatio);
                    newWidth = Math.Min(newWidth, destRectangle.Width);
                    newTopLeft = (destRectangle.Width - newWidth) / 2;
                    destRectangle.Width = newWidth;
                    destRectangle.X = newTopLeft;

                }
                //save original graphic transform for file name dispaly
                System.Drawing.Drawing2D.Matrix originalTransform = g.Transform;

                switch (m_AnimationType)
                {
                    case AnimationTypes.None:
                        // Image shows imidiatly - no animation
                        g.DrawImage(m_AnimatedBitmap, destRectangle, sourceRectangle, GraphicsUnit.Pixel);
                        break;

                    case AnimationTypes.BottomLeftToTopRight:
                        // Image Slides from bottom left to top right effect

                        //scales image to control size
                        System.Drawing.Drawing2D.Matrix mx = new System.Drawing.Drawing2D.Matrix(1, 0, 0, 1, (control.Width * m_AnimationPercent / 100) - control.Width, -(control.Height * m_AnimationPercent / 100) + control.Height);
                        g.Transform = mx;
                        g.DrawImage(m_AnimatedBitmap, destRectangle, sourceRectangle, GraphicsUnit.Pixel);
                        mx.Dispose();
                        break;

                    // --------------->
                    case AnimationTypes.BottomRightToTopLeft:
                        // Image Slides from bottom right to top left

                        mx = new System.Drawing.Drawing2D.Matrix(1, 0, 0, 1, -(control.Width * m_AnimationPercent / 100) + control.Width, -(control.Height * m_AnimationPercent / 100) + control.Height);
                        g.Transform = mx;
                        //g.DrawImage(m_AnimatedBitmap, control.ClientRectangle, 0, 0, m_AnimatedBitmap.Width, m_AnimatedBitmap.Height, GraphicsUnit.Pixel);
                        g.DrawImage(m_AnimatedBitmap, destRectangle, sourceRectangle, GraphicsUnit.Pixel);
                        mx.Dispose();
                        break;

                    // --------------->
                    case AnimationTypes.ChessBoard:
                        // Image chess board effect

                        Path = new GraphicsPath();
                        int cw = Convert.ToInt32((control.Width * m_AnimationPercent / 100)) / m_Divider;
                        int ch = Convert.ToInt32((control.Height * m_AnimationPercent / 100)) / m_Divider;
                        int row = 0;
                        int col = 0;

                        int y = 0;
                        while (y < control.Height)
                        {
                            int x = 0;
                            while (x < control.Width)
                            {
                                Rectangle rc = new Rectangle(x, y, cw, ch);

                                if ((row & 1) == 1)
                                {
                                    if ((col & 1) == 1)
                                    {
                                        rc.Offset(control.Width / (2 * m_Divider), control.Height / (2 * m_Divider));
                                    }

                                }

                                Path.AddRectangle(rc);

                                if (m_AnimationPercent >= 50 && (row & 1) == 1 && x == 0)
                                {

                                    if (m_AnimationPercent >= 50 && (col & 1) == 1 && y == 0)
                                    {
                                        rc.Offset((control.Width / m_Divider), (control.Height / m_Divider));
                                        Path.AddRectangle(rc);

                                    }
                                }
                                x += control.Width / m_Divider;
                            }
                            row += 1;
                            y += control.Height / m_Divider;
                        }
                        col += 1;

                        Region r = new Region(Path);
                        g.SetClip(r, CombineMode.Intersect);
                        //g.DrawImage(m_AnimatedBitmap, control.ClientRectangle, 0, 0, m_AnimatedBitmap.Width, m_AnimatedBitmap.Height, GraphicsUnit.Pixel);
                        g.DrawImage(m_AnimatedBitmap, destRectangle, sourceRectangle, GraphicsUnit.Pixel);
                        r.Dispose();
                        Path.Dispose();
                        break;

                    // --------------->
                    case AnimationTypes.ChessHorizontal:
                        // Image chess board horizontal effect

                        Path = new GraphicsPath();
                        cw = Convert.ToInt32((control.Width * m_AnimationPercent / 100)) / m_Divider;
                        ch = control.Height / m_Divider;
                        row = 0;
                        y = 0;
                        while (y < control.Height)
                        {
                            int x = 0;
                            while (x < control.Width)
                            {
                                Rectangle rc = new Rectangle(x, y, cw, ch);
                                if ((row & 1) == 1)
                                {
                                    rc.Offset(control.Width / (2 * m_Divider), 0);
                                }
                                Path.AddRectangle(rc);
                                if (m_AnimationPercent >= 50 && (row & 1) == 1 && x == 0)
                                {
                                    rc.Offset(-(control.Width / m_Divider), 0);
                                    Path.AddRectangle(rc);
                                }
                                x += control.Width / m_Divider;
                            }
                            row += 1;
                            y += ch;
                        }
                        r = new Region(Path);
                        g.SetClip(r, CombineMode.Intersect);
                        //g.DrawImage(m_AnimatedBitmap, control.ClientRectangle, 0, 0, m_AnimatedBitmap.Width, m_AnimatedBitmap.Height, GraphicsUnit.Pixel);
                        g.DrawImage(m_AnimatedBitmap, destRectangle, sourceRectangle, GraphicsUnit.Pixel);
                        r.Dispose();
                        Path.Dispose();
                        break;

                    // --------------->
                    case AnimationTypes.ChessVertical:
                        // Image chess board vertical effect

                        Path = new GraphicsPath();
                        cw = destRectangle.Width / m_Divider;
                        ch = Convert.ToInt32((destRectangle.Height * m_AnimationPercent / 100)) / m_Divider;
                        col = 0;
                        int x1 = 0;
                        while (x1 < destRectangle.Width)
                        {
                            int y1 = 0;
                            while (y1 < control.Height)
                            {
                                Rectangle rc = new Rectangle(x1, y1, cw, ch);
                                if ((col & 1) == 1)
                                {
                                    rc.Offset(0, destRectangle.Height / (2 * m_Divider));
                                }
                                Path.AddRectangle(rc);
                                if (m_AnimationPercent >= 50 && (col & 1) == 1 && y1 == 0)
                                {
                                    rc.Offset(0, -(destRectangle.Height / m_Divider));
                                    Path.AddRectangle(rc);
                                }
                                y1 += destRectangle.Height / m_Divider;
                            }
                            col += 1;
                            x1 += cw;
                        }
                        r = new Region(Path);
                        g.SetClip(r, CombineMode.Intersect);
                        //g.DrawImage(m_AnimatedBitmap, control.ClientRectangle, 0, 0, m_AnimatedBitmap.Width, m_AnimatedBitmap.Height, GraphicsUnit.Pixel);
                        g.DrawImage(m_AnimatedBitmap, destRectangle, sourceRectangle, GraphicsUnit.Pixel);
                        r.Dispose();
                        Path.Dispose();
                        break;

                    // --------------->
                    case AnimationTypes.Fade:
                        // Image fade effect


                        if (true)
                        {
                            ImageAttributes attr = new ImageAttributes();
                            ColorMatrix mx1 = new ColorMatrix();
                            mx1.Matrix33 = 1f / 255 * (255 * m_AnimationPercent / 100);
                            attr.SetColorMatrix(mx1);
                            //g.DrawImage(m_AnimatedBitmap, control.ClientRectangle, 0, 0, m_AnimatedBitmap.Width, m_AnimatedBitmap.Height, GraphicsUnit.Pixel, attr);
                            g.DrawImage(m_AnimatedBitmap, destRectangle, sourceRectangle.X, sourceRectangle.Y, sourceRectangle.Width, sourceRectangle.Height, GraphicsUnit.Pixel, attr);
                            attr.Dispose();
                        }

                        break;

                    // --------------->
                    case AnimationTypes.Fade2Images:
                        // fade two image effect


                        if (true)
                        {
                            if (m_AnimationPercent < 100)
                            {
                                if (m_AnimatedFadeImage != null)
                                {
                                    //g.DrawImage(m_AnimatedFadeImage, control.ClientRectangle, 0, 0, m_AnimatedFadeImage.Width, m_AnimatedFadeImage.Height, GraphicsUnit.Pixel);
                                    g.DrawImage(m_AnimatedBitmap, destRectangle, sourceRectangle, GraphicsUnit.Pixel);
                                }
                            }

                            ImageAttributes attr = new ImageAttributes();
                            ColorMatrix mx2 = new ColorMatrix();
                            mx2.Matrix33 = 1f / 255 * (255 * m_AnimationPercent / 100);
                            attr.SetColorMatrix(mx2);
                            //g.DrawImage(m_AnimatedBitmap, control.ClientRectangle, 0, 0, m_AnimatedBitmap.Width, m_AnimatedBitmap.Height, GraphicsUnit.Pixel, attr);
                            g.DrawImage(m_AnimatedBitmap, destRectangle, sourceRectangle.X, sourceRectangle.Y, sourceRectangle.Width, sourceRectangle.Height, GraphicsUnit.Pixel, attr);
                            attr.Dispose();

                        }

                        break;

                    // --------------->
                    case AnimationTypes.DownToTop:
                        // Image slide from down to top effect

                        System.Drawing.Drawing2D.Matrix mx3 = new System.Drawing.Drawing2D.Matrix(1, 0, 0, 1, 0, -(destRectangle.Height * m_AnimationPercent / 100) + destRectangle.Height);
                        g.Transform = mx3;
                        //g.DrawImage(m_AnimatedBitmap, control.ClientRectangle, 0, 0, m_AnimatedBitmap.Width, m_AnimatedBitmap.Height, GraphicsUnit.Pixel);
                        g.DrawImage(m_AnimatedBitmap, destRectangle, sourceRectangle, GraphicsUnit.Pixel);
                        mx3.Dispose();
                        break;

                    // --------------->
                    case AnimationTypes.Circular:
                        // Image circular effect

                        Path = new System.Drawing.Drawing2D.GraphicsPath();
                        int w = Convert.ToInt32(((destRectangle.Width * 1.414f) * m_AnimationPercent / 200));
                        int h = Convert.ToInt32(((destRectangle.Height * 1.414f) * m_AnimationPercent / 200));

                        Path.AddEllipse(Convert.ToInt32(control.Width / 2) - w, Convert.ToInt32(control.Height / 2) - h, 2 * w, 2 * h);
                        g.SetClip(Path, System.Drawing.Drawing2D.CombineMode.Replace);
                        g.DrawImage(m_AnimatedBitmap, destRectangle, 0, 0, m_AnimatedBitmap.Width, m_AnimatedBitmap.Height, GraphicsUnit.Pixel);
                        Path.Dispose();

                        break;

                    // --------------->
                    case AnimationTypes.Elliptical:
                        // Image elliptical effect

                        Path = new System.Drawing.Drawing2D.GraphicsPath();
                        w = Convert.ToInt32(((control.Width * 1.1 * 1.42f) * m_AnimationPercent / 200));
                        h = Convert.ToInt32(((control.Height * 1.3 * 1.42f) * m_AnimationPercent / 200));

                        Path.AddEllipse(Convert.ToInt32(control.Width / 2) - w, Convert.ToInt32(control.Height / 2) - h, 2 * w, 2 * h);
                        g.SetClip(Path, System.Drawing.Drawing2D.CombineMode.Replace);
                        //g.DrawImage(m_AnimatedBitmap, control.ClientRectangle, 0, 0, m_AnimatedBitmap.Width, m_AnimatedBitmap.Height, GraphicsUnit.Pixel);
                        g.DrawImage(m_AnimatedBitmap, destRectangle, sourceRectangle, GraphicsUnit.Pixel);
                        Path.Dispose();

                        break;

                    // --------------->
                    case AnimationTypes.LeftToRight:
                        // Image slide from left to right effect

                        System.Drawing.Drawing2D.Matrix mx4 = new System.Drawing.Drawing2D.Matrix(1, 0, 0, 1, (destRectangle.Width * m_AnimationPercent / 100) - destRectangle.Width, 0);
                        g.Transform = mx4;
                        //g.DrawImage(m_AnimatedBitmap, control.ClientRectangle, 0, 0, m_AnimatedBitmap.Width, m_AnimatedBitmap.Height, GraphicsUnit.Pixel);
                        g.DrawImage(m_AnimatedBitmap, destRectangle, sourceRectangle, GraphicsUnit.Pixel);
                        mx4.Dispose();
                        break;

                    // --------------->
                    case AnimationTypes.Maximize:
                        // Image maximize effect

                        float m_scale = (float)m_AnimationPercent / 100;
                        float cX = control.Width / 2;
                        float cY = control.Height / 2;

                        if (m_scale == 0)
                        {
                            m_scale = 0.0001f;
                        }
                        System.Drawing.Drawing2D.Matrix mx5 = new System.Drawing.Drawing2D.Matrix(m_scale, 0, 0, m_scale, cX, cY);
                        g.Transform = mx5;
                        //g.DrawImage(m_AnimatedBitmap, new Rectangle(-control.Width / 2, -control.Height / 2, control.Width, control.Height), 0, 0, m_AnimatedBitmap.Width, m_AnimatedBitmap.Height, GraphicsUnit.Pixel);
                        destRectangle = new Rectangle(-destRectangle.Width / 2, -destRectangle.Height / 2, destRectangle.Width, destRectangle.Height);
                        g.DrawImage(m_AnimatedBitmap, destRectangle, sourceRectangle, GraphicsUnit.Pixel);
                        break;

                    // --------------->
                    case AnimationTypes.Rectangular:
                        // Image rectangular effect

                        Path = new System.Drawing.Drawing2D.GraphicsPath();
                        w = Convert.ToInt32(((control.Width * 1.414f) * m_AnimationPercent / 200));
                        h = Convert.ToInt32(((control.Height * 1.414f) * m_AnimationPercent / 200));

                        Rectangle rect = new Rectangle(Convert.ToInt32(control.Width / 2) - w, Convert.ToInt32(control.Height / 2) - h, 2 * w, 2 * h);
                        Path.AddRectangle(rect);

                        g.SetClip(Path, System.Drawing.Drawing2D.CombineMode.Replace);
                        //g.DrawImage(m_AnimatedBitmap, control.ClientRectangle, 0, 0, m_AnimatedBitmap.Width, m_AnimatedBitmap.Height, GraphicsUnit.Pixel);
                        g.DrawImage(m_AnimatedBitmap, destRectangle, sourceRectangle, GraphicsUnit.Pixel);
                        Path.Dispose();

                        break;

                    // --------------->
                    case AnimationTypes.RighTotLeft:
                        // Image slide right to left effect

                        System.Drawing.Drawing2D.Matrix mx6 = new System.Drawing.Drawing2D.Matrix(1, 0, 0, 1, -(control.Width * m_AnimationPercent / 100) + control.Width, 0);
                        g.Transform = mx6;
                        //g.DrawImage(m_AnimatedBitmap, control.ClientRectangle, 0, 0, m_AnimatedBitmap.Width, m_AnimatedBitmap.Height, GraphicsUnit.Pixel);
                        g.DrawImage(m_AnimatedBitmap, destRectangle, sourceRectangle, GraphicsUnit.Pixel);
                        mx6.Dispose();
                        break;

                    // --------------->
                    case AnimationTypes.Rotate:
                        // Image rotate effect

                        float m_rotation = 360 * m_AnimationPercent / 100;
                        cX = control.Width / 2;
                        cY = control.Height / 2;
                        System.Drawing.Drawing2D.Matrix mx8 = new System.Drawing.Drawing2D.Matrix(1, 0, 0, 1, cX, cY);
                        mx8.Rotate(m_rotation, System.Drawing.Drawing2D.MatrixOrder.Prepend);
                        g.Transform = mx8;
                        //g.DrawImage(m_AnimatedBitmap, new Rectangle(-control.Width / 2, -control.Height / 2, control.Width, control.Height), 0, 0, m_AnimatedBitmap.Width, m_AnimatedBitmap.Height, GraphicsUnit.Pixel);
                        Rectangle destRectangleRotate = new Rectangle(destRectangle.X + (int)-cX, (int)(-cY + destRectangle.Y), destRectangle.Width, destRectangle.Height);
                        g.DrawImage(m_AnimatedBitmap, destRectangleRotate, sourceRectangle, GraphicsUnit.Pixel);
                        break;

                    // --------------->
                    case AnimationTypes.SpinTopLeft:
                        // Image spin in effect

                        m_rotation = 360 * m_AnimationPercent / 100;
                        m_scale = (float)m_AnimationPercent / 100;
                        if (m_scale == 0)
                        {
                            m_scale = 0.5f;
                        }
                        Rectangle destRectangleSpin = new Rectangle(destRectangle.X, destRectangle.Y, (int)(destRectangle.Width * m_scale), (int)(destRectangle.Height * m_scale));
                        cX = destRectangleSpin.Width / 2;
                        cY = destRectangleSpin.Height / 2;

                        g.TranslateTransform(cX, cY);//set rotation center on destination rectangle center
                        g.RotateTransform(m_rotation);
                        g.TranslateTransform(-cX, -cY);//relocate image back after transformation

                        g.DrawImage(m_AnimatedBitmap, destRectangleSpin, sourceRectangle, GraphicsUnit.Pixel);
                        break;

                    // --------------->
                    case AnimationTypes.SpinCenter:
                        // Image spin in effect

                        m_rotation = 360 * m_AnimationPercent / 100;
                        cX = destRectangle.Width / 2;
                        cY = destRectangle.Height / 2;
                        m_scale = (float)m_AnimationPercent / 100;
                        if (m_scale == 0)
                        {
                            m_scale = 0.01f;
                        }

                        System.Drawing.Drawing2D.Matrix mx9 = new System.Drawing.Drawing2D.Matrix(m_scale, 0, 0, m_scale, cX, cY);
                        mx9.Rotate(m_rotation, System.Drawing.Drawing2D.MatrixOrder.Prepend);
                        g.Transform = mx9;

                        //g.DrawImage(m_AnimatedBitmap, new Rectangle(-control.Width / 2, -control.Height / 2, control.Width, control.Height), 0, 0, m_AnimatedBitmap.Width, m_AnimatedBitmap.Height, GraphicsUnit.Pixel);
                        //g.DrawImage(m_AnimatedBitmap, new Rectangle(-control.Width / 2, -control.Height / 2, control.Width, control.Height), 0, 0, m_AnimatedBitmap.Width, m_AnimatedBitmap.Height, GraphicsUnit.Pixel);
                        destRectangleSpin = new Rectangle(-destRectangle.Width / 2 + destRectangle.X, -destRectangle.Height / 2 + destRectangle.Y, destRectangle.Width, destRectangle.Height);
                        g.DrawImage(m_AnimatedBitmap, destRectangleSpin, 0, 0, m_AnimatedBitmap.Width, m_AnimatedBitmap.Height, GraphicsUnit.Pixel);
                        //g.DrawImage(m_AnimatedBitmap, destRectangle, sourceRectangle, GraphicsUnit.Pixel);
                        break;

                    // --------------->
                    case AnimationTypes.TopLeftToBottomRight:
                        // Image slide from top left to bottom right effect

                        //System.Drawing.Drawing2D.Matrix mx11 = new System.Drawing.Drawing2D.Matrix(1, 0, 0, 1, (control.Width * m_AnimationPercent / 100) - control.Width, (control.Height * m_AnimationPercent / 100) - control.Height);
                        System.Drawing.Drawing2D.Matrix mx11 = new System.Drawing.Drawing2D.Matrix(1, 0, 0, 1, (destRectangle.Width * m_AnimationPercent / 100) - destRectangle.Width, (destRectangle.Height * m_AnimationPercent / 100) - destRectangle.Height);
                        g.Transform = mx11;
                        //g.DrawImage(m_AnimatedBitmap, control.ClientRectangle, 0, 0, m_AnimatedBitmap.Width, m_AnimatedBitmap.Height, GraphicsUnit.Pixel);
                        g.DrawImage(m_AnimatedBitmap, destRectangle, sourceRectangle, GraphicsUnit.Pixel);
                        mx11.Dispose();

                        break;

                    // --------------->
                    case AnimationTypes.TopRightToBottomLeft:
                        // Image slide from top right to bottom left effect

                        System.Drawing.Drawing2D.Matrix mx22 = new System.Drawing.Drawing2D.Matrix(1, 0, 0, 1, -(control.Width * m_AnimationPercent / 100) + control.Width, (control.Height * m_AnimationPercent / 100) - control.Height);
                        g.Transform = mx22;
                        //g.DrawImage(m_AnimatedBitmap, control.ClientRectangle, 0, 0, m_AnimatedBitmap.Width, m_AnimatedBitmap.Height, GraphicsUnit.Pixel);
                        g.DrawImage(m_AnimatedBitmap, destRectangle, sourceRectangle, GraphicsUnit.Pixel);
                        mx22.Dispose();

                        break;

                    // --------------->
                    case AnimationTypes.TopToDown:
                        // Image slide top to down effect

                        System.Drawing.Drawing2D.Matrix mx33 = new System.Drawing.Drawing2D.Matrix(1, 0, 0, 1, 0, (control.Height * m_AnimationPercent / 100) - control.Height);
                        g.Transform = mx33;
                        //g.DrawImage(m_AnimatedBitmap, control.ClientRectangle, 0, 0, m_AnimatedBitmap.Width, m_AnimatedBitmap.Height, GraphicsUnit.Pixel);
                        g.DrawImage(m_AnimatedBitmap, destRectangle, sourceRectangle, GraphicsUnit.Pixel);
                        mx33.Dispose();

                        break;

                    // --------------->
                    case AnimationTypes.SplitHorizontal:
                        // Image split horizontal effect

                        //left half
                        //g.DrawImage(m_AnimatedBitmap, new Rectangle(0, 0, Convert.ToInt32((control.Width * m_AnimationPercent / 200)), control.Height), 0, 0, Convert.ToInt32(m_AnimatedBitmap.Width / 2), m_AnimatedBitmap.Height, GraphicsUnit.Pixel);
                        Int32 leftDestWidth = Convert.ToInt32((destRectangle.Width * m_AnimationPercent / 200));
                        Rectangle destRectangleLclSplitH = new Rectangle(destRectangle.X, destRectangle.Y, leftDestWidth, destRectangle.Height);
                        ////sourceRectangle = new Rectangle(0, 0, Convert.ToInt32(sourceRectangle.Width / 2), sourceRectangle.Height);
                        Int32 leftSourceWidth = Convert.ToInt32(sourceRectangle.Width / 2);
                        Rectangle sourceRectangleLcl = new Rectangle(0, 0, Convert.ToInt32(sourceRectangle.Width / 2), sourceRectangle.Height);
                        g.DrawImage(m_AnimatedBitmap, destRectangleLclSplitH, sourceRectangleLcl, GraphicsUnit.Pixel);

                        //right half
                        //g.DrawImage(m_AnimatedBitmap, new Rectangle(Convert.ToInt32((control.Width - Convert.ToInt32(control.Width * m_AnimationPercent / 200))), 0, Convert.ToInt32((control.ClientRectangle.Width * m_AnimationPercent / 200)), control.ClientRectangle.Height), Convert.ToInt32(m_AnimatedBitmap.Width / 2), 0, Convert.ToInt32(m_AnimatedBitmap.Width / 2), m_AnimatedBitmap.Height, GraphicsUnit.Pixel);
                        destRectangleLclSplitH = new Rectangle(destRectangle.X + Convert.ToInt32((destRectangle.Width - Convert.ToInt32(destRectangle.Width * m_AnimationPercent / 200))), destRectangle.Y, Convert.ToInt32((destRectangle.Width * m_AnimationPercent / 200)), destRectangle.Height);
                        sourceRectangleLcl = new Rectangle(Convert.ToInt32(sourceRectangle.Width / 2), 0, Convert.ToInt32(sourceRectangle.Width / 2), sourceRectangle.Height);
                        if (m_AnimationPercent > 99)
                        {
                            //make sure the ends meet
                            sourceRectangleLcl.Width = sourceRectangle.Width - leftSourceWidth;
                            destRectangleLclSplitH.Width = destRectangle.Width - leftDestWidth;
                            destRectangleLclSplitH.X = destRectangle.X + leftDestWidth;
                        }
                        g.DrawImage(m_AnimatedBitmap, destRectangleLclSplitH, sourceRectangleLcl, GraphicsUnit.Pixel);

                        break;

                    // --------------->
                    case AnimationTypes.SplitQuarter:
                        // Image split quarter effect


                        g.DrawImage(m_AnimatedBitmap, new Rectangle(destRectangle.X, destRectangle.Y, 1 + Convert.ToInt32((destRectangle.Width * m_AnimationPercent / 200)), 1 + Convert.ToInt32((destRectangle.Height * m_AnimationPercent / 200))), 0, 0, Convert.ToInt32(m_AnimatedBitmap.Width / 2), Convert.ToInt32(m_AnimatedBitmap.Height / 2), GraphicsUnit.Pixel);
                        g.DrawImage(m_AnimatedBitmap, new Rectangle(destRectangle.X + Convert.ToInt32((destRectangle.Width - Convert.ToInt32(destRectangle.Width * m_AnimationPercent / 200))), destRectangle.Y, Convert.ToInt32((destRectangle.Width * m_AnimationPercent / 200)), Convert.ToInt32((destRectangle.Height * m_AnimationPercent / 200))), Convert.ToInt32(m_AnimatedBitmap.Width / 2), 0, Convert.ToInt32(m_AnimatedBitmap.Width / 2), Convert.ToInt32(m_AnimatedBitmap.Height / 2), GraphicsUnit.Pixel);
                        g.DrawImage(m_AnimatedBitmap, new Rectangle(destRectangle.X, destRectangle.Y + Convert.ToInt32((destRectangle.Height - Convert.ToInt32(destRectangle.Height * m_AnimationPercent / 200))), Convert.ToInt32((destRectangle.Width * m_AnimationPercent / 200)), Convert.ToInt32((destRectangle.Height * m_AnimationPercent / 200))), 0, Convert.ToInt32(m_AnimatedBitmap.Height / 2), Convert.ToInt32(m_AnimatedBitmap.Width / 2), Convert.ToInt32(m_AnimatedBitmap.Height / 2), GraphicsUnit.Pixel);
                        g.DrawImage(m_AnimatedBitmap, new Rectangle(destRectangle.X + Convert.ToInt32((destRectangle.Width - 1 - Convert.ToInt32(destRectangle.Width * m_AnimationPercent / 200))), destRectangle.Y - 1 + Convert.ToInt32((destRectangle.Height - Convert.ToInt32(destRectangle.Height * m_AnimationPercent / 200))), 1 + Convert.ToInt32((destRectangle.Width * m_AnimationPercent / 200)), 1 + Convert.ToInt32((destRectangle.Height * m_AnimationPercent / 200))), Convert.ToInt32(m_AnimatedBitmap.Width / 2), Convert.ToInt32(m_AnimatedBitmap.Height / 2), Convert.ToInt32(m_AnimatedBitmap.Width / 2), Convert.ToInt32(m_AnimatedBitmap.Height / 2), GraphicsUnit.Pixel);

                        break;

                    // --------------->
                    case AnimationTypes.SplitBoom:
                        // Image split shake effect

                        g.DrawImage(m_AnimatedBitmap, new Rectangle(destRectangle.X, destRectangle.Y, Convert.ToInt32((destRectangle.Width * m_AnimationPercent / 200)), destRectangle.Height), 0, 0, Convert.ToInt32(m_AnimatedBitmap.Width / 2), m_AnimatedBitmap.Height, GraphicsUnit.Pixel);
                        g.DrawImage(m_AnimatedBitmap, new Rectangle(destRectangle.X + Convert.ToInt32((destRectangle.Width - Convert.ToInt32(destRectangle.Width * m_AnimationPercent / 200))), destRectangle.Y, Convert.ToInt32((destRectangle.Width * m_AnimationPercent / 200)), destRectangle.Height), Convert.ToInt32(m_AnimatedBitmap.Width / 2), 0, Convert.ToInt32(m_AnimatedBitmap.Width / 2), m_AnimatedBitmap.Height, GraphicsUnit.Pixel);
                        g.DrawImage(m_AnimatedBitmap, new Rectangle(destRectangle.X, destRectangle.Y, destRectangle.Width, Convert.ToInt32((destRectangle.Height * m_AnimationPercent / 200))), 0, 0, m_AnimatedBitmap.Width, Convert.ToInt32(m_AnimatedBitmap.Height / 2), GraphicsUnit.Pixel);
                        g.DrawImage(m_AnimatedBitmap, new Rectangle(destRectangle.X, destRectangle.Y + Convert.ToInt32((destRectangle.Height - Convert.ToInt32(destRectangle.Height * m_AnimationPercent / 200))), destRectangle.Width, Convert.ToInt32((destRectangle.Height * m_AnimationPercent / 200))), 0, Convert.ToInt32(m_AnimatedBitmap.Height / 2), m_AnimatedBitmap.Width, Convert.ToInt32(m_AnimatedBitmap.Height / 2), GraphicsUnit.Pixel);

                        break;

                    // --------------->
                    case AnimationTypes.SplitVertical:
                        // Image split vertical effect

                        //Top half
                        Int32 topDestHeight = Convert.ToInt32((destRectangle.Height * m_AnimationPercent / 200));
                        Rectangle destRectangleLclSplitV = new Rectangle(destRectangle.X, destRectangle.Y, destRectangle.Width, topDestHeight);
                        ////sourceRectangle = new Rectangle(0, 0, Convert.ToInt32(sourceRectangle.Width / 2), sourceRectangle.Height);
                        Int32 topSourceHeight = Convert.ToInt32(sourceRectangle.Height / 2);
                        sourceRectangleLcl = new Rectangle(0, 0, sourceRectangle.Width, topSourceHeight);
                        g.DrawImage(m_AnimatedBitmap, destRectangleLclSplitV, sourceRectangleLcl, GraphicsUnit.Pixel);

                        //Bottom half
                        //g.DrawImage(m_AnimatedBitmap, new Rectangle(Convert.ToInt32((control.Width - Convert.ToInt32(control.Width * m_AnimationPercent / 200))), 0, Convert.ToInt32((control.ClientRectangle.Width * m_AnimationPercent / 200)), control.ClientRectangle.Height), Convert.ToInt32(m_AnimatedBitmap.Width / 2), 0, Convert.ToInt32(m_AnimatedBitmap.Width / 2), m_AnimatedBitmap.Height, GraphicsUnit.Pixel);
                        destRectangleLclSplitV = new Rectangle(destRectangle.X, destRectangle.Y + Convert.ToInt32((destRectangle.Height - Convert.ToInt32(destRectangle.Height * m_AnimationPercent / 200))), destRectangle.Width, Convert.ToInt32((destRectangle.Height * m_AnimationPercent / 200)));
                        sourceRectangleLcl = new Rectangle(0, Convert.ToInt32(sourceRectangle.Height / 2), sourceRectangle.Width, Convert.ToInt32(sourceRectangle.Height / 2));
                        if (m_AnimationPercent > 99)
                        {
                            //make sure the ends meet
                            sourceRectangleLcl.Height = sourceRectangle.Height - topSourceHeight;
                            destRectangleLclSplitV.Height = destRectangle.Height - topDestHeight;
                            destRectangleLclSplitV.Y = topDestHeight + destRectangle.Y;
                        }
                        g.DrawImage(m_AnimatedBitmap, destRectangleLclSplitV, sourceRectangleLcl, GraphicsUnit.Pixel);

                        //g.DrawImage(m_AnimatedBitmap, new Rectangle(0, 0, control.Width, Convert.ToInt32((control.Height * m_AnimationPercent / 200))), 0, 0, m_AnimatedBitmap.Width, Convert.ToInt32(m_AnimatedBitmap.Height / 2), GraphicsUnit.Pixel);

                        //g.DrawImage(m_AnimatedBitmap, new Rectangle(0, Convert.ToInt32((control.Height - Convert.ToInt32(control.Height * m_AnimationPercent / 200))), control.ClientRectangle.Width, Convert.ToInt32((control.ClientRectangle.Height * m_AnimationPercent / 200))), 0, Convert.ToInt32(m_AnimatedBitmap.Height / 2), m_AnimatedBitmap.Width, Convert.ToInt32(m_AnimatedBitmap.Height / 2), GraphicsUnit.Pixel);
                        break;

                    // --------------->
                    case AnimationTypes.Panorama:
                        // Image panorama effect

                        for (int y4 = 0; y4 <= m_Divider - 1; y4++)
                        {

                            for (int x = 0; x <= m_Divider - 1; x++)
                            {
                                Rectangle src = new Rectangle(x * (m_AnimatedBitmap.Width / m_Divider), y4 * (m_AnimatedBitmap.Height / m_Divider), m_AnimatedBitmap.Width / m_Divider, m_AnimatedBitmap.Height / m_Divider);


                                Rectangle drc = new Rectangle(destRectangle.X + x * (destRectangle.Width / m_Divider), destRectangle.Y + y4 * (destRectangle.Height / m_Divider), Convert.ToInt32(((destRectangle.Width / m_Divider) * m_AnimationPercent / 100)), Convert.ToInt32(((destRectangle.Height / m_Divider) * m_AnimationPercent / 100)));

                                drc.Offset((destRectangle.Width / (m_Divider * 2)) - drc.Width / 2, (destRectangle.Height / (m_Divider * 2)) - drc.Height / 2);

                                g.DrawImage(m_AnimatedBitmap, drc, src, GraphicsUnit.Pixel);

                            }
                        }


                        break;

                    // --------------->
                    case AnimationTypes.PanoramaHorizontal:
                        // Image panorama horizontal effect


                        for (int y5 = 0; y5 <= m_Divider - 1; y5++)
                        {
                            Rectangle src = new Rectangle(0, y5 * (m_AnimatedBitmap.Height / m_Divider), m_AnimatedBitmap.Width, m_AnimatedBitmap.Height / m_Divider);

                            Rectangle drc = new Rectangle(destRectangle.X, destRectangle.Y + y5 * (destRectangle.Height / m_Divider), destRectangle.Width, Convert.ToInt32(((destRectangle.Height / m_Divider) * m_AnimationPercent / 100)));

                            drc.Offset(0, (destRectangle.Height / (m_Divider * 2)) - drc.Height / 2);

                            g.DrawImage(m_AnimatedBitmap, drc, src, GraphicsUnit.Pixel);
                        }


                        break;

                    // --------------->
                    case AnimationTypes.PanoramaVertical:
                        // Image panorama vetical effect

                        for (int x = 0; x <= m_Divider - 1; x++)
                        {
                            Rectangle src = new Rectangle(x * (m_AnimatedBitmap.Width / m_Divider), 0, m_AnimatedBitmap.Width / m_Divider, m_AnimatedBitmap.Height);

                            Rectangle drc = new Rectangle(destRectangle.X + x * (destRectangle.Width / m_Divider), destRectangle.Y, Convert.ToInt32(((destRectangle.Width / m_Divider) * m_AnimationPercent / 100)), destRectangle.Height);

                            drc.Offset((destRectangle.Width / (m_Divider * 2)) - drc.Width / 2, 0);
                            g.DrawImage(m_AnimatedBitmap, drc, src, GraphicsUnit.Pixel);
                        }

                        break;

                    // --------------->
                    case AnimationTypes.Spiral:
                        // Image spiral effect


                        if (m_AnimationPercent < 100)
                        {
                            double percentageAngle = m_Divider * (Math.PI * 2) / 100;
                            double percentageDistance = Math.Max(control.Width, control.Height) / 100;
                            Path = new GraphicsPath(FillMode.Winding);

                            float cx = control.Width / 2;
                            float cy = control.Height / 2;

                            double pc1 = m_AnimationPercent - 100;
                            double pc2 = m_AnimationPercent;

                            if (pc1 < 0)
                            {
                                pc1 = 0;
                            }

                            double a = percentageAngle * pc2;
                            PointF last = new PointF(Convert.ToSingle((cx + (pc1 * percentageDistance * Math.Cos(a)))), Convert.ToSingle((cy + (pc1 * percentageDistance * Math.Sin(a)))));
                            a = percentageAngle * pc1;

                            while (pc1 <= pc2)
                            {
                                PointF thisPoint = new PointF(Convert.ToSingle((cx + (pc1 * percentageDistance * Math.Cos(a)))), Convert.ToSingle((cy + (pc1 * percentageDistance * Math.Sin(a)))));
                                Path.AddLine(last, thisPoint);
                                last = thisPoint;
                                pc1 += 0.1;
                                a += percentageAngle / 10;
                            }

                            Path.CloseFigure();
                            g.SetClip(Path, CombineMode.Replace);
                            Path.Dispose();

                        }

                        //g.DrawImage(m_AnimatedBitmap, control.ClientRectangle, 0, 0, m_AnimatedBitmap.Width, m_AnimatedBitmap.Height, GraphicsUnit.Pixel);
                        g.DrawImage(m_AnimatedBitmap, destRectangle, sourceRectangle, GraphicsUnit.Pixel);

                        break;

                    case AnimationTypes.SpiralBoom:
                        // Image spiral boom effect


                        if (m_AnimationPercent < 100)
                        {
                            double percentageAngle = m_Divider * (Math.PI * 2) / 100;
                            double percentageDistance = Math.Max(control.Width, control.Height) / 100;
                            Path = new GraphicsPath(FillMode.Winding);

                            float cx = control.Width / 2;
                            float cy = control.Height / 2;

                            double pc1 = m_AnimationPercent - 100;
                            double pc2 = m_AnimationPercent;

                            if (pc1 < 0)
                            {
                                pc1 = 0;
                            }

                            double a = percentageAngle * pc2;
                            PointF last = new PointF(Convert.ToSingle((cx + (pc1 * percentageDistance * Math.Cos(a)))), Convert.ToSingle((cy + (pc1 * percentageDistance * Math.Sin(a)))));
                            a = percentageAngle * pc1;

                            while (pc1 <= pc2)
                            {
                                PointF thisPoint = new PointF(Convert.ToSingle((cx + (pc1 * percentageDistance * Math.Cos(a)))), Convert.ToSingle((cy + (pc1 * percentageDistance * Math.Sin(a)))));
                                Path.AddLine(last, thisPoint);
                                last = thisPoint;
                                pc1 += 0.1;
                                a += percentageAngle / 10;
                            }

                            Path.CloseFigure();
                            g.SetClip(Path, CombineMode.Exclude);
                            Path.Dispose();

                        }

                        //g.DrawImage(m_AnimatedBitmap, control.ClientRectangle, 0, 0, m_AnimatedBitmap.Width, m_AnimatedBitmap.Height, GraphicsUnit.Pixel);
                        g.DrawImage(m_AnimatedBitmap, destRectangle, sourceRectangle, GraphicsUnit.Pixel);

                        break;

                }

                //show file name field
                if (m_showFileName)
                {
                    float padx = ((float)sourceRectangle.Size.Width) * (0.05F);
                    float pady = ((float)sourceRectangle.Size.Height) * (0.05F);

                    //float width = ((float)sourceRectangle.Size.Width) - 2 * padx;
                    //float height = ((float)sourceRectangle.Size.Height) - 2 * pady;;

                    float m_fontSize = 10;

                    //return to original transformation
                    g.Transform = originalTransform;
                    g.DrawString(imageName, new Font(FontFamily.GenericSansSerif, m_fontSize, FontStyle.Regular), new SolidBrush(Color.White), 0, 0);
                }
            }


            if (m_AnimationPercent >= 100)
            {
                AnimationStop();
            }

        }


        protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
        {
            base.OnPaint(e);
            
            DrawBorder(e.Graphics, this);
            DrawBackground(e.Graphics, this);
            
            if (AnimatedImage != null)
            {
                if (IsAnimating)
                {
                    DrawAnimatedImage(e.Graphics, this);
                }
                else
                {
                    // Draw the image normally
                    Rectangle destRectangle = this.ClientRectangle;
                    Rectangle sourceRectangle = new Rectangle(0, 0, AnimatedImage.Width, AnimatedImage.Height);
                    
                    // Preserve aspect ratio
                    float aspectRatio = ((float)AnimatedImage.Height / (float)AnimatedImage.Width);
                    int newHeight = (int)(destRectangle.Width * aspectRatio);
                    int newTopLeft = (destRectangle.Height - newHeight) / 2;
                    if (newTopLeft > 0)
                    {
                        destRectangle.Height = newHeight;
                        destRectangle.Y = newTopLeft;
                    }
                    else
                    {
                        int newWidth = (int)(destRectangle.Height / aspectRatio);
                        newWidth = Math.Min(newWidth, destRectangle.Width);
                        newTopLeft = (destRectangle.Width - newWidth) / 2;
                        destRectangle.Width = newWidth;
                        destRectangle.X = newTopLeft;
                    }
                    
                    e.Graphics.DrawImage(AnimatedImage, destRectangle, sourceRectangle, GraphicsUnit.Pixel);
                    
                    // Draw the file name if enabled
                    if (showFileName)
                    {
                        string displayText = GetFormattedFileName();
                        if (!string.IsNullOrEmpty(displayText))
                        {
                            // Create shadow effect
                            using (Brush shadowBrush = new SolidBrush(Color.FromArgb(64, 0, 0, 0)))
                            {
                                e.Graphics.DrawString(displayText, FileNameFont, shadowBrush, 11, 11);
                            }
                            
                            // Draw the actual text
                            using (Brush textBrush = new SolidBrush(FileNameColor))
                            {
                                e.Graphics.DrawString(displayText, FileNameFont, textBrush, 10, 10);
                            }
                        }
                    }
                }
            }
        }

        protected override void OnResize(System.EventArgs e)
        {
            base.OnResize(e);
            Invalidate();
        }

        #endregion

        #region " Function "

        private Color GetLightColor(Color colorIn, float percent)
        {
            if (percent < 0 || percent > 100)
            {
                throw new ArgumentOutOfRangeException("percent must be between 0 and 100");
            }
            Int32 a = Convert.ToInt32(colorIn.A * this.Opacity);
            Int32 r = colorIn.R + Convert.ToInt32(((255 - colorIn.R) / 100) * percent);
            Int32 g = colorIn.G + Convert.ToInt32(((255 - colorIn.G) / 100) * percent);
            Int32 b = colorIn.B + Convert.ToInt32(((255 - colorIn.B) / 100) * percent);
            return Color.FromArgb(a, r, g, b);
        }

        private Color GetDarkColor(Color colorIn, float percent)
        {
            if (percent < 0 || percent > 100)
            {
                throw new ArgumentOutOfRangeException("percent must be between 0 and 100");
            }
            Int32 a = Convert.ToInt32(colorIn.A * this.Opacity);
            Int32 r = colorIn.R - Convert.ToInt32((colorIn.R / 100) * percent);
            Int32 g = colorIn.G - Convert.ToInt32((colorIn.G / 100) * percent);
            Int32 b = colorIn.B - Convert.ToInt32((colorIn.B / 100) * percent);
            return Color.FromArgb(a, r, g, b);
        }

        #endregion

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ResumeLayout(false);

        }


        public string imageName { get; set; }

        private string GetFormattedFileName()
        {
            if (string.IsNullOrEmpty(imageName)) return string.Empty;

            switch (FileNameDisplayMode)
            {
                case 0: // Full path
                    return imageName;
                case 1: // Relative path
                    try
                    {
                        string rootPath = System.IO.Path.GetDirectoryName(Application.ExecutablePath);
                        // Make path relative manually
                        if (imageName.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
                        {
                            string relativePath = imageName.Substring(rootPath.Length);
                            if (relativePath.StartsWith(System.IO.Path.DirectorySeparatorChar.ToString()) || 
                                relativePath.StartsWith(System.IO.Path.AltDirectorySeparatorChar.ToString()))
                            {
                                relativePath = relativePath.Substring(1);
                            }
                            return relativePath;
                        }
                        return imageName;
                    }
                    catch
                    {
                        return System.IO.Path.GetFileName(imageName);
                    }
                case 2: // File name only
                default:
                    return System.IO.Path.GetFileName(imageName);
            }
        }
    }
}
