using System.Drawing;
using System.Windows.Forms;
using System;
using System.Collections.Generic;

namespace ConvexHullRectangleApp
{
    partial class Form1 : Form
    {
        private System.ComponentModel.IContainer components = null;
        private List<PointF> points = new List<PointF>();
        private GrahamScan grahamScan;
        private PointF[] hull = null;
        private List<PointF> rectanglePoints = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void btnComputeHull_Click(object sender, EventArgs e)
        {
            if (points.Count < 3)
            {
                MessageBox.Show("Please add at least three points.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            grahamScan = new GrahamScan(points.ToArray());
            hull = grahamScan.Run();
            rectanglePoints = null;
            pictureBox1.Invalidate();
        }

        private void btnClearPoints_Click(object sender, EventArgs e)
        {
            points.Clear();
            hull = null;
            rectanglePoints = null;
            pictureBox1.Invalidate();
        }

        private void btnRandomPoints_Click(object sender, EventArgs e)
        {
            int numberOfPoints;
            if (int.TryParse(txtNumberOfPoints.Text, out numberOfPoints))
            {
                Random random = new Random();
                points.Clear();
                for (int i = 0; i < numberOfPoints; i++)
                {
                    float x = random.Next(10, pictureBox1.Width - 10);
                    float y = random.Next(10, pictureBox1.Height - 10);
                    points.Add(new PointF(x, y));
                }
                hull = null;
                rectanglePoints = null;
                pictureBox1.Invalidate();
            }
            else
            {
                MessageBox.Show("Please enter a valid number of points.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            // Draw coordinate system
            DrawCoordinateSystem(g);

            // Draw points
            foreach (var point in points)
            {
                PointF adjustedPoint = AdjustPointCoordinates(point);
                g.FillEllipse(Brushes.Black, adjustedPoint.X - 2, adjustedPoint.Y - 2, 4, 4);
                g.DrawString($"({point.X},{point.Y})", new Font("Arial", 8), Brushes.Blue, new PointF(adjustedPoint.X + 5, adjustedPoint.Y - 15));
            }

            // Draw hull
            if (hull != null)
            {
                DrawHull(hull, g);
            }

            // Draw rectangle
            if (rectanglePoints != null)
            {
                DrawRectangle(rectanglePoints, g);
            }
        }

        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            points.Add(AdjustPointCoordinates(new PointF(e.X, e.Y)));
            pictureBox1.Invalidate();
        }

        private PointF AdjustPointCoordinates(PointF point)
        {
            return new PointF(point.X, pictureBox1.Height - point.Y);
        }

        private void DrawCoordinateSystem(Graphics g)
        {
            Pen axisPen = new Pen(Color.Gray, 1);
            Pen gridPen = new Pen(Color.LightGray, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };

            int step = 20; // Grid step in pixels

            // Draw vertical grid lines
            for (int x = 0; x < pictureBox1.Width; x += step)
            {
                g.DrawLine(gridPen, x, 0, x, pictureBox1.Height);
            }

            // Draw horizontal grid lines
            for (int y = 0; y < pictureBox1.Height; y += step)
            {
                g.DrawLine(gridPen, 0, y, pictureBox1.Width, y);
            }

            // Draw X and Y axis
            g.DrawLine(axisPen, 0, pictureBox1.Height / 2, pictureBox1.Width, pictureBox1.Height / 2); // X axis
            g.DrawLine(axisPen, pictureBox1.Width / 2, 0, pictureBox1.Width / 2, pictureBox1.Height); // Y axis

            // Draw axis labels
            g.DrawString("X", new Font("Arial", 10), Brushes.Black, pictureBox1.Width - 20, pictureBox1.Height / 2 - 20);
            g.DrawString("Y", new Font("Arial", 10), Brushes.Black, pictureBox1.Width / 2 + 5, 5);
        }

        private void DrawHull(PointF[] hull, Graphics g)
        {
            if (hull.Length < 2)
                return;

            Pen pen = new Pen(Color.Green, 2);
            for (int i = 0; i < hull.Length - 1; i++)
            {
                g.DrawLine(pen, AdjustPointCoordinates(hull[i]), AdjustPointCoordinates(hull[i + 1]));
            }
            g.DrawLine(pen, AdjustPointCoordinates(hull[hull.Length - 1]), AdjustPointCoordinates(hull[0]));
        }

        private void DrawRectangle(List<PointF> rectangle, Graphics g)
        {
            if (rectangle.Count != 4)
                return;

            Pen pen = new Pen(Color.Red, 2);
            for (int i = 0; i < rectangle.Count; i++)
            {
                g.DrawLine(pen, AdjustPointCoordinates(rectangle[i]), AdjustPointCoordinates(rectangle[(i + 1) % 4]));
            }
        }

        private void btnDrawRectangle_Click(object sender, EventArgs e)
        {
            if (points.Count < 3)
            {
                MessageBox.Show("Please add at least three points.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            grahamScan = new GrahamScan(points.ToArray());
            hull = grahamScan.Run();
            rectanglePoints = grahamScan.solveGetMaximumAxisAlignedRectangle();
            pictureBox1.Invalidate();
        }

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnComputeHull = new System.Windows.Forms.Button();
            this.btnClearPoints = new System.Windows.Forms.Button();
            this.btnRandomPoints = new System.Windows.Forms.Button();
            this.txtNumberOfPoints = new System.Windows.Forms.TextBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.btnDrawRectangle = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();

            // 
            // btnComputeHull
            // 
            this.btnComputeHull.Location = new System.Drawing.Point(12, 12);
            this.btnComputeHull.Name = "btnComputeHull";
            this.btnComputeHull.Size = new System.Drawing.Size(100, 23);
            this.btnComputeHull.TabIndex = 0;
            this.btnComputeHull.Text = "Compute Hull";
            this.btnComputeHull.UseVisualStyleBackColor = true;
            this.btnComputeHull.Click += new System.EventHandler(this.btnComputeHull_Click);

            // 
            // btnClearPoints
            // 
            this.btnClearPoints.Location = new System.Drawing.Point(118, 12);
            this.btnClearPoints.Name = "btnClearPoints";
            this.btnClearPoints.Size = new System.Drawing.Size(100, 23);
            this.btnClearPoints.TabIndex = 1;
            this.btnClearPoints.Text = "Clear Points";
            this.btnClearPoints.UseVisualStyleBackColor = true;
            this.btnClearPoints.Click += new System.EventHandler(this.btnClearPoints_Click);

            // 
            // btnRandomPoints
            // 
            this.btnRandomPoints.Location = new System.Drawing.Point(224, 12);
            this.btnRandomPoints.Name = "btnRandomPoints";
            this.btnRandomPoints.Size = new System.Drawing.Size(125, 23);
            this.btnRandomPoints.TabIndex = 2;
            this.btnRandomPoints.Text = "Random Points";
            this.btnRandomPoints.UseVisualStyleBackColor = true;
            this.btnRandomPoints.Click += new System.EventHandler(this.btnRandomPoints_Click);

            // 
            // txtNumberOfPoints
            // 
            this.txtNumberOfPoints.Location = new System.Drawing.Point(355, 14);
            this.txtNumberOfPoints.Name = "txtNumberOfPoints";
            this.txtNumberOfPoints.Size = new System.Drawing.Size(50, 20);
            this.txtNumberOfPoints.TabIndex = 3;

            // 
            // pictureBox1
            // 
            this.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBox1.Location = new System.Drawing.Point(12, 41);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(1080, 720);
            this.pictureBox1.TabIndex = 4;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.Paint += new System.Windows.Forms.PaintEventHandler(this.pictureBox1_Paint);
            this.pictureBox1.MouseClick += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseClick);

            // 
            // btnDrawRectangle
            // 
            this.btnDrawRectangle.Location = new System.Drawing.Point(12, 770);
            this.btnDrawRectangle.Name = "btnDrawRectangle";
            this.btnDrawRectangle.Size = new System.Drawing.Size(120, 23);
            this.btnDrawRectangle.TabIndex = 5;
            this.btnDrawRectangle.Text = "Draw Rectangle";
            this.btnDrawRectangle.UseVisualStyleBackColor = true;
            this.btnDrawRectangle.Click += new System.EventHandler(this.btnDrawRectangle_Click);

            // 
            // Form1
            // 
            this.ClientSize = new System.Drawing.Size(1104, 811);
            this.Controls.Add(this.btnDrawRectangle);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.txtNumberOfPoints);
            this.Controls.Add(this.btnRandomPoints);
            this.Controls.Add(this.btnClearPoints);
            this.Controls.Add(this.btnComputeHull);
            this.Name = "Form1";
            this.Text = "Convex Hull App";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Button btnComputeHull;
        private System.Windows.Forms.Button btnClearPoints;
        private System.Windows.Forms.Button btnRandomPoints;
        private System.Windows.Forms.TextBox txtNumberOfPoints;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Button btnDrawRectangle;
    }
}
