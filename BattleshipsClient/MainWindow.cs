using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BattleshipsClient
{
    public partial class MainWindow : Form
    {
        private const int boardWidth = 10;
        private const int boardHeight = boardWidth;
        private const int boardX = 40;
        private const int boardY = 40;
        private const int cellSize = 25;
        private const int shipWindowSize = cellSize * 5;
        private const int shipWindowX = boardX + (boardWidth + 1) * cellSize;
        private const int shipWindowY = boardY;

        //private static readonly Color backgroundColor = Color.White;
        private static readonly Pen boardLinePen = new Pen(Color.FromArgb(180, 180, 255));
        private static readonly Brush boardTextBrush = Brushes.Black;
        private static readonly Brush shipFillBrush = new SolidBrush(Color.FromArgb(20, Color.Black));
        private static readonly Pen shipOutlinePen = new Pen(Color.Blue, 2);
        private static readonly Pen shipHighlightPen = new Pen(Color.Green, shipOutlinePen.Width);
        private static readonly Pen shipWindowPen = Pens.Navy;
        private static readonly Font boardFont = new Font("Consolas", 10);

        private Graphics gfx;
        private bool dragging = false;
        private int dragOffsetX;
        private int dragOffsetY;
        private int shipLength = 4;
        private int shipX;
        private int shipY;
        private bool shipVertical = true;
        private bool snapping = false;
        private int snapX;
        private int snapY;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            int shipWidth, shipHeight;
            GetShipDimensions(shipVertical, shipLength, false, out shipWidth, out shipHeight);

            shipX = shipWindowX + (shipWindowSize - shipWidth) / 2;
            shipY = shipWindowY + (shipWindowSize - shipHeight) / 2;
        }

        private static void GetShipDimensions(bool vertical, int length, bool board, out int shipW, out int shipH)
        {
            if (vertical)
            {
                shipW = 1;
                shipH = length;
            }
            else
            {
                shipW = length;
                shipH = 1;
            }

            if (!board)
            {
                shipW *= cellSize;
                shipH *= cellSize;
            }
        }

        private void MainWindow_Shown(object sender, EventArgs e)
        {

        }

        /*private static void DrawShipCell(Graphics g, Brush fillBrush, Pen outlinePen, int x, int y)
        {
            var ship = new Rectangle(x, y, cellSize, cellSize);
            g.FillRectangle(fillBrush, ship);
            g.DrawRectangle(outlinePen, ship);
        }*/

        private static void DrawShip(Graphics g, Brush fillBrush, Pen outlinePen, bool vertical, int length, int x, int y)
        {
            int shipWidth, shipHeight;
            GetShipDimensions(vertical, length, false, out shipWidth, out shipHeight);

            var ship = new Rectangle(x, y, shipWidth, shipHeight);
            g.FillRectangle(fillBrush, ship);
            g.DrawRectangle(outlinePen, ship);
        }

        private static void DrawBoard(Graphics g, Brush textBrush, Font textFont, Pen linePen, int boardx, int boardy)
        {
            for (int i = 0; i <= boardWidth; i++)
            {
                int x = boardx + i * cellSize;
                g.DrawLine(linePen, x, boardy, x, boardy + boardHeight * cellSize);
            }

            for (int i = 0; i <= boardHeight; i++)
            {
                int y = boardy + i * cellSize;
                g.DrawLine(linePen, boardx, y, boardx + boardWidth * cellSize, y);
            }

            for (int i = 0; i < boardWidth; i++)
                g.DrawString(((char)('A' + i)).ToString(), textFont, textBrush, boardx + i * cellSize + 7, boardy - cellSize + 3);

            for (int i = 0; i < boardHeight; i++)
            {
                var number = (i + 1).ToString();
                g.DrawString(number, textFont, textBrush, boardx - cellSize + (number.Length == 1 ? 4 : 0), boardy + i * cellSize + 4);
            }
        }

        private static void DrawShipWindow(Graphics g, Pen pen, int x, int y, int size) => g.DrawRectangle(pen, x, y, size, size);

        private void MainWindow_Paint(object sender, PaintEventArgs e)
        {
            gfx = e.Graphics;
            //gfx.Clear(backgroundColor);
            DrawBoard(gfx, boardTextBrush, boardFont, boardLinePen, boardX, boardY);
            DrawShipWindow(gfx, shipWindowPen, shipWindowX, shipWindowY, shipWindowSize);

            int shipx, shipy;
            if (dragging)
            {
                if (snapping)
                {
                    shipx = boardX + snapX * cellSize;
                    shipy = boardY + snapY * cellSize;
                }
                else
                {
                    var mousePosition = PointToClient(Cursor.Position);
                    shipx = mousePosition.X - dragOffsetX;
                    shipy = mousePosition.Y - dragOffsetY;
                }
            }
            else
            {
                shipx = shipX;
                shipy = shipY;
            }

            DrawShip(gfx, shipFillBrush, snapping ? shipHighlightPen : shipOutlinePen, shipVertical, shipLength, shipx, shipy);
        }

        private void MainWindow_MouseDown(object sender, MouseEventArgs e)
        {
            int shipWidth, shipHeight;
            GetShipDimensions(shipVertical, shipLength, false, out shipWidth, out shipHeight);

            if (!dragging && e.Button == MouseButtons.Left && e.X >= shipX && e.X <= shipX + shipWidth && e.Y >= shipY && e.Y <= shipY + shipHeight)
            {
                dragOffsetX = e.X - shipX;
                dragOffsetY = e.Y - shipY;
                dragging = true;
            }
        }

        private void MainWindow_MouseUp(object sender, MouseEventArgs e)
        {

        }

        private void MainWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                Snap(e.X, e.Y, dragOffsetX, dragOffsetY, shipLength, shipVertical, out snapping, out snapX, out snapY);
                Invalidate();
            }
        }

        private void Snap(int mx, int my, int offsetX, int offsetY, int shiplength, bool shipvertical, out bool snapping, out int snapX, out int snapY)
        {
            double adjustedOffsetX = (Math.Floor((double)offsetX / cellSize) + 0.5) * cellSize;
            double adjustedOffsetY = (Math.Floor((double)offsetY / cellSize) + 0.5) * cellSize;

            snapX = (int)Math.Round((mx - adjustedOffsetX - boardX) / cellSize);
            snapY = (int)Math.Round((my - adjustedOffsetY - boardY) / cellSize);

            int shipW, shipH;
            GetShipDimensions(shipvertical, shiplength, true, out shipW, out shipH);
            
            snapping = snapX >= 0 && snapX < boardWidth &&
                       snapY >= 0 && snapY < boardHeight &&
                       snapX + shipW - 1 < boardWidth &&
                       snapY + shipH - 1 < boardHeight;
        }
    }
}
