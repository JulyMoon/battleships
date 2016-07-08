using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace BattleshipsClient
{
    public partial class MainWindow : Form
    {
        private const int boardWidth = Battleships.BoardWidth;
        private const int boardHeight = Battleships.BoardHeight;
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

        private readonly Battleships game = new Battleships();
        private readonly List<Battleships.Ship.Properties> currentShips = new List<Battleships.Ship.Properties>();
        private bool dragging;
        private int dragOffsetX;
        private int dragOffsetY;
        private int shipLength = 4;
        private int shipX;
        private int shipY;
        private bool shipVertical;
        private bool snapping;
        private int snapX;
        private int snapY;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            UpdateShipPosition();
        }

        private void UpdateShipPosition()
        {
            int shipWidth, shipHeight;
            Battleships.GetShipDimensions(shipVertical, shipLength, out shipWidth, out shipHeight);
            ScaleUp(ref shipWidth, ref shipHeight);

            shipX = shipWindowX + (shipWindowSize - shipWidth) / 2;
            shipY = shipWindowY + (shipWindowSize - shipHeight) / 2;
        }

        private static void ScaleUp(ref int width, ref int height)
        {
            width *= cellSize;
            height *= cellSize;
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
            Battleships.GetShipDimensions(vertical, length, out shipWidth, out shipHeight);
            ScaleUp(ref shipWidth, ref shipHeight);

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

        private static void DrawShips(Graphics g, Brush fillBrush, Pen outlinePen, List<Battleships.Ship.Properties> ships, int boardx, int boardy)
        {
            foreach (var ship in ships)
                DrawShip(g, fillBrush, outlinePen, ship.IsVertical, ship.Size, boardx + ship.X * cellSize, boardy + ship.Y * cellSize);
        }

        private void MainWindow_Paint(object sender, PaintEventArgs e)
        {
            //gfx.Clear(backgroundColor);

            DrawBoard(e.Graphics, boardTextBrush, boardFont, boardLinePen, boardX, boardY);
            DrawShipWindow(e.Graphics, shipWindowPen, shipWindowX, shipWindowY, shipWindowSize);

            DrawShips(e.Graphics, shipFillBrush, shipOutlinePen, currentShips, boardX, boardY);

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

            DrawShip(e.Graphics, shipFillBrush, snapping ? shipHighlightPen : shipOutlinePen, shipVertical, shipLength, shipx, shipy);
        }

        private void MainWindow_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                int shipWidth, shipHeight;
                Battleships.GetShipDimensions(shipVertical, shipLength, out shipWidth, out shipHeight);
                ScaleUp(ref shipWidth, ref shipHeight);

                if (!dragging && e.X >= shipX && e.X <= shipX + shipWidth && e.Y >= shipY && e.Y <= shipY + shipHeight)
                {
                    dragOffsetX = e.X - shipX;
                    dragOffsetY = e.Y - shipY;
                    dragging = true;
                }
            }
            else if (e.Button == MouseButtons.Right && dragging)
            {
                shipVertical = !shipVertical;
                UpdateShipPosition();

                int tmp = dragOffsetX;
                dragOffsetX = dragOffsetY;
                dragOffsetY = tmp;
            }
        }

        private void MainWindow_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (snapping)
                {
                    currentShips.Add(new Battleships.Ship.Properties(shipLength, shipVertical, snapX, snapY));
                }

                snapping = false;
                dragging = false;

                Invalidate();
            }

        }

        private void MainWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                Snap(e.X, e.Y, dragOffsetX, dragOffsetY, shipLength, shipVertical, out snapping, out snapX, out snapY);
                Invalidate();
            }
        }

        private static void Snap(int mx, int my, int offsetX, int offsetY, int shiplength, bool shipvertical, out bool snapping, out int snapX, out int snapY)
        {
            // concise version
            //double adjustedOffsetX = (Math.Floor((double)offsetX / cellSize) + 0.5) * cellSize;
            //double adjustedOffsetY = (Math.Floor((double)offsetY / cellSize) + 0.5) * cellSize;

            // performance friendly version
            double adjustedOffsetX, adjustedOffsetY;
            if (shipvertical)
            {
                adjustedOffsetX = cellSize / 2d;
                adjustedOffsetY = (Math.Floor((double)offsetY / cellSize) + 0.5) * cellSize;
            }
            else
            {
                adjustedOffsetX = (Math.Floor((double)offsetX / cellSize) + 0.5) * cellSize;
                adjustedOffsetY = cellSize / 2d;
            }

            snapX = (int)Math.Round((mx - adjustedOffsetX - boardX) / cellSize);
            snapY = (int)Math.Round((my - adjustedOffsetY - boardY) / cellSize);

            snapping = Battleships.WithinBoard(shipvertical, shiplength, snapX, snapY);
        }
    }
}
