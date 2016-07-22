using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BattleshipsClient
{
    public partial class MainWindow : Form
    {
        public const bool DEBUG = false;

        private const int boardWidth = Battleships.BoardWidth;
        private const int boardHeight = Battleships.BoardHeight;
        private const int myBoardX = 40;
        private const int myBoardY = 40;
        private const int enemyBoardX = myBoardX + (boardWidth + 3) * cellSize;
        private const int enemyBoardY = myBoardY;
        private const int cellSize = 25;
        private const int shipWindowX = myBoardX + boardWidth * cellSize + padding;
        private const int shipWindowY = myBoardY;
        private const int padding = 20;
        private const int shipWindowShipMargin = 15;
        private const int shipPenWidth = 3;
        
        private static readonly Pen boardLinePen = new Pen(Color.FromArgb(180, 180, 255));
        private static readonly Brush boardTextBrush = Brushes.Black;
        private static readonly Pen shipEditablePen = new Pen(Color.DodgerBlue, shipPenWidth);
        private static readonly Pen shipAlivePen = new Pen(Color.FromArgb(0, 90, 156), shipPenWidth);
        private static readonly Pen shipSnapPen = new Pen(Color.ForestGreen, shipPenWidth);
        private static readonly Pen shipDeadPen = new Pen(Color.Red, shipPenWidth);
        private static readonly Pen shipDeadCrossPen = new Pen(shipDeadPen.Color, 2);
        private static readonly Pen shipWindowPen = boardLinePen;
        private static readonly Font boardFont = new Font("Consolas", 10);

        private readonly Battleships game = new Battleships();
        private readonly List<Tuple<Battleships.Ship.Properties, bool>> currentShips = new List<Tuple<Battleships.Ship.Properties, bool>>();
        private readonly List<Tuple<Battleships.Ship.Properties, bool>> setShips = new List<Tuple<Battleships.Ship.Properties, bool>>();
                                                               // ^ this bool represents whether the ship was used or not

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

        private enum Stage
        {
            Placement, Matchmaking, Playing, Postgame
        }

        private Stage stage = Stage.Placement;

        public MainWindow()
        {
            InitializeComponent();

            AdoptShipSet(Battleships.ShipSet);
            PlacePlayButton();
            PlaceStatusLabel();
            PlaceNameTextbox();

            game.OpponentFound += () => statusLabel.Text = "OPPONENT FOUND :O";
        }

        private void PlacePlayButton()
        {
            playButton.Location = new Point(shipWindowX + (shipWindowWidth - playButton.Width) / 2, shipWindowY + shipWindowHeight + padding);
        }

        private void CenterConnectionControls(Control[] controls)
        {
            int minX = controls.Select(control => control.Location.X).Min();
            int minY = controls.Select(control => control.Location.Y).Min();

            int maxX = controls.Select(control => control.Location.X + control.Width).Max();
            int maxY = controls.Select(control => control.Location.Y + control.Height).Max();

            int width = maxX - minX;
            int height = maxY - minY;

            int ax = (ClientSize.Width - width) / 2;
            int ay = (ClientSize.Height - height) / 2;

            foreach (var control in controls)
            {
                int nx = ax + (control.Location.X - minX);
                int ny = ay + (control.Location.Y - minY);

                control.Location = new Point(nx, ny);
            }
        }

        private void PlaceStatusLabel()
        {
            const int myBoardBottomY = myBoardY + boardHeight * cellSize;
            statusLabel.Location = new Point((ClientSize.Width - statusLabel.Width) / 2, myBoardBottomY + (ClientSize.Height - myBoardBottomY - statusLabel.Height) / 2);
        }

        private void PlaceNameTextbox()
        {
            nameTextBox.Location = new Point(shipWindowX + shipWindowWidth + padding, shipWindowY);
            nameTextBox.Width = ClientSize.Width - nameTextBox.Location.X - padding;
        }

        private void MainWindow_Load(object sender, EventArgs e) { }

        private void MainWindow_Shown(object sender, EventArgs e) { }

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

                int x = shipWindowX + padding + count * (shipSize * cellSize + shipWindowShipMargin);
                int y = shipWindowY + padding + shipTypes.IndexOf(shipSize) * (cellSize + shipWindowShipMargin);

                int width = padding * 2 + (count + 1) * (shipSize * cellSize + shipWindowShipMargin) - shipWindowShipMargin;
                if (width > shipWindowWidth)
                    shipWindowWidth = width;

                setShips.Add(Tuple.Create(new Battleships.Ship.Properties(shipSize, false, x, y), false));
            }
            
            shipWindowHeight = padding * 2 + shipTypes.Count * (cellSize + shipWindowShipMargin) - shipWindowShipMargin;
        }

        private static Rectangle GetShipRectangle(Battleships.Ship.Properties shipProps)
        {
            int shipWidth, shipHeight;
            Battleships.GetShipDimensions(shipProps.IsVertical, shipProps.Size, out shipWidth, out shipHeight);
            shipWidth *= cellSize;
            shipHeight *= cellSize;

            return new Rectangle(shipProps.X, shipProps.Y, shipWidth, shipHeight);
        }

        private static void DrawAbsoluteShip(Graphics g, Pen outlinePen, Battleships.Ship.Properties shipProps)
        {
            var shipRect = GetShipRectangle(shipProps);
            g.DrawRectangle(outlinePen, shipRect);
        }

        private static void DrawBoardShip(Graphics g, Pen outlinePen, Battleships.Ship.Properties ship, int boardx, int boardy)
        {
            DrawAbsoluteShip(g, outlinePen, new Battleships.Ship.Properties(ship.Size, ship.IsVertical, boardx + ship.X * cellSize, boardy + ship.Y * cellSize));
        }

        private static void DrawBoardShip(Graphics g, Pen outlinePen, Pen deadPen, Pen crossPen, Battleships.Ship ship, int boardx, int boardy)
        {
            DrawAbsoluteShip(g, ship.Dead ? deadPen : outlinePen, new Battleships.Ship.Properties(ship.Props.Size, ship.Props.IsVertical, boardx + ship.Props.X * cellSize, boardy + ship.Props.Y * cellSize));

            for (int i = 0; i < ship.Props.Size; i++)
            {
                if (ship.IsAlive[i])
                    continue;

                int x, y;
                if (ship.Props.IsVertical)
                {
                    x = ship.Props.X;
                    y = ship.Props.Y + i;
                }
                else
                {
                    x = ship.Props.X + i;
                    y = ship.Props.Y;
                }

                x = boardx + x * cellSize;
                y = boardy + y * cellSize;

                g.DrawLine(crossPen, x, y, x + cellSize, y + cellSize);
                g.DrawLine(crossPen, x, y + cellSize, x + cellSize, y);
            }
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

        private static void DrawBoardShips(Graphics g, Pen outlinePen, IEnumerable<Battleships.Ship.Properties> ships, int boardx, int boardy)
        {
            foreach (var ship in ships)
            {
                DrawBoardShip(g, outlinePen, ship, boardx, boardy);
            }
        }

        private static void DrawBoardShips(Graphics g, Pen outlinePen, Pen deadPen, Pen crossPen, IEnumerable<Battleships.Ship> ships, int boardx, int boardy)
        {
            foreach (var ship in ships)
            {
                DrawBoardShip(g, outlinePen, deadPen, crossPen, ship, boardx, boardy);
            }
        }

        private static void DrawAbsoluteShips(Graphics g, Pen outlinePen, IEnumerable<Battleships.Ship.Properties> ships)
        {
            foreach (var ship in ships)
            {
                DrawAbsoluteShip(g, outlinePen, ship);
            }
        }

        private void DrawPlacementStage(PaintEventArgs e)
        {
            DrawBoard(e.Graphics, boardTextBrush, boardFont, boardLinePen, myBoardX, myBoardY);
            DrawShipWindow(e.Graphics, shipWindowPen, shipWindowX, shipWindowY, shipWindowWidth, shipWindowHeight);

            DrawBoardShips(e.Graphics, shipEditablePen, GetNotUsedShips(currentShips), myBoardX, myBoardY);
            DrawAbsoluteShips(e.Graphics, shipEditablePen, GetNotUsedShips(setShips));

            if (!dragging)
                return;

            int dragx, dragy;
            if (snapping)
            {
                dragx = myBoardX + snapX * cellSize;
                dragy = myBoardY + snapY * cellSize;
            }
            else
            {
                var mousePosition = PointToClient(Cursor.Position);
                dragx = mousePosition.X - dragOffsetX;
                dragy = mousePosition.Y - dragOffsetY;
            }

            DrawAbsoluteShip(e.Graphics, snapping ? shipSnapPen : shipEditablePen, new Battleships.Ship.Properties(drag.Size, drag.IsVertical, dragx, dragy));
        }

        private void DrawPlayingStage(PaintEventArgs e)
        {
            DrawBoard(e.Graphics, boardTextBrush, boardFont, boardLinePen, myBoardX, myBoardY);
            DrawBoardShips(e.Graphics, shipAlivePen, shipDeadPen, shipDeadCrossPen, game.MyShips, myBoardX, myBoardY);

            DrawBoard(e.Graphics, boardTextBrush, boardFont, boardLinePen, enemyBoardX, enemyBoardY);
            // todo
        }

        private void DrawMatchmakingStage(PaintEventArgs e)
        {
            DrawBoard(e.Graphics, boardTextBrush, boardFont, boardLinePen, myBoardX, myBoardY);
            DrawShipWindow(e.Graphics, shipWindowPen, shipWindowX, shipWindowY, shipWindowWidth, shipWindowHeight);

            DrawBoardShips(e.Graphics, shipAlivePen, GetNotUsedShips(currentShips), myBoardX, myBoardY);
        }

        private void MainWindow_Paint(object sender, PaintEventArgs e)
        {
            switch (stage)
            {
                case Stage.Placement: DrawPlacementStage(e); break;
                case Stage.Matchmaking: DrawMatchmakingStage(e); break;
                case Stage.Playing: DrawPlayingStage(e); break;
                case Stage.Postgame: throw new NotImplementedException();
                default: throw new Exception();
            }
        }

        private static bool IntersectsWithMouse(MouseEventArgs e, Rectangle rect) => rect.IntersectsWith(new Rectangle(e.X, e.Y, 1, 1));

        private static Battleships.Ship.Properties GetAbsoluteShip(Battleships.Ship.Properties ship)
            => new Battleships.Ship.Properties(ship.Size, ship.IsVertical, myBoardX + ship.X * cellSize, myBoardY + ship.Y * cellSize);

        private bool GetShipBelowMouse(MouseEventArgs e, out int index, out bool isSetShip)
        {
            for (int i = 0; i < setShips.Count; i++)
            {
                if (setShips[i].Item2)
                    continue;

                if (!IntersectsWithMouse(e, GetShipRectangle(setShips[i].Item1)))
                    continue;
                
                isSetShip = true;
                index = i;

                return true;
            }

            for (int i = 0; i < currentShips.Count; i++)
            {
                if (currentShips[i].Item2)
                    continue;

                if (!IntersectsWithMouse(e, GetShipRectangle(GetAbsoluteShip(currentShips[i].Item1))))
                    continue;

                isSetShip = false;
                index = i;

                return true;
            }

            index = -1;
            isSetShip = false;

            return false;
        }

        private void MainWindow_MouseDown(object sender, MouseEventArgs e)
        {
            if (stage != Stage.Placement)
                return;

            if (e.Button != MouseButtons.Left)
                return;
            
            if (!GetShipBelowMouse(e, out dragIndex, out draggingSetShip))
                return;

            if (draggingSetShip)
            {
                var ship = setShips[dragIndex].Item1;
                setShips[dragIndex] = Tuple.Create(ship, true);
                drag = ship;
            }
            else
            {
                var ship = currentShips[dragIndex].Item1;
                currentShips[dragIndex] = Tuple.Create(ship, true);
                drag = GetAbsoluteShip(ship);
            }

            dragOffsetX = e.X - drag.X;
            dragOffsetY = e.Y - drag.Y;
            dragging = true;

            SnapInvalidate(e);
        }

        private void MainWindow_MouseUp(object sender, MouseEventArgs e)
        {
            if (stage != Stage.Placement)
                return;

            if (e.Button == MouseButtons.Left)
            {
                if (snapping)
                {
                    var ship = new Battleships.Ship.Properties(drag.Size, drag.IsVertical, snapX, snapY);
                    if (draggingSetShip)
                        currentShips.Add(Tuple.Create(ship, false));
                    else
                        currentShips[dragIndex] = Tuple.Create(ship, false);

                    if (currentShips.Count == setShips.Count)
                        playButton.Enabled = true;
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
            if (stage != Stage.Placement)
                return;

            if (dragging)
            {
                SnapInvalidate(e);
            }
        }

        private static IEnumerable<Battleships.Ship.Properties> GetNotUsedShips(List<Tuple<Battleships.Ship.Properties, bool>> ships)
            => ships.Where(tuple => !tuple.Item2).Select(tuple => tuple.Item1);

        private void SnapInvalidate(MouseEventArgs e)
        {
            Snap(GetNotUsedShips(currentShips), e.X, e.Y, dragOffsetX, dragOffsetY, drag.Size, drag.IsVertical, out snapping, out snapX, out snapY);
            Invalidate();
        }

        private static void Snap(IEnumerable<Battleships.Ship.Properties> ships, int mx, int my, int offsetX, int offsetY, int shipsize, bool shipvertical, out bool snapping, out int snapX, out int snapY)
        {
            double adjustedOffsetX = (Math.Floor((double)offsetX / cellSize) + 0.5) * cellSize;
            double adjustedOffsetY = (Math.Floor((double)offsetY / cellSize) + 0.5) * cellSize;

            snapX = (int)Math.Round((mx - adjustedOffsetX - myBoardX) / cellSize);
            snapY = (int)Math.Round((my - adjustedOffsetY - myBoardY) / cellSize);

            var ship = new Battleships.Ship.Properties(shipsize, shipvertical, snapX, snapY);

            snapping = Battleships.WithinBoard(ship) && !Battleships.Overlaps(ships, ship);
        }

        private async void playButton_Click(object sender, EventArgs e)
        {
            playButton.Enabled = false;
            nameTextBox.Enabled = false;
            statusLabel.Text = "Connecting to the server...";

            if (!DEBUG)
            {
                await game.ConnectAsync(IPAddress.Loopback, "foxneZz");
            }

            statusLabel.Text = "Waiting for opponent...";

            //game.AddShips(currentShips.Select(tuple => tuple.Item1).ToList());
            stage = Stage.Matchmaking;
            Invalidate();
        }
    }
}
