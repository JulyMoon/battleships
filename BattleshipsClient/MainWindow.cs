using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
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
        private static readonly Color boardColor = Color.BlueViolet;
        private static readonly Color boardTextColor = Color.DodgerBlue;
        private static readonly Color backgroundColor = Color.White;
        private static readonly Font boardFont = new Font(FontFamily.GenericMonospace, 13);

        private static readonly Pen boardPen = new Pen(boardColor);
        private static readonly Brush boardBrush = new SolidBrush(boardTextColor);

        private Graphics gfx;
        private bool dragging = false;
        private int ship = 4;
        private bool vertical = true;

        public MainWindow()
        {
            InitializeComponent();
            gfx = CreateGraphics();
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {

        }

        private void MainWindow_Shown(object sender, EventArgs e)
        {

        }

        private void FreeDrawShipCell(Pen pen, int x, int y)
        {
            gfx.DrawRectangle(pen, x, y, cellSize, cellSize);

            const int k = 4; // assert: cellSize % k == 0

            for (int i = 0; i < cellSize / k; i++)
            {
                int ax = x + i * k;
                int ay = y + i * k;
                gfx.DrawLine(pen, ax, y, x, ay);
                gfx.DrawLine(pen, ax, y + cellSize, x + cellSize, ay);
            }
        }

        private void FreeDrawShip(Pen pen, bool vertical, int length, int x, int y)
        {
            for (int i = 0; i < length; i++)
            {
                if (vertical)
                {
                    FreeDrawShipCell(pen, x, y + i * cellSize);
                }
                else
                {
                    FreeDrawShipCell(pen, x + i * cellSize, y);
                }
            }
        }

        private void DrawBoard()
        {
            for (int i = 0; i <= boardWidth; i++)
            {
                int x = boardX + i * cellSize;
                gfx.DrawLine(boardPen, x, boardY, x, boardY + boardHeight * cellSize);
            }

            for (int i = 0; i <= boardHeight; i++)
            {
                int y = boardY + i * cellSize;
                gfx.DrawLine(boardPen, boardX, y, boardX + boardWidth * cellSize, y);
            }

            for (int i = 0; i < boardWidth; i++)
            {
                var number = (i + 1).ToString();
                gfx.DrawString(number, boardFont, boardBrush, boardX + i * cellSize + (number.Length == 1 ? 3 : -3), boardY - cellSize);
            }

            for (int i = 0; i < boardHeight; i++)
                gfx.DrawString(((char)('A' + i)).ToString(), boardFont, boardBrush, boardX - cellSize, boardY + i * cellSize);
        }

        private void DrawShipWindow(Pen pen) => gfx.DrawRectangle(pen, shipWindowX, shipWindowY, shipWindowSize, shipWindowSize);

        private void MainWindow_Paint(object sender, PaintEventArgs e)
        {
            gfx.Clear(backgroundColor);
            DrawBoard();
            DrawShipWindow(Pens.Navy);
            FreeDrawShip(Pens.Black, true, 4, 285, 105);
        }

        private void MainWindow_MouseDown(object sender, MouseEventArgs e)
        {

        }

        private void MainWindow_MouseUp(object sender, MouseEventArgs e)
        {

        }

        private void MainWindow_MouseMove(object sender, MouseEventArgs e)
        {

        }
    }
}
