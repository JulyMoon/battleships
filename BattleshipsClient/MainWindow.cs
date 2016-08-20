using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using BattleshipsCommon;

// todo: fix the rightclicking while dragging over control bug

namespace BattleshipsClient
{
    public partial class MainWindow : Form
    {
        private const int boardWidth = Game.BoardWidth;
        private const int boardHeight = Game.BoardHeight;
        private const int myBoardX = 40;
        private const int myBoardY = 40;
        private const int enemyBoardX = myBoardX + (boardWidth + 3) * cellSize;
        private const int enemyBoardY = myBoardY;
        private const int cellSize = 25;
        private const int shipWindowX = myBoardX + boardWidth * cellSize + 35;
        private const int shipWindowY = myBoardY;
        private const int shipWindowPadding = 20;
        private const int shipWindowMargin = 15;
        private const int turnIndicatorPadding = 5;
        private const int turnIndicatorWidth = 5;
        private const int turnIndicatorAlpha = 120;
        private const int hoverPenWidth = 3;
        private const int shipPenWidth = 2;
        private const int cellSideLineAdjust = 1;
        private const float emptyCellRatio = 1f / 6;

        private static readonly int[,] adjacentNeighbors = { { 0, -1 }, { 1, 0 }, { -1, 0 }, { 0, 1 } };
        
        private static readonly Pen boardPen = new Pen(Color.FromArgb(180, 180, 255));
        private static readonly Brush boardTextBrush = Brushes.Black;
        private static readonly Pen shipEditablePen = new Pen(Color.CornflowerBlue, shipPenWidth);
        private static readonly Pen shipPen = new Pen(Color.Blue, shipPenWidth);
        private static readonly Pen shipSnapPen = new Pen(Color.ForestGreen, shipPenWidth);
        private static readonly Pen shipDeadPen = new Pen(Color.Red, shipPenWidth);
        private static readonly Pen shipDeadCrossPen = new Pen(shipDeadPen.Color, 2);
        private static readonly Pen shipWindowPen = boardPen;
        private static readonly Pen myTurnPen = new Pen(Color.FromArgb(turnIndicatorAlpha, Color.LimeGreen), turnIndicatorWidth);
        private static readonly Pen enemyTurnPen = new Pen(Color.FromArgb(turnIndicatorAlpha, Color.Red), turnIndicatorWidth);
        private static readonly Color winColor = Color.FromArgb(120, Color.LimeGreen);
        private static readonly Color loseColor = Color.FromArgb(120, Color.Red);
        private static readonly Color neutralStatusColor = Color.Transparent;
        private static readonly Pen hoverPen = new Pen(Color.MediumSeaGreen, hoverPenWidth);
        private static readonly Pen hoverDisabledPen = new Pen(Color.DarkGray, hoverPenWidth);
        private static readonly Brush emptyBrush = new SolidBrush(Color.DimGray);
        private static readonly Brush verifiedEmptyBrush = new SolidBrush(Color.DarkGray);
        private static readonly Font boardFont = new Font("Consolas", 10);

        private Locale locale;
        private readonly Battleships game = new Battleships();
        private readonly List<Tuple<ShipProperties, bool>> currentShips = new List<Tuple<ShipProperties, bool>>();
        private readonly List<Tuple<ShipProperties, bool>> setShips = new List<Tuple<ShipProperties, bool>>();
                                                  // ^ this bool represents whether the ship was used or not

        private Graphics g;

        private bool draggingSetShip;
        private int dragIndex;
        private bool dragging;
        private int dragOffsetX;
        private int dragOffsetY;
        private ShipProperties drag;

        private bool snapping;
        private int snapX;
        private int snapY;

        private int shipWindowWidth;
        private int shipWindowHeight;

        private bool hovering; // over the enemy board;
        private int hoverX;
        private int hoverY;

        private bool responseReceived = true;
        private bool canShoot => game.MyTurn && game.GetEnemyCell(hoverX, hoverY) == Battleships.Cell.Unknown
            && !game.GetEnemyVerifiedEmptyCell(hoverX, hoverY) && responseReceived;

        private readonly Control[] placementControls;

        private enum Stage { Placement, Matchmaking, Playing, Postgame }

        private Stage stage = Stage.Placement;

        public MainWindow()
        {
            InitializeComponent();

            ApplyLocale(Locale.GetLocale(CultureInfo.InstalledUICulture));

            AdoptShipSet(Game.ShipSet);

            placementControls = new Control[] {randomButton, playButton};
            CenterControls(new Rectangle(shipWindowX, shipWindowY + shipWindowHeight, shipWindowWidth, boardHeight * cellSize - shipWindowHeight), placementControls);
            CenterControls(new Rectangle(0, myBoardY + boardHeight * cellSize, ClientSize.Width, ClientSize.Height - (myBoardY + boardHeight * cellSize)), statusLabel);
            CenterControls(new Rectangle(ClientSize.Width / 2, myBoardY + boardHeight * cellSize, ClientSize.Width / 2, ClientSize.Height - (myBoardY + boardHeight * cellSize)), continueButton);

            ResetStatus();

            game.OpponentFound += OnOpponentFound;
            game.OpponentShot += OnOpponentShot;
            game.MyShotReceived += OnMyShotReceived;
        }

        private void ApplyLocale(Locale l)
        {
            locale = l;
            Text = locale.Title;
            statusLabel.Text = locale.PlacementStatus;
            randomButton.Text = locale.RandomButton;
            playButton.Text = locale.PlayButton;
            continueButton.Text = locale.ContinueButton;
        }

        private void SwitchToPlayingStage()
        {
            UpdateStatus();

            TogglePlacementControlVisibility(false);

            stage = Stage.Playing;
            Invalidate();
        }

        private void SwitchToPostgameStage()
        {
            statusLabel.Text = game.Won ? locale.Win : locale.Loss;
            statusLabel.BackColor = game.Won ? winColor : loseColor;
            continueButton.Visible = true;
            stage = Stage.Postgame;
        }

        private void SwitchToPlacementStage()
        {
            ResetStatus();
            TogglePlacementControlAvailability(true);
            TogglePlacementControlVisibility(true);
            continueButton.Visible = false;
            game.NewGame();

            stage = Stage.Placement;
            Invalidate();
        }

        private void HandleShot()
        {
            responseReceived = true;

            if (game.GameOver)
                SwitchToPostgameStage();
            else
                UpdateStatus();

            Invalidate();
        }

        private void OnOpponentFound() => RunOnUIThread(SwitchToPlayingStage);

        private void OnOpponentShot() => RunOnUIThread(HandleShot);

        private void OnMyShotReceived() => RunOnUIThread(HandleShot);

        private void UpdateStatus()
            => statusLabel.Text = game.MyTurn ? locale.YourTurn : locale.OpponentsTurn;

        private void TogglePlacementControlVisibility(bool visible)
        {
            foreach (var control in placementControls)
                control.Visible = visible;
        }

        private void TogglePlacementControlAvailability(bool enabled)
        {
            foreach (var control in placementControls)
                control.Enabled = enabled;
        }

        private void ResetStatus()
        {
            statusLabel.Text = locale.PlacementStatus;
            statusLabel.BackColor = neutralStatusColor;
        }

        private void RunOnUIThread(Action action)
        {
            if (InvokeRequired)
                Invoke(action);
            else
                action();
        }

        private static void CenterControls(Rectangle rect, params Control[] controls)
        {
            int minX = controls.Select(control => control.Location.X).Min();
            int minY = controls.Select(control => control.Location.Y).Min();

            int maxX = controls.Select(control => control.Location.X + control.Width).Max();
            int maxY = controls.Select(control => control.Location.Y + control.Height).Max();

            int width = maxX - minX;
            int height = maxY - minY;

            int ax = rect.X + (rect.Width - width) / 2;
            int ay = rect.Y + (rect.Height - height) / 2;

            foreach (var control in controls)
            {
                int nx = ax + (control.Location.X - minX);
                int ny = ay + (control.Location.Y - minY);

                control.Location = new Point(nx, ny);
            }
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

                int x = shipWindowX + shipWindowPadding + count * (shipSize * cellSize + shipWindowMargin);
                int y = shipWindowY + shipWindowPadding + shipTypes.IndexOf(shipSize) * (cellSize + shipWindowMargin);

                int width = shipWindowPadding * 2 + (count + 1) * (shipSize * cellSize + shipWindowMargin) - shipWindowMargin;
                if (width > shipWindowWidth)
                    shipWindowWidth = width;

                setShips.Add(Tuple.Create(new ShipProperties(shipSize, false, x, y), false));
            }
            
            shipWindowHeight = shipWindowPadding * 2 + shipTypes.Count * (cellSize + shipWindowMargin) - shipWindowMargin;
        }

        private static Rectangle GetShipRectangle(ShipProperties shipProps)
        {
            int shipWidth, shipHeight;
            Game.GetShipDimensions(shipProps.IsVertical, shipProps.Size, out shipWidth, out shipHeight);
            shipWidth *= cellSize;
            shipHeight *= cellSize;

            return new Rectangle(shipProps.X, shipProps.Y, shipWidth, shipHeight);
        }

        private void DrawAbsoluteShip(Pen pen, ShipProperties shipProps)
            => g.DrawRectangle(pen, GetShipRectangle(shipProps));

        private void DrawBoardShip(Pen pen, ShipProperties ship, int boardx, int boardy)
            => DrawAbsoluteShip(pen, new ShipProperties(ship.Size, ship.IsVertical, boardx + ship.X * cellSize, boardy + ship.Y * cellSize));

        private void DrawBoardShip(Pen pen, Ship ship, int boardx, int boardy)
        {
            for (int i = 0; i < ship.Size; i++)
            {
                if (ship.IsAlive[i])
                    continue;

                int x, y;
                if (ship.IsVertical)
                {
                    x = ship.X;
                    y = ship.Y + i;
                }
                else
                {
                    x = ship.X + i;
                    y = ship.Y;
                }

                x = boardx + x * cellSize;
                y = boardy + y * cellSize;

                g.DrawLine(shipDeadCrossPen, x, y, x + cellSize, y + cellSize);
                g.DrawLine(shipDeadCrossPen, x, y + cellSize, x + cellSize, y);
            }

            DrawAbsoluteShip(ship.Dead ? shipDeadPen : pen, new ShipProperties(ship.Size, ship.IsVertical, boardx + ship.X * cellSize, boardy + ship.Y * cellSize));
        }

        private void DrawBoard(int boardx, int boardy)
        {
            for (int i = 0; i <= boardWidth; i++)
            {
                int x = boardx + i * cellSize;
                g.DrawLine(boardPen, x, boardy, x, boardy + boardHeight * cellSize);
            }

            for (int i = 0; i <= boardHeight; i++)
            {
                int y = boardy + i * cellSize;
                g.DrawLine(boardPen, boardx, y, boardx + boardWidth * cellSize, y);
            }

            for (int i = 0; i < boardWidth; i++)
                g.DrawString(locale.Alphabet[i].ToString(), boardFont, boardTextBrush, boardx + i * cellSize + 7, boardy - cellSize + 3);

            for (int i = 0; i < boardHeight; i++)
            {
                string number = (i + 1).ToString();
                g.DrawString(number, boardFont, boardTextBrush, boardx - cellSize + (number.Length == 1 ? 4 : 0), boardy + i * cellSize + 4);
            }
        }

        private void DrawShipWindow(Pen pen, int x, int y, int width, int height) => g.DrawRectangle(pen, x, y, width, height);

        private void DrawBoardShips(Pen pen, IEnumerable<ShipProperties> ships, int boardx, int boardy)
        {
            foreach (var ship in ships)
                DrawBoardShip(pen, ship, boardx, boardy);
        }

        private void DrawBoardShips(Pen pen, IEnumerable<Ship> ships, int boardx, int boardy)
        {
            foreach (var ship in ships)
                DrawBoardShip(pen, ship, boardx, boardy);
        }

        private void DrawAbsoluteShips(Pen pen, IEnumerable<ShipProperties> ships)
        {
            foreach (var ship in ships)
                DrawAbsoluteShip(pen, ship);
        }

        private void DrawPlacementStage()
        {
            DrawBoard(myBoardX, myBoardY);
            DrawShipWindow(shipWindowPen, shipWindowX, shipWindowY, shipWindowWidth, shipWindowHeight);

            DrawBoardShips(shipEditablePen, GetNotUsedShips(currentShips), myBoardX, myBoardY);
            DrawAbsoluteShips(shipEditablePen, GetNotUsedShips(setShips));

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

            DrawAbsoluteShip(snapping ? shipSnapPen : shipEditablePen, new ShipProperties(drag.Size, drag.IsVertical, dragx, dragy));
        }

        private void DrawPlayingStage()
        {
            int boardx, boardy;
            if (game.MyTurn)
            {
                boardx = enemyBoardX;
                boardy = enemyBoardY;
            }
            else
            {
                boardx = myBoardX;
                boardy = myBoardY;
            }
            DrawTurnIndicator(game.MyTurn ? myTurnPen : enemyTurnPen, boardx, boardy);

            DrawBoard(myBoardX, myBoardY);
            DrawBoardShips(shipPen, game.MyShips, myBoardX, myBoardY);
            DrawMyMissCells();
            
            DrawBoard(enemyBoardX, enemyBoardY);
            DrawEnemyBoardCells();

            if (hovering)
                DrawHoverIndicator();
        }

        private void DrawPostgameStage()
        {
            DrawBoard(myBoardX, myBoardY);
            DrawBoardShips(shipPen, game.MyShips, myBoardX, myBoardY);
            DrawMyMissCells();

            DrawBoard(enemyBoardX, enemyBoardY);
            DrawEnemyBoardCells();
        }

        private void DrawHoverIndicator()
            => g.DrawRectangle(canShoot ? hoverPen : hoverDisabledPen, enemyBoardX + hoverX * cellSize, enemyBoardY + hoverY * cellSize, cellSize, cellSize);

        private void DrawMyMissCells()
        {
            for (int x = 0; x < Game.BoardWidth; x++)
                for (int y = 0; y < Game.BoardHeight; y++)
                {
                    if (game.GetMyMissCell(x, y))
                        DrawEmptyCell(true, x, y, myBoardX, myBoardY);
                    else if (game.GetMyVerifiedEmptyCell(x, y))
                        DrawEmptyCell(false, x, y, myBoardX, myBoardY);
                }
        }

        private void DrawShotEnemyShipCell(int x, int y)
        {
            g.DrawLine(shipDeadCrossPen, enemyBoardX + x * cellSize, enemyBoardY + y * cellSize, enemyBoardX + (x + 1) * cellSize, enemyBoardY + (y + 1) * cellSize);
            g.DrawLine(shipDeadCrossPen, enemyBoardX + x * cellSize, enemyBoardY + (y + 1) * cellSize, enemyBoardX + (x + 1) * cellSize, enemyBoardY + y * cellSize);

            for (int i = 0; i < 4; i++)
            {
                int ax = adjacentNeighbors[i, 0];
                int ay = adjacentNeighbors[i, 1];
                int xx = x + ax;
                int yy = y + ay;

                if (Game.WithinBoard(xx, yy) && !game.GetEnemyVerifiedEmptyCell(xx, yy) && game.GetEnemyCell(xx, yy) != Battleships.Cell.Empty)
                    continue;

                xx = enemyBoardX + x * cellSize;
                yy = enemyBoardY + y * cellSize;

                int x1, y1, x2, y2;

                if (ax == 0 && ay == -1) // up
                {
                    x1 = xx - cellSideLineAdjust;
                    y1 = yy;
                    x2 = x1 + cellSize + cellSideLineAdjust;
                    y2 = y1;
                }
                else if (ax == 1 && ay == 0) // right
                {
                    x1 = xx + cellSize;
                    y1 = yy - cellSideLineAdjust;
                    x2 = x1;
                    y2 = y1 + cellSize + cellSideLineAdjust;
                }
                else if (ax == 0 && ay == 1) // down
                {
                    x1 = xx - cellSideLineAdjust;
                    y1 = yy + cellSize;
                    x2 = x1 + cellSize + cellSideLineAdjust;
                    y2 = y1;
                }
                else // left
                {
                    x1 = xx;
                    y1 = yy - cellSideLineAdjust;
                    x2 = x1;
                    y2 = y1 + cellSize + cellSideLineAdjust;
                }

                g.DrawLine(shipDeadPen, x1, y1, x2, y2);
            }
        }

        private void DrawEnemyBoardCells()
        {
            for (int x = 0; x < Game.BoardWidth; x++)
                for (int y = 0; y < Game.BoardHeight; y++)
                {
                    switch (game.GetEnemyCell(x, y))
                    {
                        case Battleships.Cell.Ship: DrawShotEnemyShipCell(x, y); break;
                        case Battleships.Cell.Empty: DrawEmptyCell(true, x, y, enemyBoardX, enemyBoardY); break;
                        case Battleships.Cell.Unknown: if (game.GetEnemyVerifiedEmptyCell(x, y)) DrawEmptyCell(false, x, y, enemyBoardX, enemyBoardY); break;
                    }
                }
        }

        private void DrawEmptyCell(bool real, int x, int y, int boardx, int boardy)
        {
            const float add = (1 - emptyCellRatio) / 2;
            g.FillEllipse(real ? emptyBrush : verifiedEmptyBrush, boardx + (x + add) * cellSize, boardy + (y + add) * cellSize, emptyCellRatio * cellSize, emptyCellRatio * cellSize);
        }

        private void DrawTurnIndicator(Pen pen, int boardx, int boardy)
            => g.DrawRectangle(pen, boardx - turnIndicatorPadding, boardy - turnIndicatorPadding, boardWidth * cellSize + turnIndicatorPadding * 2, boardHeight * cellSize + turnIndicatorPadding * 2);

        private void DrawMatchmakingStage()
        {
            DrawBoard(myBoardX, myBoardY);
            DrawShipWindow(shipWindowPen, shipWindowX, shipWindowY, shipWindowWidth, shipWindowHeight);

            DrawBoardShips(shipPen, game.MyShipProps, myBoardX, myBoardY);
        }

        private void MainWindow_Paint(object sender, PaintEventArgs e)
        {
            g = e.Graphics;

            switch (stage)
            {
                case Stage.Placement: DrawPlacementStage(); break;
                case Stage.Matchmaking: DrawMatchmakingStage(); break;
                case Stage.Playing: DrawPlayingStage(); break;
                case Stage.Postgame: DrawPostgameStage(); break;
            }
        }

        private static bool IntersectsWithMouse(MouseEventArgs e, Rectangle rect) => rect.IntersectsWith(new Rectangle(e.X, e.Y, 1, 1));

        private static ShipProperties GetAbsoluteShip(ShipProperties ship)
            => new ShipProperties(ship.Size, ship.IsVertical, myBoardX + ship.X * cellSize, myBoardY + ship.Y * cellSize);

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
            if (e.Button != MouseButtons.Left)
                return;

            switch (stage)
            {
                case Stage.Placement:
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

                    Snap(e);
                    break;

                case Stage.Playing:
                    if (hovering && canShoot)
                    {
                        responseReceived = false;
                        game.Shoot(hoverX, hoverY);
                    }
                    break;
            }
        }

        private void MainWindow_MouseUp(object sender, MouseEventArgs e)
        {
            if (stage != Stage.Placement)
                return;

            if (e.Button == MouseButtons.Left)
            {
                if (snapping)
                {
                    var ship = new ShipProperties(drag.Size, drag.IsVertical, snapX, snapY);
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
                drag = new ShipProperties(drag.Size, !drag.IsVertical, drag.X, drag.Y);

                int tmp = dragOffsetX;
                dragOffsetX = dragOffsetY;
                dragOffsetY = tmp;

                Snap(e);
            }
        }

        private void MainWindow_MouseMove(object sender, MouseEventArgs e)
        {
            switch (stage)
            {
                case Stage.Placement: if (dragging) Snap(e); break;
                case Stage.Playing:
                    int x = e.X - enemyBoardX;
                    int y = e.Y - enemyBoardY;

                    hoverX = x / cellSize;
                    hoverY = y / cellSize;

                    hovering = Game.WithinBoard(hoverX, hoverY) && x >= 0 && y >= 0;
                    Invalidate();
                    break;
            }
        }

        private static IEnumerable<ShipProperties> GetNotUsedShips(List<Tuple<ShipProperties, bool>> ships)
            => ships.Where(tuple => !tuple.Item2).Select(tuple => tuple.Item1);

        private void Snap(MouseEventArgs e)
        {
            double adjustedOffsetX = (Math.Floor((double)dragOffsetX / cellSize) + 0.5) * cellSize;
            double adjustedOffsetY = (Math.Floor((double)dragOffsetY / cellSize) + 0.5) * cellSize;

            snapX = (int)Math.Round((e.X - adjustedOffsetX - myBoardX) / cellSize);
            snapY = (int)Math.Round((e.Y - adjustedOffsetY - myBoardY) / cellSize);

            var ship = new ShipProperties(drag.Size, drag.IsVertical, snapX, snapY);

            snapping = Game.WithinBoard(ship) && !Game.Overlaps(GetNotUsedShips(currentShips), ship);
            Invalidate();
        }

        private async void playButton_Click(object sender, EventArgs e)
        {
            TogglePlacementControlAvailability(false);

            if (!game.ConnectedToServer)
            {
                statusLabel.Text = locale.ConnectingStatus;
                await game.ConnectAsync(Game.ServerIP, Environment.UserName);
            }
            
            game.EnterMatchmaking(currentShips.Select(tuple => tuple.Item1).ToList());
            statusLabel.Text = locale.Waiting;

            stage = Stage.Matchmaking;
            Invalidate();
        }

        private void randomButton_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < setShips.Count; i++)
                setShips[i] = Tuple.Create(setShips[i].Item1, true);

            var randomShips = Game.GetRandomShips();
            currentShips.Clear();
            foreach (var ship in randomShips)
                currentShips.Add(Tuple.Create(ship, false));

            playButton.Enabled = true;

            Invalidate();
        }

        private void continueButton_Click(object sender, EventArgs e) => SwitchToPlacementStage();
    }
}
