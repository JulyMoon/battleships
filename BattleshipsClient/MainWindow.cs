using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
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
        private const int shipWindowX = boardX + (boardWidth + 1) * cellSize;
        private const int shipWindowY = boardY;
        private const int shipWindowPadding = 20;
        private const int shipWindowShipMargin = 15;

        //private static readonly Color backgroundColor = Color.White;
        private static readonly Pen boardLinePen = new Pen(Color.FromArgb(180, 180, 255));
        private static readonly Brush boardTextBrush = Brushes.Black;
        private static readonly Brush shipFillBrush = Brushes.Transparent;
        private static readonly Pen shipOutlinePen = new Pen(Color.Blue, 2);
        private static readonly Pen shipHighlightPen = new Pen(Color.Green, shipOutlinePen.Width);
        private static readonly Pen shipWindowPen = boardLinePen;
        private static readonly Font boardFont = new Font("Consolas", 10);

        private readonly Battleships game = new Battleships();
        private readonly List<Tuple<Battleships.Ship.Properties, bool>> currentShips = new List<Tuple<Battleships.Ship.Properties, bool>>();
        private readonly List<Tuple<Battleships.Ship.Properties, bool>> setShips = new List<Tuple<Battleships.Ship.Properties, bool>>();
        //                                                        ^ this bool represents whether the ship was used or not

        private bool draggingSetShip;
        private int dragIndex;
        private bool dragging;
        private int dragOffsetX;
        private int dragOffsetY;
        private Battleships.Ship.Properties drag;

        private bool snapping;
        private int snapX;
        private int snapY;

        private int shipWindowWidth;
        private int shipWindowHeight;

        public MainWindow()
        {
            InitializeComponent();
            AdoptShipSet(Battleships.ShipSet);
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {

        }

        private void MainWindow_Shown(object sender, EventArgs e)
        {

        }

        private void AdoptShipSet(ReadOnlyCollection<int> shipSet)
        {
            setShips.Clear();

            var shipTypes = shipSet.Distinct().OrderByDescending(size => size).ToList();

            int count = 0;
            int previousType = -1;
            foreach (int shipSize in shipSet)
            {
                if (previousType == shipSize)
                {
                    count++;
                }
                else
                {
                    count = 0;
                    previousType = shipSize;
                }

                int x = shipWindowX + shipWindowPadding + count * (shipSize * cellSize + shipWindowShipMargin);
                int y = shipWindowY + shipWindowPadding + shipTypes.IndexOf(shipSize) * (cellSize + shipWindowShipMargin);

                int width = shipWindowPadding * 2 + (count + 1) * (shipSize * cellSize + shipWindowShipMargin) - shipWindowShipMargin;
                if (width > shipWindowWidth)
                    shipWindowWidth = width;

                setShips.Add(Tuple.Create(new Battleships.Ship.Properties(shipSize, false, x, y), false));
            }
            
            shipWindowHeight = shipWindowPadding * 2 + shipTypes.Count * (cellSize + shipWindowShipMargin) - shipWindowShipMargin;
        }

        private static Rectangle GetShipRectangle(Battleships.Ship.Properties shipProps)
        {
            int shipWidth, shipHeight;
            Battleships.GetShipDimensions(shipProps.IsVertical, shipProps.Size, out shipWidth, out shipHeight);
            ScaleUp(ref shipWidth, ref shipHeight);

            return new Rectangle(shipProps.X, shipProps.Y, shipWidth, shipHeight);
        }

        private static void ScaleUp(ref int width, ref int height)
        {
            width *= cellSize;
            height *= cellSize;
        }

        private static void DrawShip(Graphics g, Brush fillBrush, Pen outlinePen, Battleships.Ship.Properties shipProps)
        {
            var shipRect = GetShipRectangle(shipProps);
            g.FillRectangle(fillBrush, shipRect);
            g.DrawRectangle(outlinePen, shipRect);
        }

        private static void DrawShip(Graphics g, Brush fillBrush, Pen outlinePen, Battleships.Ship ship)
        {
            throw new NotImplementedException();
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

        private static void DrawShipWindow(Graphics g, Pen pen, int x, int y, int width, int height) => g.DrawRectangle(pen, x, y, width, height);

        private static void DrawBoardShips(Graphics g, Brush fillBrush, Pen outlinePen, IEnumerable<Battleships.Ship.Properties> ships, int boardx, int boardy)
        {
            foreach (var ship in ships)
            {
                DrawShip(g, fillBrush, outlinePen, new Battleships.Ship.Properties(ship.Size, ship.IsVertical, boardx + ship.X * cellSize, boardy + ship.Y * cellSize));
            }
        }

        private static void DrawFreeShips(Graphics g, Brush fillBrush, Pen outlinePen, IEnumerable<Battleships.Ship.Properties> ships)
        {
            foreach (var ship in ships)
            {
                DrawShip(g, fillBrush, outlinePen, new Battleships.Ship.Properties(ship.Size, ship.IsVertical, ship.X, ship.Y));
            }
        }

        private void MainWindow_Paint(object sender, PaintEventArgs e)
        {
            //gfx.Clear(backgroundColor);

            DrawBoard(e.Graphics, boardTextBrush, boardFont, boardLinePen, boardX, boardY);
            DrawShipWindow(e.Graphics, shipWindowPen, shipWindowX, shipWindowY, shipWindowWidth, shipWindowHeight);

            DrawBoardShips(e.Graphics, shipFillBrush, shipOutlinePen, currentShips.Where(tuple => !tuple.Item2).Select(tuple => tuple.Item1), boardX, boardY);
            DrawFreeShips(e.Graphics, shipFillBrush, shipOutlinePen, setShips.Where(tuple => !tuple.Item2).Select(tuple => tuple.Item1));

            if (dragging)
            {
                int dragx, dragy;
                if (snapping)
                {
                    dragx = boardX + snapX * cellSize;
                    dragy = boardY + snapY * cellSize;
                }
                else
                {
                    var mousePosition = PointToClient(Cursor.Position);
                    dragx = mousePosition.X - dragOffsetX;
                    dragy = mousePosition.Y - dragOffsetY;
                }

                DrawShip(e.Graphics, shipFillBrush, snapping ? shipHighlightPen : shipOutlinePen, new Battleships.Ship.Properties(drag.Size, drag.IsVertical, dragx, dragy));
            }
        }

        private static bool IntersectsWithMouse(MouseEventArgs e, Rectangle rect) => rect.IntersectsWith(new Rectangle(e.X, e.Y, 1, 1));

        private void MainWindow_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            bool intersectionFound = false;
            Battleships.Ship.Properties ship = null, absoluteShip = null;
            for (int i = 0; i < setShips.Count; i++)
            {
                if (setShips[i].Item2)
                    continue;

                ship = setShips[i].Item1;
                absoluteShip = ship;

                if (!IntersectsWithMouse(e, GetShipRectangle(absoluteShip)))
                    continue;

                intersectionFound = true;
                draggingSetShip = true;
                dragIndex = i;
                break;
            }

            if (!intersectionFound)
            {
                for (int i = 0; i < currentShips.Count; i++)
                {
                    if (currentShips[i].Item2)
                        continue;

                    ship = currentShips[i].Item1;
                    absoluteShip = new Battleships.Ship.Properties(ship.Size, ship.IsVertical, boardX + ship.X * cellSize, boardY + ship.Y * cellSize);

                    if (!IntersectsWithMouse(e, GetShipRectangle(absoluteShip)))
                        continue;

                    intersectionFound = true;
                    draggingSetShip = false;
                    dragIndex = i;
                    break;
                }
            }

            if (!intersectionFound)
                return;

            var usedShip = Tuple.Create(ship, true);
            if (draggingSetShip)
                setShips[dragIndex] = usedShip;
            else
                currentShips[dragIndex] = usedShip;

            drag = absoluteShip.Clone();
            dragOffsetX = e.X - drag.X;
            dragOffsetY = e.Y - drag.Y;
            dragging = true;

            SnapInvalidate(e);
        }

        private void MainWindow_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (snapping)
                {
                    currentShips.Add(Tuple.Create(new Battleships.Ship.Properties(drag.Size, drag.IsVertical, snapX, snapY), false));
                }
                else if (dragging)
                {
                    if (draggingSetShip)
                        setShips[dragIndex] = Tuple.Create(setShips[dragIndex].Item1, false);
                    else
                        currentShips[dragIndex] = Tuple.Create(currentShips[dragIndex].Item1, false);
                }

                snapping = false;
                dragging = false;

                Invalidate();
            }
            else if (e.Button == MouseButtons.Right && dragging && drag.Size != 1)
            {
                drag = new Battleships.Ship.Properties(drag.Size, !drag.IsVertical, drag.X, drag.Y);

                int tmp = dragOffsetX;
                dragOffsetX = dragOffsetY;
                dragOffsetY = tmp;

                SnapInvalidate(e);
            }
        }

        private void MainWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                SnapInvalidate(e);
            }
        }

        private void SnapInvalidate(MouseEventArgs e)
        {
            Snap(e.X, e.Y, dragOffsetX, dragOffsetY, drag.Size, drag.IsVertical, out snapping, out snapX, out snapY);
            Invalidate();
        }

        private static void Snap(int mx, int my, int offsetX, int offsetY, int shiplength, bool shipvertical, out bool snapping, out int snapX, out int snapY)
        {
            // concise version
            double adjustedOffsetX = (Math.Floor((double)offsetX / cellSize) + 0.5) * cellSize;
            double adjustedOffsetY = (Math.Floor((double)offsetY / cellSize) + 0.5) * cellSize;

            /*
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
            */

            snapX = (int)Math.Round((mx - adjustedOffsetX - boardX) / cellSize);
            snapY = (int)Math.Round((my - adjustedOffsetY - boardY) / cellSize);

            snapping = Battleships.WithinBoard(shipvertical, shiplength, snapX, snapY);
        }
    }
}
