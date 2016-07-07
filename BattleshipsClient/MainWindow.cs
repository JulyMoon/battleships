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
        private const int cellSize = 20;
        private const int shipWindowSize = cellSize * 5;
        private const int shipWindowX = boardX + (boardWidth + 1) * cellSize;
        private const int shipWindowY = boardY;

        //private static readonly Color backgroundColor = Color.White;
        private static readonly Pen boardLinePen = Pens.LightSkyBlue;
        private static readonly Brush boardTextBrush = Brushes.DodgerBlue;
        private static readonly Brush shipFillBrush = Brushes.SpringGreen;
        private static readonly Pen shipOutlinePen = Pens.Black;
        private static readonly Pen shipWindowPen = Pens.Navy;
        private static readonly Font boardFont = new Font(FontFamily.GenericMonospace, 13);

        private Graphics gfx;
        private bool dragging = false;
        private int dragOffsetX;
        private int dragOffsetY;
        private int shipLength = 4;
        private int shipX;
        private int shipY;
        private bool shipVertical = true;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            int shipWidth;
            int shipHeight;
            GetShipDimensions(out shipWidth, out shipHeight);

            shipX = shipWindowX + (shipWindowSize - shipWidth) / 2;
            shipY = shipWindowY + (shipWindowSize - shipHeight) / 2;

            //SetDoubleBuffered(this);
        }

        private void GetShipDimensions(out int shipW, out int shipH)
        {
            if (shipVertical)
            {
                shipW = cellSize;
                shipH = shipLength * cellSize;
            }
            else
            {
                shipW = shipLength * cellSize;
                shipH = cellSize;
            }
        }

        private void MainWindow_Shown(object sender, EventArgs e)
        {

        }

        private static void FreeDrawShipCell(Graphics g, Brush fillBrush, Pen outlinePen, int x, int y, int cellsize)
        {
            /*gfx.DrawRectangle(pen, x, y, cellSize, cellSize);

            const int k = 2; // assert: cellSize % k == 0

            for (int i = 0; i < cellSize / k; i++)
            {
                int ax = x + i * k;
                int ay = y + i * k;
                gfx.DrawLine(pen, ax, y, x, ay);
                gfx.DrawLine(pen, ax, y + cellSize, x + cellSize, ay);
            }*/

            var ship = new Rectangle(x, y, cellsize, cellsize);
            g.FillRectangle(fillBrush, ship);
            g.DrawRectangle(outlinePen, ship);
        }

        private static void FreeDrawShip(Graphics g, Brush fillBrush, Pen outlinePen, bool vertical, int length, int x, int y, int cellsize)
        {
            for (int i = 0; i < length; i++)
            {
                if (vertical)
                    FreeDrawShipCell(g, fillBrush, outlinePen, x, y + i * cellsize, cellsize);
                else
                    FreeDrawShipCell(g, fillBrush, outlinePen, x + i * cellsize, y, cellsize);
            }
        }

        private static void DrawBoard(Graphics g, Brush textBrush, Font textFont, Pen linePen, int boardx, int boardy, int boardwidth, int boardheight, int cellsize)
        {
            for (int i = 0; i <= boardwidth; i++)
            {
                int x = boardx + i * cellsize;
                g.DrawLine(linePen, x, boardy, x, boardy + boardheight * cellsize);
            }

            for (int i = 0; i <= boardheight; i++)
            {
                int y = boardy + i * cellsize;
                g.DrawLine(linePen, boardx, y, boardx + boardwidth * cellsize, y);
            }

            for (int i = 0; i < boardwidth; i++)
            {
                var number = (i + 1).ToString();
                g.DrawString(number, textFont, textBrush, boardx + i * cellsize + (number.Length == 1 ? 3 : -3), boardy - cellsize);
            }

            for (int i = 0; i < boardHeight; i++)
                g.DrawString(((char)('A' + i)).ToString(), textFont, textBrush, boardx - cellsize, boardy + i * cellsize);
        }

        private static void DrawShipWindow(Graphics g, Pen pen, int x, int y, int size) => g.DrawRectangle(pen, x, y, size, size);

        private void MainWindow_Paint(object sender, PaintEventArgs e)
        {
            gfx = e.Graphics;
            //gfx.Clear(backgroundColor);
            DrawBoard(gfx, boardTextBrush, boardFont, boardLinePen, boardX, boardY, boardWidth, boardHeight, cellSize);
            DrawShipWindow(gfx, shipWindowPen, shipWindowX, shipWindowY, shipWindowSize);
            if (dragging)
            {
                var mousePosition = PointToClient(Cursor.Position);
                FreeDrawShip(gfx, shipFillBrush, shipOutlinePen, shipVertical, shipLength, mousePosition.X - dragOffsetX, mousePosition.Y - dragOffsetY, cellSize);
            }
            else
            {
                FreeDrawShip(gfx, shipFillBrush, shipOutlinePen, shipVertical, shipLength, shipX, shipY, cellSize);
            }
        }

        private void MainWindow_MouseDown(object sender, MouseEventArgs e)
        {
            int shipWidth;
            int shipHeight;
            GetShipDimensions(out shipWidth, out shipHeight);

            if (e.Button == MouseButtons.Left && e.X > shipX && e.X < shipX + shipWidth && e.Y > shipY && e.Y < shipY + shipHeight)
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
            Invalidate();
        }
    }
}
